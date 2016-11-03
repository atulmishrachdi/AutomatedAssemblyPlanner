using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TVGL;
using StarMathLib;

namespace Assembly_Planner
{
    internal class AutoNonthreadedFastenerDetection
    {
        internal static void Run(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs)
        {
            // This approach will use the symmetric shape of the fasteners' heads. If there is no thread,
            // we willl consider the area of the positive culinders for bolts and negative cylinder for 
            // nuts. 
            // This is an important point: 
            //      1. Find the small objects using all of the solids
            //      2. Group them using small objects
            //      3. Detect the fasteners using multiple references. (for each similar object, detect one of them) 

            var smallParts = FastenerDetector.SmallObjectsDetector(DisassemblyDirectionsWithFastener.PartsWithOneGeom);
                //new List<TessellatedSolid>(DisassemblyDirectionsWithFastener.PartsWithOneGeom);

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
            var checkedSolids = new HashSet<TessellatedSolid>();
            FastenerGaussianNaiveBayes.GNB();

            foreach( var part in uniqueParts)
            {
                FastenerDetector.SolidHasFastenerKeyword(part);
            }

            Parallel.ForEach(uniqueParts, solid =>
                //foreach (var solid in uniqueParts)
            {
                var initialCertainty = FastenerGaussianNaiveBayes.GaussianNaiveBayesClassifier(solidPrimitive[solid],
                    solid);
                
               
                
                FastenerDetector.PotentialFastener[solid] = 0.1 + initialCertainty;
                foreach (var up in multipleRefs[solid])
                    FastenerDetector.PotentialFastener[up] = 0.1 + initialCertainty;
                if (solidPrimitive[solid].Count == 0) return;
                if (HexBoltNutAllen(solid, solidPrimitive[solid], equalPrimitivesForEveryUniqueSolid[solid],
                    multipleRefs[solid]))
                {
                    //continue;
                    lock (checkedSolids)
                    {
                        checkedSolids.Add(solid);
                    }
                    return;
                }
                if (PhillipsHeadBolt(solid, solidPrimitive[solid], equalPrimitivesForEveryUniqueSolid[solid],
                    multipleRefs[solid]))
                {
                    //continue;
                    lock (checkedSolids)
                    {
                        checkedSolids.Add(solid);
                    }
                    return;
                }
                if (SlotHeadBolt(solid, solidPrimitive[solid], equalPrimitivesForEveryUniqueSolid[solid],
                    multipleRefs[solid]))
                {
                    //continue;
                    lock (checkedSolids)
                    {
                        checkedSolids.Add(solid);
                    }
                    return;
                }
                if (PhillipsSlotComboHeadBolt(solid, solidPrimitive[solid], equalPrimitivesForEveryUniqueSolid[solid],
                    multipleRefs[solid]))
                {
                    //continue;
                    lock (checkedSolids)
                    {
                        checkedSolids.Add(solid);
                    }
                    return;
                }
                lock (checkedSolids)
                {
                    checkedSolids.Add(solid);

                }
            }
                ); //
            // when all of the fasteners are recognized, it's time to check the third level filter:
            // something is not acceptable: nut without fastener. If there is a nut without fastener,
            // I will try to look for that.
            // Is there anyway to detect more?
            ConnectFastenersNutsAndWashers(groupedPotentialFasteners);
        }

        private static void PreSelectedFastenerToFastenerClass(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs)
        {
            foreach (var preFastener in FastenerDetector.PreSelectedFasteners)
            {
                var lengthAndRadius = FastenerEngagedLengthAndRadiusNoThread(preFastener, solidPrimitive[preFastener]);
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
                foreach (var repeatedSolid in repeated)
                {
                    var fastener = new Fastener
                    {
                        Solid = repeatedSolid,
                        FastenerType = FastenerTypeEnum.Bolt,
                        RemovalDirection =
                            FastenerDetector.RemovalDirectionFinderUsingObb(preFastener,
                                BoundingGeometry.OrientedBoundingBoxDic[preFastener]),
                        OverallLength =
                            GeometryFunctions.SortedLengthOfObbEdges(
                                BoundingGeometry.OrientedBoundingBoxDic[preFastener])[2],
                        EngagedLength = lengthAndRadius[0],
                        Diameter = lengthAndRadius[1] * 2.0,
                        Certainty = 1.0
                    };
                    FastenerDetector.Fasteners.Add(fastener);
                }
            }
        }

        private static bool HexBoltNutAllen(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<TemporaryFlat, List<TemporaryFlat>> equalPrimitives, List<TessellatedSolid> repeated)
        {
            repeated.Add(solid);
            var sixFlat = FastenerDetector.EqualPrimitivesFinder(equalPrimitives, 6);
            if (!sixFlat.Any()) return false;
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
                var lengthAndRadius = FastenerEngagedLengthAndRadiusNoThread(solid, solidPrim);
                if (IsItAllen(candidateHexVal))
                {
                    // this is a socket bolt (allen)
                    foreach (var repeatedSolid in repeated)
                    {
                        var fastener = new Fastener
                        {
                            Solid = repeatedSolid,
                            FastenerType = FastenerTypeEnum.Bolt,
                            Tool = Tool.Allen,
                            ToolSize = ToolSizeFinder(candidateHexVal),
                            RemovalDirection =
                                //RemovalDirectionFinderForAllenHexPhillips(candidateHexVal.Cast<Flat>().ToList(),
                                //    BoundingGeometry.OrientedBoundingBoxDic[solid]),
                                FastenerDetector.RemovalDirectionFinderUsingObb(repeatedSolid, BoundingGeometry.OrientedBoundingBoxDic[repeatedSolid]),
                            OverallLength =
                                GeometryFunctions.SortedLengthOfObbEdges(BoundingGeometry.OrientedBoundingBoxDic[solid])[2],
                            EngagedLength = lengthAndRadius[0],
                            Diameter = lengthAndRadius[1] * 2.0,
                            Certainty = 1.0
                        };
                        lock (FastenerDetector.Fasteners)
                            FastenerDetector.Fasteners.Add(fastener);
                    }
                    return true;
                }
                // else: it is a hex bolt or nut
                if (IsItNut(solidPrim.Where(p => p is Cylinder).Cast<Cylinder>().ToList(), solid))
                {
                    foreach (var repeatedSolid in repeated)
                    {
                        lock (FastenerDetector.Nuts)
                            FastenerDetector.Nuts.Add(new Nut
                            {
                                NutType = NutType.Hex,
                                Solid = repeatedSolid,
                                ToolSize = ToolSizeFinder(candidateHexVal),
                                OverallLength = lengthAndRadius[0],
                                Diameter = lengthAndRadius[1] * 2.0,
                                Certainty = 1.0
                            });
                    }
                    return true;
                }
                foreach (var repeatedSolid in repeated)
                {
                    lock (FastenerDetector.Fasteners)
                        FastenerDetector.Fasteners.Add(new Fastener
                        {
                            Solid = repeatedSolid,
                            FastenerType = FastenerTypeEnum.Bolt,
                            Tool = Tool.HexWrench,
                            ToolSize = ToolSizeFinder(candidateHexVal),
                            RemovalDirection =
                                //RemovalDirectionFinderForAllenHexPhillips(candidateHexVal.Cast<Flat>().ToList(),
                                //    BoundingGeometry.OrientedBoundingBoxDic[solid]),
                                FastenerDetector.RemovalDirectionFinderUsingObb(repeatedSolid, BoundingGeometry.OrientedBoundingBoxDic[repeatedSolid]),
                            OverallLength =
                                GeometryFunctions.SortedLengthOfObbEdges(BoundingGeometry.OrientedBoundingBoxDic[solid])
                                    [2],
                            EngagedLength = lengthAndRadius[0],
                            Diameter = lengthAndRadius[1] * 2.0,
                            Certainty = 1.0
                        });
                }
                return true;
            }
            return false;
        }

        internal static bool IsItAllen(List<TemporaryFlat> candidateHexVal)
        {
            TemporaryFlat p1 = candidateHexVal[0], p2 = null;
            for (var i = 1; i < candidateHexVal.Count; i++)
            {
                var prim = candidateHexVal[i];
                if (Math.Abs(prim.Normal.dotProduct(p1.Normal)) < 0.9)
                {
                    p2 = prim;
                    break;
                }
            }
            var refVector = p1.Normal.crossProduct(p2.Normal).normalize();
            return
                candidateHexVal.Any(
                    p =>
                        p.OuterEdges.Any(
                            e =>
                                1 - Math.Abs(e.Vector.dotProduct(refVector)) < 0.8 &&
                                e.Curvature == CurvatureType.Concave));
        }

        internal static bool IsItNut(List<Cylinder> cylinders, TessellatedSolid boltOrNut)
        {
            if (cylinders.Any(p => !p.IsPositive))
                if (cylinders.Where(c => c.IsPositive).Sum(pC => pC.Area) <
                    0.05 * boltOrNut.SurfaceArea)
                    // this is a nut. Because it has negative cylinders and positive cylinders that it has
                    // are minor
                    return true;
            return false;
        }

        private static bool PhillipsHeadBolt(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<TemporaryFlat, List<TemporaryFlat>> equalPrimitives, List<TessellatedSolid> repeated)
        {
            repeated.Add(solid);
            var eightFlat = FastenerDetector.EqualPrimitivesFinder(equalPrimitives, 8);
            if (!eightFlat.Any())
            {
                // before I return false, I will take the ones with 4 faces that have a close area and merge them.
                /*var fourEqual1 = FastenerDetector.EqualPrimitivesFinder(equalPrimitives, 4);
                var closeArea1 = new List<TemporaryFlat[]>();
                for (var i = 0; i < fourEqual1.Count - 1; i++)
                {
                    for (var j = i + 1; j < fourEqual1.Count; j++)
                    {
                        if (Math.Abs(fourEqual1[i].Area - fourEqual1[j].Area) < 0.1 * fourEqual1[i].Area)
                        {
                            closeArea1.Add(new[] { fourEqual1[i], fourEqual1[j] });
                            break;
                        }
                    }
                }
                foreach (var cA in closeArea1)
                {
                    equalPrimitives[cA[0]].AddRange(equalPrimitives[cA[1]]);
                }
                return PhillipsHeadBolt(solid, solidPrim, equalPrimitives, repeated);*/
                return false;
            }
            foreach (var candidateHex in eightFlat)
            {
                var candidateHexVal = equalPrimitives[candidateHex];
                var cos = new List<double>();
                var firstPrimNormal = (candidateHexVal[0]).Normal;
                for (var i = 1; i < candidateHexVal.Count; i++)
                    cos.Add(firstPrimNormal.dotProduct((candidateHexVal[i]).Normal));
                // if it is philips head, the cos list must have four 0, two -1 and one 1
                if (cos.Count(c => Math.Abs(0.0 - c) < 0.025) != 4 ||
                    cos.Count(c => Math.Abs(-1 - c) < 0.025) != 2 ||
                    cos.Count(c => Math.Abs(1 - c) < 0.001) != 1) continue;
                var lengthAndRadius = FastenerEngagedLengthAndRadiusNoThread(solid, solidPrim);
                foreach (var repeatedSolid in repeated)
                {
                    var fastener = new Fastener
                    {
                        Solid = repeatedSolid,
                        FastenerType = FastenerTypeEnum.Bolt,
                        Tool = Tool.PhillipsBlade,
                        RemovalDirection =
                            //RemovalDirectionFinderForAllenHexPhillips(candidateHexVal.Cast<Flat>().ToList(),
                            //    BoundingGeometry.OrientedBoundingBoxDic[solid]),
                            FastenerDetector.RemovalDirectionFinderUsingObb(repeatedSolid, BoundingGeometry.OrientedBoundingBoxDic[repeatedSolid]),
                        OverallLength =
                            GeometryFunctions.SortedLengthOfObbEdges(BoundingGeometry.OrientedBoundingBoxDic[solid])[2],
                        EngagedLength = lengthAndRadius[0],
                        Diameter = lengthAndRadius[1] * 2.0,
                        Certainty = 1.0
                    };
                    lock (FastenerDetector.Fasteners)
                        FastenerDetector.Fasteners.Add(fastener);
                }
                return true;
            }
            // before I return false, I will take the ones with 4 faces that have a close area and merge them.
            /*var fourEqual = FastenerDetector.EqualPrimitivesFinder(equalPrimitives, 4);
            var closeArea = new List<TemporaryFlat[]>();
            for (var i = 0; i < fourEqual.Count - 1; i++)
            {
                for (var j = i+1; j < fourEqual.Count; j++)
                {
                    if (Math.Abs(fourEqual[i].Area - fourEqual[j].Area) < 0.01*fourEqual[i].Area)
                    {
                        closeArea.Add(new []{ fourEqual[i] , fourEqual[j] });
                        break;
                    }
                }
            }
            foreach (var cA in closeArea)
            {
                equalPrimitives[cA[0]].AddRange(equalPrimitives[cA[1]]);
            }
            return PhillipsHeadBolt(solid, solidPrim, equalPrimitives, repeated);*/
            return false;
        }

        private static bool SlotHeadBolt(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<TemporaryFlat, List<TemporaryFlat>> equalPrimitives, List<TessellatedSolid> repeated)
        {
            repeated.Add(solid);
            var twoFlat = FastenerDetector.EqualPrimitivesFinder(equalPrimitives, 2);
            if (!twoFlat.Any()) return false;
            foreach (var candidateHex in twoFlat)
            {
                var candidateHexVal = equalPrimitives[candidateHex];
                var cos = (candidateHexVal[0]).Normal.dotProduct((candidateHexVal[1]).Normal);
                if (!(Math.Abs(-1 - cos) < 0.0001)) continue;
                // I will add couple of conditions here:
                //    1. If the number of solid vertices in front of each flat is equal to another
                //    2. If the summation of the vertices in 1 is greater than the total # of verts
                //    3. and I also need to add some constraints for the for eample the area of the cylinder
                var leftVerts = VertsInfrontOfFlat(solid, candidateHexVal[0]);
                var rightVerts = VertsInfrontOfFlat(solid, candidateHexVal[1]);
                if (Math.Abs(leftVerts - rightVerts) > 2 || leftVerts + rightVerts <= solid.Vertices.Length)
                    continue;
                if (!solidPrim.Where(p => p is Cylinder).Cast<Cylinder>().Any(c => c.IsPositive)) continue;
                var lengthAndRadius = FastenerEngagedLengthAndRadiusNoThread(solid, solidPrim);
                foreach (var repeatedSolid in repeated)
                {
                    var fastener = new Fastener
                    {
                        Solid = repeatedSolid,
                        FastenerType = FastenerTypeEnum.Bolt,
                        Tool = Tool.FlatBlade,
                        RemovalDirection =
                            //RemovalDirectionFinderForSlot(candidateHexVal.Cast<Flat>().ToList(),
                            //    solidPrim.Where(p => p is Flat).Cast<Flat>().ToList()),
                            FastenerDetector.RemovalDirectionFinderUsingObb(repeatedSolid,
                                BoundingGeometry.OrientedBoundingBoxDic[repeatedSolid]),
                        OverallLength =
                            GeometryFunctions.SortedLengthOfObbEdges(BoundingGeometry.OrientedBoundingBoxDic[solid])[2],
                        EngagedLength = lengthAndRadius[0],
                        Diameter = lengthAndRadius[1] * 2.0,
                        Certainty = 1.0
                    };
                    lock (FastenerDetector.Fasteners)
                        FastenerDetector.Fasteners.Add(fastener);
                }
                return true;
            }
            return false;
        }

        private static bool PhillipsSlotComboHeadBolt(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<TemporaryFlat, List<TemporaryFlat>> equalPrimitives, List<TessellatedSolid> repeated)
        {
            repeated.Add(solid);
            var fourFlat = FastenerDetector.EqualPrimitivesFinder(equalPrimitives, 4);
            if (fourFlat.Count < 2) return false;
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
                if (!solidPrim.Where(p => p is Cylinder).Cast<Cylinder>().Any(c => c.IsPositive)) continue;
                flats.AddRange(candidateHexVal);
                eachSlot++;
            }
            if (eachSlot != 2) return false;
            var lengthAndRadius = FastenerEngagedLengthAndRadiusNoThread(solid, solidPrim);
            foreach (var repeatedSolid in repeated)
            {
                var fastener = new Fastener
                {
                    Solid = repeatedSolid,
                    FastenerType = FastenerTypeEnum.Bolt,
                    Tool = Tool.PhillipsBlade,
                    RemovalDirection =
                        //RemovalDirectionFinderForAllenHexPhillips(flats.Cast<Flat>().ToList(),
                        //    BoundingGeometry.OrientedBoundingBoxDic[solid]),
                        FastenerDetector.RemovalDirectionFinderUsingObb(repeatedSolid, BoundingGeometry.OrientedBoundingBoxDic[repeatedSolid]),
                    OverallLength =
                        GeometryFunctions.SortedLengthOfObbEdges(BoundingGeometry.OrientedBoundingBoxDic[solid])[2],
                    EngagedLength = lengthAndRadius[0],
                    Diameter = lengthAndRadius[1] * 2.0,
                    Certainty = 1.0
                };
                lock (FastenerDetector.Fasteners)
                    FastenerDetector.Fasteners.Add(fastener);
            }
            return true;
        }

        internal static int VertsInfrontOfFlat(TessellatedSolid solid, TemporaryFlat flat)
        {
            return
                solid.Vertices.Count(
                    v => flat.Normal.dotProduct(v.Position.subtract(flat.Faces[0].Vertices[0].Position)) > 0);
        }

        internal static double ToolSizeFinder(List<TemporaryFlat> candidateHexVal)
        {
            var firstPrimNormal = (candidateHexVal[0]).Normal;
            for (var i = 1; i < candidateHexVal.Count; i++)
            {
                if (Math.Abs(firstPrimNormal.dotProduct((candidateHexVal[i]).Normal) + 1) > 0.0001) continue;
                return
                    Math.Abs(Math.Abs(candidateHexVal[0].Faces[0].Vertices[0].Position.dotProduct(firstPrimNormal)) -
                             Math.Abs(candidateHexVal[i].Faces[0].Vertices[0].Position.dotProduct(firstPrimNormal)));
            }
            return 0.0;
        }


        private static int RemovalDirectionFinderForSlot(List<Flat> equalFlats, List<Flat> flats)
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
                DisassemblyDirections.Directions.Where(d => Math.Abs(d.dotProduct(normalGuess) - 1) < 0.01).ToList()[0];
            // find the furthest vertex (b) to a vertex (a) from allen faces. if
            // Normal.dotproduct(a-b) must be positive. If it was negative, return 
            // multiply(-1)
            var a = flatPrims[0].Vertices[0];
            var farthestVer = flatPrims[0].Vertices[0];
            var dist = 0.0;
            foreach (var ver in solid.CornerVertices)
            {
                var locDist = GeometryFunctions.DistanceBetweenTwoVertices(a.Position, ver.Position);
                if (locDist <= dist) continue;
                farthestVer = new Vertex(ver.Position);
                dist = locDist;
            }
            var reference = a.Position.subtract(farthestVer.Position);
            if (normalGuess.dotProduct(reference) > 0)
                return DisassemblyDirections.Directions.IndexOf(equInDirections);
            return DisassemblyDirections.Directions.IndexOf(equInDirections.multiply(-1.0));
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

        internal static double[] FastenerEngagedLengthAndRadiusNoThread(TessellatedSolid solid, List<PrimitiveSurface> solidPrim)
        {
            // the length and the radius of the longest cylinder
            //if (!solidPrim.Any(p => p is Cylinder))
            //    throw new Exception("the fastener does not have any cylinder");
            //[0] = length, [1] = radius
            if (!solidPrim.Any(p => p is Cylinder))
                return new[]
                {BoundingGeometry.BoundingCylinderDic[solid].Length, BoundingGeometry.BoundingCylinderDic[solid].Radius};
            Cylinder longestCyl = null;
            var length = double.NegativeInfinity;
            foreach (Cylinder cylinder in solidPrim.Where(p => p is Cylinder))
            {
                var medLength = GeometryFunctions.SortedEdgeLengthOfTriangle(cylinder.Faces[0])[1];
                if (medLength <= length) continue;
                length = medLength;
                longestCyl = cylinder;
            }
            return new[] { length, longestCyl.Radius };
        }

        internal static void ConnectFastenersNutsAndWashers(List<List<TessellatedSolid>> groupedPotentialFasteners)
        {
            // Possible cases:
            //  1. Fastener
            //  2. Fastener-Nut
            //  3. Fastener-Nut-Washer
            //  4. Fastener-Washer
            //  5. Nut
            //  6. Nut-Washer
            var counter1 = 0;
            foreach (var fastener in FastenerDetector.Fasteners)
            {
                counter1++;
                // if there is a fastener, find its nuts and washers
                var groupList = groupedPotentialFasteners.Where(g => g.Contains(fastener.Solid)).ToList();

                //------------------------------------- Case 1 ------------------------------------
                if (groupList.Count == 0) continue;
                var group = groupList.ToList()[0];
                var numFasteners = group.Count(s => FastenerDetector.Fasteners.Any(f => f.Solid == s));
                if (numFasteners > 1) continue;
                //group.Add(fastener.Solid);
                //if (group.Count == 1) continue;

                //----------------------------------- Cases 2,3,4 ---------------------------------
                var nutAndWasherRemovalDirection = DisassemblyDirections.DirectionsAndOpposits[fastener.RemovalDirection];
                var nuts = FastenerDetector.Nuts.Where(n => group.Contains(n.Solid)).ToList();
                var nutList = new List<Nut>();
                if (nuts.Any())
                {
                    foreach (var nut in nuts)
                    {
                        nut.RemovalDirection = nutAndWasherRemovalDirection;
                        nut.Diameter = fastener.Diameter;
                        nut.Certainty = 1.0;
                        nutList.Add(nut);
                    }
                }
                var potentialWasher = group.Where(s => s != fastener.Solid && nuts.All(n => s != n.Solid)).ToList();
                // if there is a solid which is very very small comparing the bolt, it is a washer?
                // the possible washer can still be a nut that has not been detected before
                // take its obb. if the smallest edge to the longest edge is less than 0.2, it's a washer,
                // otherwise it can be a nut with low certainty
                var washers = new List<Washer>();
                foreach (var pWasher in potentialWasher)
                {
                    if (Math.Abs(pWasher.Volume) > Math.Abs(fastener.Solid.Volume)) continue;
                    var edgesOfObb =
                        GeometryFunctions.SortedLengthOfObbEdges(BoundingGeometry.OrientedBoundingBoxDic[pWasher]);
                    if ((edgesOfObb[0] / edgesOfObb[2]) < 0.2)
                    // shortest/ longest // this can be fuzzified to define ceratinty
                    {
                        washers.Add(new Washer
                        {
                            Solid = pWasher,
                            Certainty = 0.7,
                            RemovalDirection = nutAndWasherRemovalDirection
                        });
                    }
                    else
                    {
                        // it can be a nut
                        var uncertainNut = new Nut
                        {
                            Solid = pWasher,
                            Certainty = 0.4,
                            RemovalDirection = nutAndWasherRemovalDirection
                        };
                        nutList.Add(uncertainNut);
                        FastenerDetector.Nuts.Add(uncertainNut);
                    }
                }
                fastener.Nuts = nutList;
                fastener.Washer = washers;
                FastenerDetector.Washers.UnionWith(washers);
            }
            // if there is a detected nut, but its fastener was not detected:
            // fastener is unknown
            var counter2 = 0;
            foreach (var nut in FastenerDetector.Nuts)
            {
                counter2++;
                if (FastenerDetector.Fasteners.All(f => f.Nuts == null || !f.Nuts.Contains(nut)))
                {
                    // this a nut that doesnt have any fastener.
                    var groupList = groupedPotentialFasteners.Where(g => g.Contains(nut.Solid)).ToList();

                    //------------------------------------- Case 5 ------------------------------------
                    if (groupList.Count == 0) continue;
                    var group = groupList.ToList()[0];
                    group.Add(nut.Solid);
                    //if (group.Count == 1) continue;

                    //---------------------------------- Cases 2,6 --------------------------------
                    var potentialFastener = group.Where(s => s != nut.Solid).ToList();
                    Fastener fastener = null;
                    var nutAndWasherRemovalDirection = 0;
                    List<Washer> washers = new List<Washer>();
                    foreach (var pF in potentialFastener)
                    {
                        if (pF.Volume > nut.Solid.Volume && fastener == null)
                        {
                            // can be a fastener
                            fastener = new Fastener
                            {
                                Solid = pF,
                                RemovalDirection =
                                    FastenerDetector.RemovalDirectionFinderUsingObb(pF,
                                        BoundingGeometry.OrientedBoundingBoxDic[pF]),
                                OverallLength =
                                    GeometryFunctions.SortedLengthOfObbEdges(
                                        BoundingGeometry.OrientedBoundingBoxDic[pF])[2],
                                EngagedLength = GeometryFunctions.SortedLengthOfObbEdges( // TBD
                                    BoundingGeometry.OrientedBoundingBoxDic[pF])[2],
                                Diameter = nut.Diameter,
                                Certainty = 0.3
                            };
                            nutAndWasherRemovalDirection = DisassemblyDirections.Directions.IndexOf(
                                DisassemblyDirections.Directions[fastener.RemovalDirection].multiply(-1.0));
                        }
                        if (pF.Volume < nut.Solid.Volume)
                        {
                            // can be a washer
                            washers.Add(new Washer
                            {
                                Solid = pF,
                                Certainty = 0.2
                            });
                        }
                    }
                    // by this point these things can have happened: 
                    // A nut that its unknown fastener is found
                    // or a nut that its washer is found without fastener
                    // a nut, with its fastener and washer
                    FastenerDetector.Washers.UnionWith(washers);
                    if (fastener != null)
                    {
                        FastenerDetector.Fasteners.Add(fastener);
                        nut.RemovalDirection = nutAndWasherRemovalDirection;
                        fastener.Nuts = new List<Nut> { nut };
                        fastener.Washer = washers;
                    }
                    // but if the fastener is null, if any washer exists,
                    //  it is a washer for the nut. Example: there is a 
                    //  rod with some threads on one of its ends.
                    //  I cannot find the rod, but I can possibly find the
                    //  nut and the washer. So I will also define washer as 
                    //  a property for nut.
                    nut.Washer = washers;
                    // removal direction? I can say along a line, and
                    // the see if any of those directions are infinite.
                    // but this doesnt work for threaded nuts and rods
                }
            }
        }

    }
}
