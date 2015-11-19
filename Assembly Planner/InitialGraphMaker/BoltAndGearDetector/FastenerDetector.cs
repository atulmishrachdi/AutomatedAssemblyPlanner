using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using StarMathLib;
using TVGL;

namespace Assembly_Planner
{
    internal class FastenerDetector
    {
        internal static List<Fastener> Fasteners = new List<Fastener>();
        internal static List<Nut> Nuts = new List<Nut>();
        internal static List<Washer> Washers = new List<Washer>();

        internal static HashSet<TessellatedSolid> Run(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs, bool autoDetection, bool threaded,
            bool regenerateTrainingData = false)
        {
            var s = Stopwatch.StartNew();
            s.Start();
            Console.WriteLine();
            Console.WriteLine("Detecting Fasteners ....");
            Fasteners.Clear();
            Nuts.Clear();
            Washers.Clear();
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
                    AutoNonthreadedFastenerDetection.Run(solidPrimitive, multipleRefs);
                else
                    AutoThreadedFastenerDetection.Run(solidPrimitive, multipleRefs);
            }

            s.Stop();
            Console.WriteLine("Fastener Detection:" + "     " + s.Elapsed);
            return fastener;
        }

        internal static List<List<TessellatedSolid>> GroupingSmallParts(List<TessellatedSolid> firstFilter)
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

        internal static Dictionary<TessellatedSolid, Dictionary<PrimitiveSurface, List<PrimitiveSurface>>>
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

        internal static List<PrimitiveSurface> EqualPrimitivesFinder(
            Dictionary<PrimitiveSurface, List<PrimitiveSurface>> equalPrimittives, int numberOfEqualPrim)
        {
            return equalPrimittives.Keys.Where(
                k => equalPrimittives[k].Count == numberOfEqualPrim).ToList();
        }

        internal static List<TessellatedSolid> SmallObjectsDetector(
            Dictionary<TessellatedSolid, List<TessellatedSolid>> solidRepeated)
        {
            var partSize = new Dictionary<TessellatedSolid, double>();
            var parts = solidRepeated.Keys.ToList();
            foreach (var solid in solidRepeated.Keys.ToList())
            {
                var shortestObbEdge = double.PositiveInfinity;
                var longestObbEdge = double.NegativeInfinity;
                var solidObb = BoundingGeometry.OrientedBoundingBoxDic[solid];
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

        internal static int RemovalDirectionFinderUsingObb(TessellatedSolid solid, BoundingBox obb)
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
            var longestPlane = GeometryFunctions.LongestPlaneOfObbDetector(obb, out facePrepToRD1, out facePrepToRD2);
            TVGL.Vertex closestVerToPlane = null;
            var minDist = double.PositiveInfinity;
            foreach (var ver in solid.ConvexHullVertices)
            {
                var dis = GeometryFunctions.DistanceBetweenVertexAndPlane(ver.Position, longestPlane[0]);
                if (dis >= minDist)
                    continue;
                closestVerToPlane = ver;
                minDist = dis;
            }
            // The closest vertex to plane is closer to which facePrepToRD?
            double[] normalGuess = null;
            if (GeometryFunctions.DistanceBetweenVertexAndPlane(closestVerToPlane.Position, facePrepToRD1) <
                GeometryFunctions.DistanceBetweenVertexAndPlane(closestVerToPlane.Position, facePrepToRD2))
                normalGuess = facePrepToRD1.Normal;
            else
                normalGuess = facePrepToRD2.Normal;
            var equInDirections =
                DisassemblyDirections.Directions.Where(d => Math.Abs(d.dotProduct(normalGuess) - 1) < 0.001).ToList()[0];
            return DisassemblyDirections.Directions.IndexOf(equInDirections);
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

    }
}
