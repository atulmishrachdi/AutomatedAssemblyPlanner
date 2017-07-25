using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using StarMathLib;

namespace Assembly_Planner
{
    internal class AutoThreadedFastenerDetection
    {
        internal static void Run(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs)
        {
            // This is mostly similar to the auto fastener detection with no thread, but instead of learning
            // from the area of the cylinder and for example flat, we will learn from the number of faces.
            // why? because if we have thread, we will have too many triangles. And this can be useful.
            // I can also detect helix and use this to detect the threaded fasteners

            // Important: if the fasteners are threaded using solidworks Fastener toolbox, it will not
            //            have helix. The threads will be small cones with the same axis and equal area.
            var width = 55;
            var check = 0;
            LoadingBar.start(width, 0);

            var smallParts = FastenerDetector.SmallObjectsDetector(DisassemblyDirectionsWithFastener.PartsWithOneGeom);

            PreSelectedFastenerToFastenerClass(solidPrimitive, multipleRefs);
            foreach (
                var preSelected in
                    FastenerDetector.PreSelectedFasteners.Where(preSelected => !smallParts.Contains(preSelected)))
                smallParts.Add(preSelected);

            FastenerDetector.PotentialFastener = new Dictionary<TessellatedSolid, double>();
            foreach (var p in smallParts)
                FastenerDetector.PotentialFastener.Add(p, 0.1);
            var groupedPotentialFasteners = FastenerDetector.GroupingSmallParts(smallParts);
            var uniqueParts = new HashSet<TessellatedSolid>();
            foreach (var s in multipleRefs.Keys.Where(FastenerDetector.PotentialFastener.Keys.Contains))
                uniqueParts.Add(s);
            foreach (
                var preFastener in
                    FastenerDetector.Fasteners.Where(preFastener => uniqueParts.Contains(preFastener.Solid)))
                uniqueParts.Remove(preFastener.Solid);

            var equalPrimitivesForEveryUniqueSolid = FastenerDetector.EqualFlatPrimitiveAreaFinder(uniqueParts,
                solidPrimitive);
            List<int> learnerVotes;
            var learnerWeights = FastenerPerceptronLearner.ReadingLearnerWeightsAndVotesFromCsv(out learnerVotes);
            FastenerGaussianNaiveBayes.GNB();


            List<string> nameList = new List<string>();
            foreach (var part in uniqueParts)
            {
                nameList.Add(part.Name);
            }
            float nameRating;

            float ratingAverage = 0;
            float ratingMax = -1;
            float ratingMin = 100000000000;
            int preCutoff = 0;
            int postCutoff = 0;
            PartNameAnalysis.findCommonPreSuffixes(nameList, ref preCutoff, ref postCutoff);
            foreach (var part in uniqueParts)
            {
                nameRating = PartNameAnalysis.SolidHasFastenerKeyword(part, preCutoff, postCutoff);
                ratingAverage += nameRating;
                ratingMax = Math.Max(ratingMax, nameRating);
                ratingMin = Math.Min(ratingMin, nameRating);
            }
            float proportion = 1 - (ratingMax - ratingMin) / 3;

            var refresh = (int)Math.Ceiling((float)uniqueParts.Count / (float)(width * 4));
            Parallel.ForEach(uniqueParts, solid =>
            //foreach (var solid in uniqueParts)
            {
                if (check % refresh == 0)
                {
                    LoadingBar.refresh(width, ((float)check) / ((float)uniqueParts.Count));
                }
                check++;

                var initialCertainty = FastenerGaussianNaiveBayes.GaussianNaiveBayesClassifier(solidPrimitive[solid], solid);

                nameRating = (PartNameAnalysis.SolidHasFastenerKeyword(solid, preCutoff, postCutoff) - ratingMin) / (0.001f + ratingMax - ratingMin);

                FastenerDetector.PotentialFastener[solid] = (0.1 + initialCertainty) * proportion + (1 - nameRating) * (1 - proportion);
                foreach (var up in multipleRefs[solid])
                    FastenerDetector.PotentialFastener[up] = (0.1 + initialCertainty) * proportion + (1 - nameRating) * (1 - proportion);

                // if a fastener is detected using polynomial trend approach, it is definitely a fastener but not a nut.
                // if it is detected using any other approach, but not polynomial trend, it is a possible nut.
                double toolSize;
                var commonHead = CommonHeadCheck(solid, solidPrimitive[solid], equalPrimitivesForEveryUniqueSolid[solid],
                    out toolSize);
                if (commonHead != 0)
                {
                    var newFastener = FastenerPolynomialTrend.PolynomialTrendDetector(solid);
                    if (newFastener != null)
                    {
                        newFastener.ToolSize = toolSize;
                        if (commonHead == 1)
                        {
                            newFastener.Tool = Tool.HexWrench;
                            lock (FastenerDetector.Fasteners)
                                FastenerDetector.Fasteners.Add(newFastener);
                            AddRepeatedSolidstoFasteners(newFastener, multipleRefs[solid]);
                            //continue;
                            return;
                        }
                        if (commonHead == 2)
                        {
                            newFastener.Tool = Tool.Allen;
                            lock (FastenerDetector.Fasteners)
                                FastenerDetector.Fasteners.Add(newFastener);
                            AddRepeatedSolidstoFasteners(newFastener, multipleRefs[solid]);
                            //continue;
                            return;
                        }
                        if (commonHead == 3)
                        {
                            newFastener.Tool = Tool.PhillipsBlade;
                            lock (FastenerDetector.Fasteners)
                                FastenerDetector.Fasteners.Add(newFastener);
                            AddRepeatedSolidstoFasteners(newFastener, multipleRefs[solid]);
                            //continue;
                            return;
                        }
                        if (commonHead == 4)
                        {
                            newFastener.Tool = Tool.FlatBlade;
                            lock (FastenerDetector.Fasteners)
                                FastenerDetector.Fasteners.Add(newFastener);
                            AddRepeatedSolidstoFasteners(newFastener, multipleRefs[solid]);
                            //continue;
                            return;
                        }
                        if (commonHead == 5)
                        {
                            newFastener.Tool = Tool.PhillipsBlade;
                            lock (FastenerDetector.Fasteners)
                                FastenerDetector.Fasteners.Add(newFastener);
                            AddRepeatedSolidstoFasteners(newFastener, multipleRefs[solid]);
                            //continue;
                            return;
                        }
                    }
                    else // can be a nut
                    {
                        if (commonHead == 1)
                        {
                            FastenerDetector.Nuts.Add(new Nut
                            {
                                Solid = solid,
                                NutType = NutType.Hex,
                                Tool = Tool.HexWrench,
                                ToolSize = toolSize,
                                OverallLength = BoundingGeometry.BoundingCylinderDic[solid].Length,
                                Certainty = initialCertainty // 0.9
                            });
                            foreach (var repeatedSolid in multipleRefs[solid])
                            {
                                lock (FastenerDetector.Nuts)
                                    FastenerDetector.Nuts.Add(new Nut
                                    {
                                        Solid = repeatedSolid,
                                        NutType = NutType.Hex,
                                        Tool = Tool.HexWrench,
                                        ToolSize = toolSize,
                                        OverallLength = BoundingGeometry.BoundingCylinderDic[solid].Length,
                                        Certainty = initialCertainty // 0.9
                                    });
                            }
                            //continue;
                            return;
                        }
                    }
                }
                if (FastenerPerceptronLearner.FastenerPerceptronClassifier(solidPrimitive[solid], solid, learnerWeights,
                    learnerVotes))
                {
                    var newFastener = FastenerPolynomialTrend.PolynomialTrendDetector(solid);
                    if (newFastener != null)
                    {
                        lock (FastenerDetector.Fasteners)
                            FastenerDetector.Fasteners.Add(newFastener);
                        AddRepeatedSolidstoFasteners(newFastener, multipleRefs[solid]);
                        //continue;
                        return;
                    }
                    // can be a nut
                    // use bounding cylinder to detect nuts.
                    // Since the nuts are small, the OBB function doesnt work accurately for them.
                    //  So, I cannot really trust this. 
                    var newNut = NutPolynomialTrend.PolynomialTrendDetector(solid);
                    if (newNut != null)
                    // It is a nut with certainty == 1
                    {
                        lock (FastenerDetector.Nuts)
                            FastenerDetector.Nuts.Add(newNut);
                        AddRepeatedSolidstoNuts(newNut, multipleRefs[solid]);
                        //continue;
                        return;
                    }
                    // still can be a nut since the upper approach is not really accurate
                    // this 50 percent certainty can go up if the nut is mated with a 
                    // detected fastener 
                    foreach (var repeatedSolid in multipleRefs[solid])
                    {
                        lock (FastenerDetector.Nuts)
                            FastenerDetector.Nuts.Add(new Nut
                            {
                                Solid = repeatedSolid,
                                Diameter = BoundingGeometry.BoundingCylinderDic[solid].Radius * 2.0,
                                // this is approximate
                                OverallLength = BoundingGeometry.BoundingCylinderDic[solid].Length,
                                Certainty = initialCertainty // 0.5
                            });
                    }
                    //continue;
                    return;
                }
                // if it is not captured by any of the upper methods, give it another chance:
                if (ThreadDetector(solid, solidPrimitive[solid]))
                {
                    var newFastener = FastenerPolynomialTrend.PolynomialTrendDetector(solid);
                    if (newFastener != null)
                    {
                        newFastener.FastenerType = FastenerTypeEnum.Bolt;
                        lock (FastenerDetector.Fasteners)
                            FastenerDetector.Fasteners.Add(newFastener);
                        AddRepeatedSolidstoFasteners(newFastener, multipleRefs[solid]);
                        //continue;
                        return;
                    }
                    //if not, it is a nut:
                    foreach (var repeatedSolid in multipleRefs[solid])
                    {
                        lock (FastenerDetector.Nuts)
                            FastenerDetector.Nuts.Add(new Nut
                            {
                                Solid = repeatedSolid,
                                Diameter = BoundingGeometry.BoundingCylinderDic[solid].Radius * 2.0,
                                // this is approximate
                                OverallLength = BoundingGeometry.BoundingCylinderDic[solid].Length * 2.0,
                                Certainty = initialCertainty // 0.5
                            });
                    }
                }
            }
                ); //
            // now use groupped small objects:
            AutoNonthreadedFastenerDetection.ConnectFastenersNutsAndWashers(groupedPotentialFasteners);
            LoadingBar.refresh(width, 1);
        }

        private static void PreSelectedFastenerToFastenerClass(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs)
        {
            foreach (var preFastener in FastenerDetector.PreSelectedFasteners)
            {
                // if this part is repeated, add also those repeated parts to the fastener list
                var repeated = new HashSet<TessellatedSolid>();
                var isAKeyInMultipleRefs = multipleRefs.Keys.Where(r => r == preFastener).ToList();
                if (isAKeyInMultipleRefs.Any())
                    repeated = new HashSet<TessellatedSolid>(multipleRefs[isAKeyInMultipleRefs[0]]) { preFastener };
                else
                {
                    // it is a part in a value list
                    foreach (var key in multipleRefs.Keys)
                    {
                        if (!multipleRefs[key].Contains(preFastener)) continue;
                        repeated = new HashSet<TessellatedSolid>(multipleRefs[key]) { key };
                    }
                }
                var newFastener = FastenerPolynomialTrend.PolynomialTrendDetector(preFastener);
                if (newFastener != null)
                {
                    foreach (var repeatedSolid in repeated)
                    {
                        FastenerDetector.Fasteners.Add(new Fastener
                        {
                            Solid = repeatedSolid,
                            NumberOfThreads = newFastener.NumberOfThreads,
                            FastenerType = newFastener.FastenerType,
                            RemovalDirection = newFastener.RemovalDirection,
                            OverallLength = newFastener.OverallLength,
                            EngagedLength = newFastener.EngagedLength,
                            Diameter = newFastener.Diameter,
                            Certainty = 1.0
                        });
                    }
                    return;
                }
                var overalLength =
                    GeometryFunctions.SortedLengthOfObbEdges(BoundingGeometry.OrientedBoundingBoxDic[preFastener])[2];
                foreach (var repeatedSolid in repeated)
                {
                    FastenerDetector.Fasteners.Add(new Fastener
                    {
                        Solid = repeatedSolid,
                        FastenerType = FastenerTypeEnum.Bolt,
                        RemovalDirection =
                            FastenerDetector.RemovalDirectionFinderUsingObb(preFastener,
                                BoundingGeometry.OrientedBoundingBoxDic[preFastener]),
                        OverallLength = overalLength,
                        EngagedLength = overalLength,
                        Diameter = BoundingGeometry.BoundingCylinderDic[repeatedSolid].Radius,
                        Certainty = 1.0
                    });
                }
            }
        }

        internal static bool ThreadDetector(TessellatedSolid solid, List<PrimitiveSurface> primitiveSurfaces)
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
                if (threads.Count < 8) continue;
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
            var gVisited = new HashSet<Edge>();
            foreach (
                var edge in
                    solid.Edges.Where(e => Math.Abs(e.InternalAngle - 2.08566845) < 0.04
                        && !gVisited.Contains(e)))
            // 2.0943951 is equal to 120 degree
            {
                // To every side of the edge if there is one edge with the IA of 120, this edge is unique and we dcannot find the second one. 
                gVisited.Add(edge);
                var visited = new HashSet<Edge> { edge };
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
                gVisited.UnionWith(visited);
                if (visited.Count < 1000) // Is it very big?
                    continue;
                return true;
            }
            return false;
        }

        private static bool ConeThreadIsInternal(List<Cone> threads)
        {
            // If it is seperated cones, it's easy: negative cones make internal thread
            // To make it robust, if 70 percent of the cones are negative, it is internal
            var neg = threads.Count(cone => !cone.IsPositive);
            if (neg >= 0.7 * threads.Count) return true;
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

        internal static int CommonHeadCheck(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<TemporaryFlat, List<TemporaryFlat>> equalPrimitives, out double toolSize)
        {
            // 0: false (doesnt have any common head shape)
            // 1: HexBolt or Nut
            // 2: Allen
            // 3: Phillips
            // 4: Slot
            // 5: Phillips and Slot combo

            toolSize = 0.0;
            // check for hex bolt, nut and allen -------------------------------------------------------------
            var sixFlat = FastenerDetector.EqualPrimitivesFinder(equalPrimitives, 6);
            var eightFlat = FastenerDetector.EqualPrimitivesFinder(equalPrimitives, 8);
            var twoFlat = FastenerDetector.EqualPrimitivesFinder(equalPrimitives, 2);
            var fourFlat = FastenerDetector.EqualPrimitivesFinder(equalPrimitives, 4);
            //if (!sixFlat.Any()) return 0;
            foreach (var candidateHex in sixFlat)
            {
                var candidateHexVal = equalPrimitives[candidateHex];
                var cos = new List<double>();
                var firstPrimNormal = (candidateHexVal[0]).Normal;
                for (var i = 1; i < candidateHexVal.Count; i++)
                    cos.Add(firstPrimNormal.dotProduct((candidateHexVal[i]).Normal));
                // if it is a hex or allen bolt, the cos list must have two 1/2, two -1/2 and one -1
                if (cos.Count(c => Math.Abs(0.5 - c) < 0.0001) != 2 ||
                    cos.Count(c => Math.Abs(-0.5 - c) < 0.0001) != 2 ||
                    cos.Count(c => Math.Abs(-1 - c) < 0.0001) != 1) continue;
                toolSize = AutoNonthreadedFastenerDetection.ToolSizeFinder(candidateHexVal);
                if (AutoNonthreadedFastenerDetection.IsItAllen(candidateHexVal))
                    return 2;
                return 1;
            }
            foreach (var candidateHex in eightFlat)
            {
                var candidateHexVal = equalPrimitives[candidateHex];
                var cos = new List<double>();
                var firstPrimNormal = (candidateHexVal[0]).Normal;
                for (var i = 1; i < candidateHexVal.Count; i++)
                    cos.Add(firstPrimNormal.dotProduct((candidateHexVal[i]).Normal));
                var oneFlat = FastenerDetector.EqualPrimitivesFinder(equalPrimitives, 1);
                // Any flat that is adjacent to all of these eights?
                // if it is philips head, the cos list must have four 0, two -1 and one 1
                foreach (var sh in oneFlat)
                    if (candidateHexVal.All(f => f.OuterEdges.Any(sh.OuterEdges.Contains)))
                        return 3;
                /*if (cos.Count(c => Math.Abs(0.0 - c) < 0.0001) != 4 ||
                    cos.Count(c => Math.Abs(-1 - c) < 0.0001) != 2 ||
                    cos.Count(c => Math.Abs(1 - c) < 0.0001) != 1) continue;
                return 3;*/
                continue;
            }
            foreach (var candidateHex in twoFlat)
            {
                var candidateHexVal = equalPrimitives[candidateHex];
                var cos = (candidateHexVal[0]).Normal.dotProduct((candidateHexVal[1]).Normal);
                if (!(Math.Abs(-1 - cos) < 0.0001)) continue;
                // I will add couple of conditions here:
                //    1. If the number of solid vertices in front of each flat is equal to another
                //    2. If the summation of the vertices in 1 is greater than the total # of verts
                //    3. and I also need to add some constraints for the for eample the area of the cylinder
                var leftVerts = AutoNonthreadedFastenerDetection.VertsInfrontOfFlat(solid, candidateHexVal[0]);
                var rightVerts = AutoNonthreadedFastenerDetection.VertsInfrontOfFlat(solid, candidateHexVal[1]);
                if (Math.Abs(leftVerts - rightVerts) > 10 || leftVerts + rightVerts <= solid.Vertices.Length)
                    continue;
                return 4;
            }

            var eachSlot = 0;
            var flats = new List<TemporaryFlat>();
            foreach (var candidateHex in fourFlat)
            {
                var candidateHexVal = equalPrimitives[candidateHex];
                var cos = new List<double>();
                var firstPrimNormal = (candidateHexVal[0]).Normal;
                for (var i = 1; i < candidateHexVal.Count; i++)
                    cos.Add(firstPrimNormal.dotProduct((candidateHexVal[i]).Normal));
                // if it is a slot and phillips combo the cos list must have two -1 and one 1
                // and this needs to appear 2 times.
                if (cos.Count(c => Math.Abs(-1 - c) < 0.0001) != 2 ||
                    cos.Count(c => Math.Abs(1 - c) < 0.0001) != 1) continue;
                flats.AddRange(candidateHexVal);
                eachSlot++;
            }
            if (eachSlot == 2) return 5;
            return 0;
        }

        internal static void AddRepeatedSolidstoFasteners(Fastener lastAddedFastener, List<TessellatedSolid> repeatedSolid)
        {
            foreach (var solid in repeatedSolid)
            {
                if (solid == lastAddedFastener.Solid) continue;
                lock (FastenerDetector.Fasteners)
                    FastenerDetector.Fasteners.Add(new Fastener
                    {
                        Solid = solid,
                        NumberOfThreads = lastAddedFastener.NumberOfThreads,
                        FastenerType = lastAddedFastener.FastenerType,
                        RemovalDirection = lastAddedFastener.RemovalDirection,
                        OverallLength = lastAddedFastener.OverallLength,
                        EngagedLength = lastAddedFastener.EngagedLength,
                        Diameter = lastAddedFastener.Diameter,
                        Certainty = lastAddedFastener.Certainty,
                        Tool = lastAddedFastener.Tool,
                        ToolSize = lastAddedFastener.ToolSize
                    });
            }
        }
        internal static void AddRepeatedSolidstoNuts(Nut lastAddedNut, List<TessellatedSolid> repeatedSolid)
        {
            foreach (var solid in repeatedSolid)
            {
                if (solid == lastAddedNut.Solid) continue;
                FastenerDetector.Nuts.Add(new Nut
                {
                    Solid = solid,
                    NumberOfThreads = lastAddedNut.NumberOfThreads,
                    RemovalDirection = lastAddedNut.RemovalDirection,
                    OverallLength = lastAddedNut.OverallLength,
                    Diameter = lastAddedNut.Diameter,
                    Certainty = lastAddedNut.Certainty,
                    Tool = lastAddedNut.Tool,
                    ToolSize = lastAddedNut.ToolSize,
                    NutType = lastAddedNut.NutType
                });
            }
        }
    }
}