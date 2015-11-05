using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using Assembly_Planner.GraphSynth.BaseClasses;
using StarMathLib;
using TVGL;
using Tool = Assembly_Planner.GraphSynth.BaseClasses.Tool;

namespace Assembly_Planner
{
    internal class BoltAndGearDetection
    {
        internal static List<Fastener> Fasteners = new List<Fastener>();
        internal static List<Nut> Nuts = new List<Nut>();

        internal static HashSet<TessellatedSolid> ScrewAndBoltDetector(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs, bool autoDetection, bool threaded,
            bool regenerateTrainingData = false)
        {
            if (threaded)
            {
                FastenerLearner.RunPerecptronLearner(regenerateTrainingData);
                // after training data is generated (or exists), now I should check and see
                // if the csv containing weights and votes exists or not.
                // Even if the trainingData csv doesnt exist but the csv of w and votes exist,
                // we can run the classifier. 
                // In TrainingDataGenerator, if user says dont regenerate the training data, now check and
                // see if the WeightsAndVotes.csv exists or not.
            }
            var s = Stopwatch.StartNew();
            s.Start();
            Console.WriteLine();
            Console.WriteLine("Detecting Gears and Fasteners ....");
            var fastener = new HashSet<TessellatedSolid>();
            if (!autoDetection)
            {
                foreach (var solid in solidPrimitive.Keys)
                    if (solid.Name.Contains("Screw") || solid.Name.Contains("Test - Part-S"))
                        fastener.Add(solid);
                    else if (solid.Name.Contains("ShaftCollar"))
                        fastener.Add(solid);
                    else if (solid.Name.Contains("DowellGrooved"))
                        if (solid.Name.Contains("-5") || solid.Name.Contains("-14") || solid.Name.Contains("-27") ||
                            solid.Name.Contains("-30"))
                            continue;
                        else
                            fastener.Add(solid);
            }
            else
            {
                if (!threaded)
                    fastener = AutoFastenerDetectionNoThread(solidPrimitive, multipleRefs);
                else
                    fastener = AutoFastenerDetectionThreaded(solidPrimitive, multipleRefs);
            }

            //var smallObjects = SmallObjectsDetector(solidPrimitive);
            //fastener.UnionWith(smallObjects);
            s.Stop();
            Console.WriteLine("Gear and Fastener Detection:" + "     " + s.Elapsed);
            return fastener;
        }


        private static HashSet<TessellatedSolid> AutoFastenerDetectionNoThread(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs)
        {
            // This approach will use the symmetric shape of the fasteners' heads. If there is no thread,
            // we willl consider the area of the positive culinders for bolts and negative cylinder for 
            // nuts. 
            var fastener = new HashSet<TessellatedSolid>();
            var firstFilter = multipleRefs.Keys.ToList(); //SmallObjectsDetector(multipleRefs);
            var equalFlatPrimitivesForEverySolid = EqualFlatPrimitiveAreaFinder(firstFilter, solidPrimitive);
            var groupedPotentialFasteners = GroupingSmallParts(firstFilter);
            foreach (var solid in firstFilter)
            {
                if (HexBoltNutAllen(solid, solidPrimitive[solid], equalFlatPrimitivesForEverySolid[solid]))
                    continue;
                if (PhillipsHeadBolt(solid, solidPrimitive[solid], equalFlatPrimitivesForEverySolid[solid]))
                    continue;
                if (SlotHeadBolt(solid, solidPrimitive[solid], equalFlatPrimitivesForEverySolid[solid]))
                    continue;
                if (PhillipsSlotComboHeadBolt(solid, solidPrimitive[solid], equalFlatPrimitivesForEverySolid[solid]))
                    continue;
            }
            // when all of the fasteners are recognized, it's time to check the third level filter:
            // something is not acceptable: nut without fastener. If there is a nut without fastener,
            // I will try to look for that.
            //var
            return fastener;
        }

        private static HashSet<TessellatedSolid> AutoFastenerDetectionThreaded(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs)
        {
            // This is mostly similar to the auto fastener detection with no thread, but instead of learning
            // from the area of the cylinder and for example flat, we will learn from the number of faces.
            // why? because if we have thread, we will have too many triangles. And this can be useful.
            // I can also detect helix and use this to detect the threaded fasteners

            // Important: if the fasteners are threaded using solidworks Fastener toolbox, it will not
            //            have helix. The threads will be small cones with the same axis and equal area.

            var firstFilter = multipleRefs.Keys.ToList(); //SmallObjectsDetector(multipleRefs);
            var equalPrimitivesForEverySolid = EqualFlatPrimitiveAreaFinder(firstFilter, solidPrimitive);
            var groupedPotentialFasteners = GroupingSmallParts(firstFilter);
            List<int> learnerVotes;
            var learnerWeights = FastenerLearner.ReadingLearnerWeightsAndVotesFromCsv(out learnerVotes);
            foreach (var solid in firstFilter)
            {
                if (HexBoltNutAllen(solid, solidPrimitive[solid], equalPrimitivesForEverySolid[solid]))
                    continue;
                if (PhillipsHeadBolt(solid, solidPrimitive[solid], equalPrimitivesForEverySolid[solid]))
                    continue;
                if (SlotHeadBolt(solid, solidPrimitive[solid], equalPrimitivesForEverySolid[solid]))
                    continue;
                if (PhillipsSlotComboHeadBolt(solid, solidPrimitive[solid], equalPrimitivesForEverySolid[solid]))
                    continue;
                // if it is not any of those, we can still give it another chance:
                if (ThreadDetector(solid, solidPrimitive[solid]))
                    continue;
                // We may still have some threaded fasteners that could not be recognized by the 
                // "ThreadDetector" function.
                // Solution: Voted Perceptron classifier
                // run it here. How? 
                if (FastenerLearner.FastenerPerceptronLearner(solidPrimitive[solid], solid, learnerWeights, learnerVotes))
                    continue;
                // One more approach which actually turned out to be really interesting:
                //    Create the OBB around the object. take any of the side faces. generate 
                //    bunch of rays on the middle line of the side face. Shoot the ray and
                //    find the smallest distance to the closest triangle. Plot these calculated
                //    distances. Take a look at the trend and try to learn from it. 
                if (FastenerPolynomialTrend.PolynomialTrendDetector(solid)) continue;
            }
            return null;
        }

        private static int HasHexagon(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<PrimitiveSurface, List<PrimitiveSurface>> equalPrimitives)
        {
            // 0: false (doesnt have hexagon)
            // 1: HexBolt
            // 2: HexNut
            // 3: Allen
            var sixFlat = EqualPrimitivesFinder(equalPrimitives, 6);
            if (!sixFlat.Any()) return 0;
            foreach (var candidateHex in sixFlat)
            {
                var candidateHexVal = equalPrimitives[candidateHex];
                var cos = new List<double>();
                var firstPrimNormal = ((Flat) candidateHexVal[0]).Normal;
                for (var i = 1; i < candidateHexVal.Count; i++)
                    cos.Add(firstPrimNormal.dotProduct(((Flat) candidateHexVal[i]).Normal));
                // if it is a hex or allen bolt, the cos list must have two 1/2, two -1/2 and one -1
                if (cos.Count(c => Math.Abs(0.5 - c) < 0.0001) != 2 ||
                    cos.Count(c => Math.Abs(-0.5 - c) < 0.0001) != 2 ||
                    cos.Count(c => Math.Abs(-1 - c) < 0.0001) != 1) continue;
                if (IsItAllen(candidateHexVal))
                    return 3;
                // else: it is a hex bolt or nut
                if (IsItNut(solidPrim.Where(p => p is Cylinder).Cast<Cylinder>().ToList(), solid))
                    return 2;
                return 1;
            }
            return 0;
        }

        private static List<List<TessellatedSolid>> GroupingSmallParts(List<TessellatedSolid> firstFilter)
        {
            var groups = new List<List<TessellatedSolid>>();
            for (var i = 0; i < firstFilter.Count - 1; i++)
            {
                for (var j = i + 1; j < firstFilter.Count; j++)
                {
                    if (!BlockingDetermination.BoundingBoxOverlap(firstFilter[i], firstFilter[j])) continue;
                    if (!BlockingDetermination.ConvexHullOverlap(firstFilter[i], firstFilter[j])) continue;
                    var exist = groups.Where(group => @group.Contains(firstFilter[i]) ||
                                                      @group.Contains(firstFilter[j])).ToList();
                    if (exist.Any())
                        exist[0].Add(exist[0].Contains(firstFilter[i]) ? firstFilter[j] : firstFilter[i]);
                    else
                        groups.Add(new List<TessellatedSolid> {firstFilter[i], firstFilter[j]});
                }
            }
            return groups;
        }

        private static bool HexBoltNutAllen(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<PrimitiveSurface, List<PrimitiveSurface>> equalPrimitives)
        {
            var sixFlat = EqualPrimitivesFinder(equalPrimitives, 6);
            if (!sixFlat.Any()) return false;
            foreach (var candidateHex in sixFlat)
            {
                var candidateHexVal = equalPrimitives[candidateHex];
                var cos = new List<double>();
                var firstPrimNormal = ((Flat) candidateHexVal[0]).Normal;
                for (var i = 1; i < candidateHexVal.Count; i++)
                    cos.Add(firstPrimNormal.dotProduct(((Flat) candidateHexVal[i]).Normal));
                // if it is a hex or allen bolt, the cos list must have two 1/2, two -1/2 and one -1
                if (cos.Count(c => Math.Abs(0.5 - c) < 0.0001) != 2 ||
                    cos.Count(c => Math.Abs(-0.5 - c) < 0.0001) != 2 ||
                    cos.Count(c => Math.Abs(-1 - c) < 0.0001) != 1) continue;
                if (IsItAllen(candidateHexVal))
                {
                    // this is a socket bolt (allen)
                    var fastener = new Fastener
                    {
                        Solid = solid,
                        FastenerType = FastenerTypeEnum.Bolt,
                        Tool = Tool.Allen,
                        ToolSize = ToolSizeFinder(candidateHexVal),
                        RemovalDirection =
                            RemovalDirectionFinderForAllenHexPhillips(candidateHexVal.Cast<Flat>().ToList(),
                                PartitioningSolid.OrientedBoundingBoxDic[solid])
                    };
                    Fasteners.Add(fastener);
                    return true;
                }
                // else: it is a hex bolt or nut
                if (IsItNut(solidPrim.Where(p => p is Cylinder).Cast<Cylinder>().ToList(), solid))
                {
                    Nuts.Add(new Nut
                    {
                        NutType = NutType.Hex,
                        Solid = solid,
                        ToolSize = ToolSizeFinder(candidateHexVal)
                    });
                    return true;
                }
                Fasteners.Add(new Fastener
                {
                    Solid = solid,
                    FastenerType = FastenerTypeEnum.Bolt,
                    Tool = Tool.HexWrench,
                    ToolSize = ToolSizeFinder(candidateHexVal),
                    RemovalDirection =
                        RemovalDirectionFinderForAllenHexPhillips(candidateHexVal.Cast<Flat>().ToList(),
                            PartitioningSolid.OrientedBoundingBoxDic[solid])
                });
                return true;
            }
            return false;
        }

        private static bool IsItAllen(List<PrimitiveSurface> candidateHexVal)
        {
            return candidateHexVal.Any(p => p.OuterEdges.Any(e => e.Curvature == CurvatureType.Concave));
        }

        private static bool IsItNut(List<Cylinder> cylinders, TessellatedSolid boltOrNut)
        {
            if (cylinders.Any(p => !p.IsPositive))
                if (cylinders.Where(c => c.IsPositive).Sum(pC => pC.Area) <
                    0.05*boltOrNut.SurfaceArea)
                    // this is a nut. Because it has negative cylinders and positive cylinders that it has
                    // are minor
                    return true;
            return false;
        }

        private static bool PhillipsHeadBolt(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<PrimitiveSurface, List<PrimitiveSurface>> equalPrimitives)
        {
            var eightFlat = EqualPrimitivesFinder(equalPrimitives, 8);
            if (!eightFlat.Any()) return false;
            foreach (var candidateHex in eightFlat)
            {
                var candidateHexVal = equalPrimitives[candidateHex];
                var cos = new List<double>();
                var firstPrimNormal = ((Flat) candidateHexVal[0]).Normal;
                for (var i = 1; i < candidateHexVal.Count; i++)
                    cos.Add(firstPrimNormal.dotProduct(((Flat) candidateHexVal[i]).Normal));
                // if it is philips head, the cos list must have four 0, two -1 and one 1
                if (cos.Count(c => Math.Abs(0.0 - c) < 0.0001) != 4 ||
                    cos.Count(c => Math.Abs(-1 - c) < 0.0001) != 2 ||
                    cos.Count(c => Math.Abs(1 - c) < 0.0001) != 1) continue;
                var fastener = new Fastener
                {
                    Solid = solid,
                    FastenerType = FastenerTypeEnum.Bolt,
                    Tool = Tool.PhillipsBlade,
                    RemovalDirection =
                        RemovalDirectionFinderForAllenHexPhillips(candidateHexVal.Cast<Flat>().ToList(),
                            PartitioningSolid.OrientedBoundingBoxDic[solid])
                };
                Fasteners.Add(fastener);
                return true;
            }
            return false;
        }

        private static bool SlotHeadBolt(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<PrimitiveSurface, List<PrimitiveSurface>> equalPrimitives)
        {
            var twoFlat = EqualPrimitivesFinder(equalPrimitives, 2);
            if (!twoFlat.Any()) return false;
            foreach (var candidateHex in twoFlat)
            {
                var candidateHexVal = equalPrimitives[candidateHex];
                var cos = ((Flat) candidateHexVal[0]).Normal.dotProduct(((Flat) candidateHexVal[1]).Normal);
                if (!(Math.Abs(-1 - cos) < 0.0001)) continue;
                // I will add couple of conditions here:
                //    1. If the number of solid vertices in front of each flat is equal to another
                //    2. If the summation of the vertices in 1 is greater than the total # of verts
                //    3. and I also need to add some constraints for the for eample the area of the cylinder
                var leftVerts = VertsInfrontOfFlat(solid, (Flat) candidateHexVal[0]);
                var rightVerts = VertsInfrontOfFlat(solid, (Flat) candidateHexVal[1]);
                if (Math.Abs(leftVerts - rightVerts) > 2 || leftVerts + rightVerts <= solid.Vertices.Length)
                    return false;
                if (!solidPrim.Where(p => p is Cylinder).Cast<Cylinder>().Any(c => c.IsPositive)) return false;
                var fastener = new Fastener
                {
                    Solid = solid,
                    FastenerType = FastenerTypeEnum.Bolt,
                    Tool = Tool.FlatBlade,
                    RemovalDirection =
                        RemovalDirectionFinderForSlot(candidateHexVal.Cast<Flat>().ToList(),
                            solidPrim.Where(p => p is Flat).Cast<Flat>().ToList(),
                            PartitioningSolid.OrientedBoundingBoxDic[solid])
                };
                Fasteners.Add(fastener);
                return true;
            }
            return false;
        }

        private static bool PhillipsSlotComboHeadBolt(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<PrimitiveSurface, List<PrimitiveSurface>> equalPrimitives)
        {
            var fourFlat = EqualPrimitivesFinder(equalPrimitives, 4);
            if (fourFlat.Count < 2) return false;
            var eachSlot = 0;
            var flats = new List<PrimitiveSurface>();
            foreach (var candidateHex in fourFlat)
            {
                var candidateHexVal = equalPrimitives[candidateHex];
                var cos = new List<double>();
                var firstPrimNormal = ((Flat) candidateHexVal[0]).Normal;
                for (var i = 1; i < candidateHexVal.Count; i++)
                    cos.Add(firstPrimNormal.dotProduct(((Flat) candidateHexVal[i]).Normal));
                // if it is a slot and phillips combo the cos list must have two -1 and one 1
                // and this needs to appear 2 times.
                if (cos.Count(c => Math.Abs(-1 - c) < 0.0001) != 2 ||
                    cos.Count(c => Math.Abs(1 - c) < 0.0001) != 1) continue;
                if (!solidPrim.Where(p => p is Cylinder).Cast<Cylinder>().Any(c => c.IsPositive)) return false;
                flats.AddRange(candidateHexVal);
                eachSlot++;
            }
            if (eachSlot != 2) return false;
            var fastener = new Fastener
            {
                Solid = solid,
                FastenerType = FastenerTypeEnum.Bolt,
                Tool = Tool.PhillipsBlade,
                RemovalDirection =
                    RemovalDirectionFinderForAllenHexPhillips(flats.Cast<Flat>().ToList(),
                        PartitioningSolid.OrientedBoundingBoxDic[solid])
            };
            Fasteners.Add(fastener);
            return true;
        }


        private static bool ThreadDetector(TessellatedSolid solid, List<PrimitiveSurface> primitiveSurfaces)
        {
            // Consider these two cases:
            //      1. Threads are helix
            //      2. Threads are seperate cones
            if (ThreadsAreSeperateCones(solid, primitiveSurfaces))
                return true;
            return SolidHasHelix(solid);

        }

        private static bool ThreadsAreSeperateCones(TessellatedSolid solid, List<PrimitiveSurface> primitiveSurfaces)
        {
            var cones = primitiveSurfaces.Where(p => p is Cone).Cast<Cone>().ToList();
            foreach (var cone in cones.Where(c => c.Faces.Count > 30))
            {
                var threads =
                    cones.Where(
                        c =>
                            (Math.Abs(c.Axis.dotProduct(cone.Axis) - 1) < 0.001 ||
                             Math.Abs(c.Axis.dotProduct(cone.Axis) + 1) < 0.001) &&
                            (Math.Abs(c.Faces.Count - cone.Faces.Count) < 3) &&
                            (Math.Abs(c.Area - cone.Area) < 0.001) &&
                            (Math.Abs(c.Aperture - cone.Aperture) < 0.001)).ToList();
                if (threads.Count < 10) continue;
                if (ConeThreadIsInternal(threads))
                    Nuts.Add(new Nut {Solid = solid});
                Fasteners.Add(new Fastener
                {
                    Solid = solid,
                    RemovalDirection =
                        RemovalDirectionFinderUsingObb(solid, PartitioningSolid.OrientedBoundingBoxDic[solid])
                });
                return true;
            }
            return false;
        }

        private static bool SolidHasHelix(TessellatedSolid solid)
        {
            // Idea: find an edge which has an internal angle equal to one of the following cases.
            // This only works if at least one of outer or inner threads have a sharo edge.
            // take the connected edges (from both sides) which have the same feature.
            // If it rotates couple of times, it is a helix.
            // It seems to be expensive. Let's see how it goes.
            // Standard thread angles:
            //       60     55     29     45     30    80 
            foreach (var edge in solid.Edges.Where(e => Math.Abs(e.InternalAngle - 2.08566845) < 0.04))
                // 2.0943951 is equal to 120 degree
            {
                // To every side of the edge if there is one edge with the IA of 120, this edge is unique and we dcannot find the second one. 
                var visited = new HashSet<Edge> {edge};
                var stack = new Stack<Edge>();
                var possibleHelixEdges = FindHelixEdgesConnectedToAnEdge(solid.Edges, edge, visited);
                // It can have 0, 1 or 2 edges
                if (possibleHelixEdges == null) continue;
                foreach (var e in possibleHelixEdges)
                    stack.Push(e);

                while (stack.Any() && visited.Count < 1000)
                {
                    var e = stack.Pop();
                    visited.Add(e);
                    var cand = FindHelixEdgesConnectedToAnEdge(solid.Edges, e, visited);
                    // if yes, it will only have one edge.
                    if (cand == null) continue;
                    stack.Push(cand[0]);
                }
                if (visited.Count < 1000) // Is it very big?
                    continue;
                // if the thread is internal, classify it as nut, else fastener
                if (HelixThreadIsInternal(visited))
                    Nuts.Add(new Nut {Solid = solid});
                Fasteners.Add(new Fastener
                {
                    Solid = solid,
                    RemovalDirection =
                        RemovalDirectionFinderUsingObb(solid, PartitioningSolid.OrientedBoundingBoxDic[solid])
                });
                return true;
            }
            return false;
        }

        private static bool ConeThreadIsInternal(List<Cone> threads)
        {
            // If it is seperated cones, it's easy: negative cones make internal thread
            // To make it robust, if 70 percent of the cones are negative, it is internal
            var neg = threads.Count(cone => !cone.IsPositive);
            if (neg >= 0.7*threads.Count) return true;
            return false;
        }

        private static bool HelixThreadIsInternal(HashSet<Edge> helixEdges)
        {
            return false;
        }

        private static Edge[] FindHelixEdgesConnectedToAnEdge(Edge[] edges, Edge edge, HashSet<Edge> visited)
        {

            var m = new List<Edge>();
            var e1 =
                edges.Where(
                    e =>
                        (edge.From == e.From || edge.From == e.To) &&
                        Math.Abs(e.InternalAngle - 2.08566845) < 0.04 && !visited.Contains(e)).ToList();
            var e2 =
                edges.Where(
                    e =>
                        (edge.To == e.From || edge.To == e.To) &&
                        Math.Abs(e.InternalAngle - 2.08566845) < 0.04 && !visited.Contains(e)).ToList();
            if (!e1.Any() && !e2.Any()) return null;
            if (e1.Any()) m.Add(e1[0]);
            if (e2.Any()) m.Add(e2[0]);
            return m.ToArray();
        }


        private static Dictionary<TessellatedSolid, Dictionary<PrimitiveSurface, List<PrimitiveSurface>>>
            EqualFlatPrimitiveAreaFinder(List<TessellatedSolid> firstFilter,
                Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive)
        {
            var equalPrim = new Dictionary<TessellatedSolid, Dictionary<PrimitiveSurface, List<PrimitiveSurface>>>();
            foreach (var solid in firstFilter)
            {
                var primEqualArea = new Dictionary<PrimitiveSurface, List<PrimitiveSurface>>();
                foreach (var prim in solidPrimitive[solid].Where(p => p is Flat))
                {
                    var equalExist = primEqualArea.Keys.Where(p => Math.Abs(p.Area - prim.Area) < 0.01).ToList();
                    if (!equalExist.Any()) primEqualArea.Add(prim, new List<PrimitiveSurface> {prim});
                    else
                    {
                        foreach (var equal in equalExist)
                        {
                            primEqualArea[equal].Add(prim);
                            break;
                        }
                    }
                }
                equalPrim.Add(solid, primEqualArea);
            }
            return equalPrim;
        }


        private static List<PrimitiveSurface> EqualPrimitivesFinder(
            Dictionary<PrimitiveSurface, List<PrimitiveSurface>> equalPrimittives, int numberOfEqualPrim)
        {
            return equalPrimittives.Keys.Where(
                k => equalPrimittives[k].Count == numberOfEqualPrim).ToList();
        }

        private static int VertsInfrontOfFlat(TessellatedSolid solid, Flat flat)
        {
            return
                solid.Vertices.Count(
                    v => flat.Normal.dotProduct(v.Position.subtract(flat.Faces[0].Vertices[0].Position)) > 0);
        }

        private static double ToolSizeFinder(List<PrimitiveSurface> candidateHexVal)
        {
            var firstPrimNormal = ((Flat) candidateHexVal[0]).Normal;
            for (var i = 1; i < candidateHexVal.Count; i++)
            {
                if (Math.Abs(firstPrimNormal.dotProduct(((Flat) candidateHexVal[i]).Normal) + 1) > 0.0001) continue;
                return
                    Math.Abs(Math.Abs(candidateHexVal[0].Vertices[0].Position.dotProduct(firstPrimNormal)) -
                             Math.Abs(candidateHexVal[i].Vertices[0].Position.dotProduct(firstPrimNormal)));
            }
            return 0.0;
        }

        private static List<TessellatedSolid> SmallObjectsDetector(
            Dictionary<TessellatedSolid, List<TessellatedSolid>> solidRepeated)
        {
            var partSize = new Dictionary<TessellatedSolid, double>();
            var parts = solidRepeated.Keys.ToList();
            foreach (var solid in solidRepeated.Keys.ToList())
            {
                var shortestObbEdge = double.PositiveInfinity;
                var longestObbEdge = double.NegativeInfinity;
                var solidObb = PartitioningSolid.OrientedBoundingBoxDic[solid];
                for (var i = 1; i < solidObb.CornerVertices.Count(); i++)
                {
                    var dis =
                        Math.Sqrt(
                            Math.Pow(solidObb.CornerVertices[0].Position[0] - solidObb.CornerVertices[i].Position[0],
                                2.0) +
                            Math.Pow(solidObb.CornerVertices[0].Position[1] - solidObb.CornerVertices[i].Position[1],
                                2.0) +
                            Math.Pow(solidObb.CornerVertices[0].Position[2] - solidObb.CornerVertices[i].Position[2],
                                2.0));
                    if (dis < shortestObbEdge) shortestObbEdge = dis;
                    if (dis > longestObbEdge) longestObbEdge = dis;
                }
                var sizeMetric = solid.Volume*(longestObbEdge/shortestObbEdge);
                partSize.Add(solid, sizeMetric);
            }
            // if removing the first 10 percent drops the max size by 95 percent, consider them as noise: 
            var maxSize = partSize.Values.Max();
            var sortedPartSize = partSize.ToList();
            sortedPartSize.Sort((x, y) => y.Value.CompareTo(x.Value));

            var noise = new List<TessellatedSolid>();
            for (var i = 0; i < Math.Ceiling(partSize.Count*5/100.0); i++)
                noise.Add(sortedPartSize[i].Key);
            var approvedNoise = new List<TessellatedSolid>();
            for (var i = 0; i < noise.Count; i++)
            {
                var newList = new List<TessellatedSolid>();
                for (var j = 0; j < i + 1; j++)
                    newList.Add(noise[j]);
                var max =
                    partSize.Where(a => !newList.Contains(a.Key))
                        .ToDictionary(key => key.Key, value => value.Value)
                        .Values.Max();
                if (max > maxSize*10.0/100.0) continue;
                approvedNoise = newList;
                break;
            }
            maxSize = partSize.Where(a => !approvedNoise.Contains(a.Key))
                .ToDictionary(key => key.Key, value => value.Value)
                .Values.Max();

            // If I have detected a portion of the fasteners (a very good number of them possibly) by this point,
            // it can accellerate the primitive classification. If it is fastener, it doesn't need to be classified?

            // creating the dictionary:
            var n = 99.0; // number of classes
            var dic = new Dictionary<double, List<TessellatedSolid>>();

            // Filling up the keys
            var minSize = partSize.Where(a => !approvedNoise.Contains(a.Key))
                .ToDictionary(key => key.Key, value => value.Value)
                .Values.Min();
            var smallestSolid = partSize.Keys.Where(s => partSize[s] == minSize).ToList();

            for (var i = 0; i < n; i++)
            {
                var ini = new List<TessellatedSolid>();
                var key = minSize + (i/(n - 1))*(maxSize - minSize);
                dic.Add(key, ini);
            }

            dic[minSize].AddRange(smallestSolid);
            dic[maxSize].AddRange(approvedNoise);

            // Filling up the values
            var keys = dic.Keys.ToList();
            foreach (var f in parts.Where(a => !approvedNoise.Contains(a)))
            {
                for (var i = 0; i < n - 2; i++)
                {
                    if (partSize[f] >= keys[i] && partSize[f] <= keys[i + 1])
                    {
                        if (Math.Abs(partSize[f] - keys[i]) > Math.Abs(partSize[f] - keys[i + 1]))
                            dic[keys[i + 1]].Add(f);
                        else dic[keys[i]].Add(f);
                    }
                }
            }
            //foreach (var v in dic[minSize])
            //    Console.WriteLine(v.Name);//partSize[v]/maxSize);
            return dic[minSize];
        }

        private static int RemovalDirectionFinderForSlot(List<Flat> equalFlats, List<Flat> flats,
            BoundingBox boundingBox)
        {
            // for the slot, there will be a flat that is prependicular to both equal flats.
            // this can give mistakably the sides of the bolt head. 
            // solution: both equal flats 2are on the opposite side of the flat
            var prependicularFlats =
                flats.Where(f => Math.Abs(f.Normal.dotProduct(equalFlats[0].Normal)) < 0.0001).ToList();
            var equalFlatsVerts = equalFlats.SelectMany(f => f.Vertices).ToList();
            double[] normal = null;
            foreach (var prep in prependicularFlats)
            {
                if (equalFlatsVerts.Any(v => prep.Normal.dotProduct(v.Position.subtract(prep.Vertices[0].Position)) < 0))
                    continue;
                normal = prep.Normal;
                break;
            }
            var equInDirections =
                DisassemblyDirections.Directions.Where(d => Math.Abs(d.dotProduct(normal) - 1) < 0.001).ToList()[0];
            return DisassemblyDirections.Directions.IndexOf(equInDirections);
        }

        private static int RemovalDirectionFinderForAllenHexPhillips(List<Flat> flatPrims, BoundingBox solid)
        {
            // This function works for hex bolt, alle, philips and phillips and slot combo
            // For slot, it will be different
            var normalGuess = NormalGuessFinder(flatPrims);
            var equInDirections =
                DisassemblyDirections.Directions.Where(d => Math.Abs(d.dotProduct(normalGuess) - 1) < 0.001).ToList()[0];
            // find the furthest vertex (b) to a vertex (a) from allen faces. if
            // Normal.dotproduct(a-b) must be positive. If it was negative, return 
            // multiply(-1)
            var a = flatPrims[0].Vertices[0];
            var farthestVer = flatPrims[0].Vertices[0];
            var dist = 0.0;
            foreach (var ver in solid.CornerVertices)
            {
                var locDist = BasicGeometryFunctions.DistanceBetweenTwoVertices(a.Position, ver.Position);
                if (locDist <= dist) continue;
                farthestVer = ver;
                dist = locDist;
            }
            var reference = a.Position.subtract(farthestVer.Position);
            if (normalGuess.dotProduct(reference) > 0)
                return DisassemblyDirections.Directions.IndexOf(equInDirections);
            return DisassemblyDirections.Directions.IndexOf(equInDirections.multiply(-1.0));
        }

        private static int RemovalDirectionFinderUsingObb(TessellatedSolid solid, BoundingBox obb)
        {
            // this is hard. it can be a simple threaded rod,or it can be a standard
            // bolt that could not be detected by other approaches.
            // If it is a rod, the removal direction is not really important,
            // but if not, it's important I still have no idea about how to detect it.
            // Idea: find any of the 4 longest sides of the OBB.
            //       find the closest vertex to this side. (if it's a rod,
            //       the closest vertex can be everywhere, but in a regular bolt,
            //       it's on the top (it's most definitely a vertex from the head))
            PolygonalFace facePrepToRD1;
            PolygonalFace facePrepToRD2;
            var longestPlane = LongestPlaneOfObbDetector(obb, out facePrepToRD1, out facePrepToRD2);
            TVGL.Vertex closestVerToPlane = null;
            var minDist = double.PositiveInfinity;
            foreach (var ver in solid.ConvexHullVertices)
            {
                var dis = DistanceBetweenVertexAndPlane(ver, longestPlane[0]);
                if (dis >= minDist)
                    continue;
                closestVerToPlane = ver;
                minDist = dis;
            }
            // The closest vertex to plane is closer to which facePrepToRD?
            double[] normalGuess = null;
            if (DistanceBetweenVertexAndPlane(closestVerToPlane, facePrepToRD1) <
                DistanceBetweenVertexAndPlane(closestVerToPlane, facePrepToRD2))
                normalGuess = facePrepToRD1.Normal;
            else
                normalGuess = facePrepToRD2.Normal;
            var equInDirections =
                DisassemblyDirections.Directions.Where(d => Math.Abs(d.dotProduct(normalGuess) - 1) < 0.001).ToList()[0];
            return DisassemblyDirections.Directions.IndexOf(equInDirections);
        }

        internal static PolygonalFace[] LongestPlaneOfObbDetector(BoundingBox obb, out PolygonalFace facePrepToRD1,
            out PolygonalFace facePrepToRD2)
        {
            // it returns longest side. (two adjacent triangles)
            var dis1 = BasicGeometryFunctions.DistanceBetweenTwoVertices(obb.CornerVertices[0].Position, obb.CornerVertices[1].Position);
            var dis2 = BasicGeometryFunctions.DistanceBetweenTwoVertices(obb.CornerVertices[0].Position, obb.CornerVertices[2].Position);
            var dis3 = BasicGeometryFunctions.DistanceBetweenTwoVertices(obb.CornerVertices[0].Position, obb.CornerVertices[4].Position);
            if (dis1 >= dis2 && dis1 >= dis3)
            {
                facePrepToRD1 =
                    new PolygonalFace(new[] {obb.CornerVertices[0], obb.CornerVertices[2], obb.CornerVertices[4]},
                        ((obb.CornerVertices[4].Position.subtract(obb.CornerVertices[0].Position)).crossProduct(
                            obb.CornerVertices[2].Position.subtract(obb.CornerVertices[0].Position))).normalize());
                facePrepToRD2 =
                    new PolygonalFace(new[] {obb.CornerVertices[1], obb.CornerVertices[3], obb.CornerVertices[7]},
                        ((obb.CornerVertices[7].Position.subtract(obb.CornerVertices[3].Position)).crossProduct(
                            obb.CornerVertices[1].Position.subtract(obb.CornerVertices[3].Position))).normalize());
                return new[]
                {
                    new PolygonalFace(new[] {obb.CornerVertices[0], obb.CornerVertices[1], obb.CornerVertices[2]},
                        ((obb.CornerVertices[2].Position.subtract(obb.CornerVertices[0].Position)).crossProduct(
                            obb.CornerVertices[1].Position.subtract(obb.CornerVertices[0].Position))).normalize()),
                    new PolygonalFace(new[] {obb.CornerVertices[1], obb.CornerVertices[2], obb.CornerVertices[3]},
                        ((obb.CornerVertices[1].Position.subtract(obb.CornerVertices[3].Position)).crossProduct(
                            obb.CornerVertices[2].Position.subtract(obb.CornerVertices[3].Position))).normalize())
                };
            }
            if (dis2 >= dis1 && dis2 >= dis3)
            {
                facePrepToRD1 =
                    new PolygonalFace(new[] {obb.CornerVertices[1], obb.CornerVertices[0], obb.CornerVertices[4]},
                        ((obb.CornerVertices[1].Position.subtract(obb.CornerVertices[0].Position)).crossProduct(
                            obb.CornerVertices[4].Position.subtract(obb.CornerVertices[0].Position))).normalize());
                facePrepToRD2 =
                    new PolygonalFace(new[] {obb.CornerVertices[2], obb.CornerVertices[3], obb.CornerVertices[7]},
                        ((obb.CornerVertices[2].Position.subtract(obb.CornerVertices[3].Position)).crossProduct(
                            obb.CornerVertices[7].Position.subtract(obb.CornerVertices[3].Position))).normalize());
                return new[]
                {
                    new PolygonalFace(new[] {obb.CornerVertices[0], obb.CornerVertices[1], obb.CornerVertices[2]},
                        ((obb.CornerVertices[2].Position.subtract(obb.CornerVertices[0].Position)).crossProduct(
                            obb.CornerVertices[1].Position.subtract(obb.CornerVertices[0].Position))).normalize()),
                    new PolygonalFace(new[] {obb.CornerVertices[1], obb.CornerVertices[2], obb.CornerVertices[3]},
                        ((obb.CornerVertices[1].Position.subtract(obb.CornerVertices[3].Position)).crossProduct(
                            obb.CornerVertices[2].Position.subtract(obb.CornerVertices[3].Position))).normalize())
                };
            }
            if (dis3 >= dis2 && dis3 >= dis1)
            {
                facePrepToRD1 =
                    new PolygonalFace(new[] {obb.CornerVertices[0], obb.CornerVertices[1], obb.CornerVertices[2]},
                        ((obb.CornerVertices[2].Position.subtract(obb.CornerVertices[0].Position)).crossProduct(
                            obb.CornerVertices[1].Position.subtract(obb.CornerVertices[0].Position))).normalize());
                facePrepToRD2 =
                    new PolygonalFace(new[] {obb.CornerVertices[4], obb.CornerVertices[5], obb.CornerVertices[6]},
                        ((obb.CornerVertices[5].Position.subtract(obb.CornerVertices[4].Position)).crossProduct(
                            obb.CornerVertices[6].Position.subtract(obb.CornerVertices[4].Position))).normalize());
                return new[]
                {
                    new PolygonalFace(new[] {obb.CornerVertices[1], obb.CornerVertices[0], obb.CornerVertices[4]},
                        ((obb.CornerVertices[1].Position.subtract(obb.CornerVertices[0].Position)).crossProduct(
                            obb.CornerVertices[4].Position.subtract(obb.CornerVertices[0].Position))).normalize()),
                    new PolygonalFace(new[] {obb.CornerVertices[1], obb.CornerVertices[5], obb.CornerVertices[4]},
                        ((obb.CornerVertices[4].Position.subtract(obb.CornerVertices[5].Position)).crossProduct(
                            obb.CornerVertices[1].Position.subtract(obb.CornerVertices[5].Position))).normalize())
                };
            }
            facePrepToRD1 = null;
            facePrepToRD2 = null;
            return null;
        }

        internal static PolygonalFace[] LongestPlaneOfObbDetector(double[][] obb, bool clockWise, out PolygonalFace facePrepToRD1,
            out PolygonalFace facePrepToRD2)
        {
            // it returns longest side. (two adjacent triangles)
            var dis1 = BasicGeometryFunctions.DistanceBetweenTwoVertices(obb[0], obb[1]);
            var dis2 = BasicGeometryFunctions.DistanceBetweenTwoVertices(obb[0], obb[3]);
            var dis3 = BasicGeometryFunctions.DistanceBetweenTwoVertices(obb[0], obb[4]);
            var a = clockWise ? 10 : 20;
            if (dis1 >= dis2 && dis1 >= dis3)
            {
                var normal1 = ((obb[3].subtract(obb[0])).crossProduct(obb[4].subtract(obb[0]))).normalize();
                facePrepToRD1 =
                    new PolygonalFace(new[] {new TVGL.Vertex(obb[0]), new TVGL.Vertex(obb[3]), new TVGL.Vertex(obb[4])},
                        clockWise ? normal1.multiply(-1.0) : normal1);
                var normal2 = ((obb[5].subtract(obb[1])).crossProduct(obb[2].subtract(obb[1]))).normalize();
                facePrepToRD2 =
                    new PolygonalFace(new[] { new TVGL.Vertex(obb[1]), new TVGL.Vertex(obb[2]), new TVGL.Vertex(obb[5]) },
                        clockWise ? normal2.multiply(-1.0) : normal2);
                var normal3 = ((obb[1].subtract(obb[0])).crossProduct(obb[3].subtract(obb[0]))).normalize();
                var normal4 = ((obb[3].subtract(obb[2])).crossProduct(obb[1].subtract(obb[2]))).normalize();
                return new[]
                {
                    new PolygonalFace(new[] {new TVGL.Vertex(obb[0]), new TVGL.Vertex(obb[1]), new TVGL.Vertex(obb[3])},
                        clockWise ? normal3.multiply(-1.0) : normal3),
                    new PolygonalFace(new[] {new TVGL.Vertex(obb[1]), new TVGL.Vertex(obb[2]), new TVGL.Vertex(obb[3])},
                        clockWise ? normal4.multiply(-1.0) : normal4)
                };
            }
            if (dis2 >= dis1 && dis2 >= dis3)
            {
                var normal1 = ((obb[4].subtract(obb[0])).crossProduct(obb[1].subtract(obb[0]))).normalize();
                facePrepToRD1 =
                    new PolygonalFace(new[] { new TVGL.Vertex(obb[0]), new TVGL.Vertex(obb[1]), new TVGL.Vertex(obb[4]) },
                        clockWise ? normal1.multiply(-1.0) : normal1);
                var normal2 = ((obb[2].subtract(obb[3])).crossProduct(obb[7].subtract(obb[3]))).normalize();
                facePrepToRD2 =
                    new PolygonalFace(new[] { new TVGL.Vertex(obb[2]), new TVGL.Vertex(obb[3]), new TVGL.Vertex(obb[7]) },
                        clockWise ? normal2.multiply(-1.0) : normal2);
                var normal3 = ((obb[1].subtract(obb[0])).crossProduct(obb[3].subtract(obb[0]))).normalize();
                var normal4 = ((obb[3].subtract(obb[2])).crossProduct(obb[1].subtract(obb[2]))).normalize();
                return new[]
                {
                    new PolygonalFace(new[] {new TVGL.Vertex(obb[0]), new TVGL.Vertex(obb[1]), new TVGL.Vertex(obb[3])},
                        clockWise ? normal3.multiply(-1.0) : normal3),
                    new PolygonalFace(new[] {new TVGL.Vertex(obb[1]), new TVGL.Vertex(obb[2]), new TVGL.Vertex(obb[3])},
                        clockWise ? normal4.multiply(-1.0) : normal4)
                };
            }
            if (dis3 >= dis2 && dis3 >= dis1)
            {
                var normal1 = ((obb[1].subtract(obb[0])).crossProduct(obb[3].subtract(obb[0]))).normalize();
                facePrepToRD1 =
                    new PolygonalFace(new[] { new TVGL.Vertex(obb[0]), new TVGL.Vertex(obb[1]), new TVGL.Vertex(obb[3]) },
                        clockWise ? normal1.multiply(-1.0) : normal1);
                var normal2 = ((obb[7].subtract(obb[4])).crossProduct(obb[5].subtract(obb[4]))).normalize();
                facePrepToRD2 =
                    new PolygonalFace(new[] { new TVGL.Vertex(obb[5]), new TVGL.Vertex(obb[4]), new TVGL.Vertex(obb[7]) },
                        clockWise ? normal2.multiply(-1.0) : normal2);
                var normal3 =  ((obb[4].subtract(obb[0])).crossProduct(obb[1].subtract(obb[0]))).normalize();
                var normal4 = ((obb[1].subtract(obb[5])).crossProduct(obb[4].subtract(obb[5]))).normalize();
                return new[]
                {
                    new PolygonalFace(new[] {new TVGL.Vertex(obb[0]), new TVGL.Vertex(obb[1]), new TVGL.Vertex(obb[4])},
                       clockWise ? normal3.multiply(-1.0) : normal3),
                    new PolygonalFace(new[] {new TVGL.Vertex(obb[1]), new TVGL.Vertex(obb[4]), new TVGL.Vertex(obb[5])},
                        clockWise ? normal4.multiply(-1.0) : normal4)
                };
            }
            facePrepToRD1 = null;
            facePrepToRD2 = null;
            return null;
        }

        private static double[] NormalGuessFinder(List<Flat> flatPrims)
        {
            // We need two flats that are not parallel to each other.
            // their dot is not 1 or -1
            var reference = flatPrims[0];
            var second = flatPrims[1];
            for (var i = 1; i < flatPrims.Count; i++)
            {
                if (Math.Abs(Math.Abs(flatPrims[i].Normal.dotProduct(reference.Normal)) - 1) < 0.004)
                    //equal to 6 degrees 
                    continue;
                second = flatPrims[i];
            }
            return reference.Normal.crossProduct(second.Normal).normalize();
        }

        private static double DistanceBetweenVertexAndPlane(TVGL.Vertex ver, PolygonalFace plane)
        {
            // create a vector from a vertex on plane to the ver
            var vector = ver.Position.subtract(plane.Vertices[0].Position);
            // distance is the dot product of the normal and the vector:
            return Math.Abs(vector.dotProduct(plane.Normal));
        }

        private static double[] BoltCenterLine(List<PrimitiveSurface> primitiveSurfaces)
        {
            // the center line of the screw CAN be the axis of the largest cylinder. I have looked at several test cases
            // and this rule works for almost all of them. 
            var maxCountFace = 0;
            var finalCenterline = new double[3];
            foreach (Cylinder cylinder in primitiveSurfaces.Where(prim => prim is Cylinder))
            {
                if (cylinder.Faces.Count > maxCountFace)
                    finalCenterline = cylinder.Axis;
            }
            return finalCenterline;
        }

        internal static Dictionary<TessellatedSolid, double[]> GearDetector(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive)
        {
            var gears = new Dictionary<TessellatedSolid, double[]>();
            foreach (var solid in solidPrimitive.Keys)
            {
                var gear = false;
                var flats =
                    solidPrimitive[solid].Where(
                        p => p is Flat && p.Faces.Count > BoltAndGearConstants.TriabglesInTheGearSideFaces).ToList();

                foreach (var flatPrim in flats)
                {
                    var outerGearEdges = GearEdge.FromTVGLEdgeClassToGearEdgeClass(flatPrim.OuterEdges);
                    var patches = SortedConnectedPatches(outerGearEdges);
                    foreach (var patch in patches)
                    {
                        var cluster = new List<GearEdge>[2];
                        var newPatch = new List<GearEdge>();
                        if (ClusteringDenseAndSparseEdges.ContainsDense(patch))
                            cluster = ClusteringDenseAndSparseEdges.ClusteringDenseSparse(patch);
                        if (cluster[0] != null && cluster[1].Count > BoltAndGearConstants.AcceptableNumberOfDenseEdges)
                            newPatch = ClusteringDenseAndSparseEdges.ReplacingDenseEdges(patch, cluster);
                        var crossP = new List<double[]>();
                        if (newPatch.Count == 0) newPatch = patch;
                        for (var i = 0; i < newPatch.Count - 1; i++)
                        {
                            var cross = new[] {0.0, 0, 0};
                            var vec1 = newPatch[i].Vector.normalize();
                            var vec2 = newPatch[i + 1].Vector.normalize();
                            if (SmoothAngle(vec1, vec2))
                                continue;
                            cross = vec1.crossProduct(vec2);
                            crossP.Add(cross);
                        }
                        if (crossP.Count < 10) continue;
                        var crossSign = BoltAndGearUpdateFunctions.ConvertCrossToSign(crossP);
                        if (!IsGear(crossSign)) continue;
                        Console.WriteLine("Is " + solid.Name + " a gear? 'y' or 'n'");
                        var read = Convert.ToString(Console.ReadLine());
                        if (read == "n")
                            continue;
                        gear = true;
                        gears.Add(solid, flatPrim.Faces[0].Normal);
                        break;
                    }
                    if (gear) break;
                }
            }
            return gears;
        }

        private static bool IsGear(List<int> crossSign)
        {
            crossSign = BoltAndGearUpdateFunctions.CrossUpdate(crossSign);
            var isGear = true;
            var counter = 0;
            var startInd = 0;
            for (var i = 0; i < crossSign.Count; i++)
            {
                if (crossSign[i] == crossSign[i + 1])
                {
                    startInd = i;
                    break;
                }
            }
            for (var i = startInd; i < crossSign.Count - 5; i += 2)
            {
                if (crossSign[i] == crossSign[i + 1] && crossSign[i] != crossSign[i + 2])
                {
                    counter++;
                    continue;
                }
                isGear = false;
                break;
            }
            if (isGear && counter > BoltAndGearConstants.GearTeeth)
                return true;
            return false;
        }

        private static List<List<GearEdge>> SortedConnectedPatches(List<GearEdge> outerEdges)
        {
            var outer = new List<GearEdge>(outerEdges);
            var patches = new List<List<GearEdge>>();
            var c = -1;

            while (outer.Count > 0)
            {
                c++;
                patches.Add(new List<GearEdge>());
                var ind = outerEdges.IndexOf(outer[0]);
                patches[c].Add(outerEdges[ind]);
                outer.Remove(outer[0]);
                var count1 = patches[c].Count;
                var count2 = 0;
                while (count1 != count2)
                {
                    count1 = patches[c].Count;
                    var ed1 = patches[c][patches[c].Count - 1];
                    for (var j = 0; j < outer.Count; j++)
                    {
                        var ed2 = outer[j];
                        //if (patches[c].Contains(ed2)) continue;
                        if (ed1.To != ed2.From && ed1.To != ed2.To)
                            continue;
                        var copy = outerEdges.Where(e => e == ed2).ToList()[0];
                        patches[c].Add(copy);
                        count2 = patches[c].Count;
                        outer.Remove(ed2);
                        var last = patches[c][patches[c].Count - 1];
                        if (ed1.To == last.To)
                        {
                            var tem = last.To;
                            last.To = last.From;
                            last.From = tem;
                            last.Vector = last.Vector.multiply(-1);
                        }

                        break;
                    }
                    if (count2 == 0) break;
                }
            }
            return patches;
        }

        private static bool SmoothAngle(double[] vec1, double[] vec2)
        {
            return Math.Abs(vec1.dotProduct(vec2)) > BoltAndGearConstants.SmoothAngle;
        }
    }
}
