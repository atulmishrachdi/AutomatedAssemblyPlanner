using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using Assembly_Planner.GeometryReasoning;
using StarMathLib;
using TVGL;
using Vertex = TVGL.Vertex;

namespace Assembly_Planner
{
    class FastenerPolynomialTrend
    {
        internal static bool PolynomialTrendDetector(TessellatedSolid solid)
        {
            // Assumptions:
            //    1. In fasteners, length is longer than width. 
            var obb = PartitioningSolid.OrientedBoundingBoxDic[solid];
            //if (!solid.Name.Contains("STLB ASM")) return true;
            double[][] dir;
            bool clockWise;
            var myObb = OBB.BuildUsingPoints(solid.Vertices.ToList(), out dir, out clockWise);
            
            PolygonalFace f1;
            PolygonalFace f2;
            //var longestSide = BoltAndGearDetection.LongestPlaneOfObbDetector(obb, out f1, out f2);
            var longestSide = BoltAndGearDetection.LongestPlaneOfObbDetector(myObb, clockWise, out f1, out f2);
            // 1. Take the middle point of the smallest edge of each triangle. Or the points of the 2nd longest edge of a side triangle
            // 2. Generate k points between them with equal distances. 
            // 3. Generate rays using generated points. 
            var midPoint1 = ShortestEdgeMidPointOfTriangle(longestSide[0]);
            var midPoint2 = ShortestEdgeMidPointOfTriangle(longestSide[1]);

            var kPointsBetweenMidPoints = KpointBtwPointsGenerator(midPoint1, midPoint2, 1000);

            var distancePointToSolid = PointToSolidDistanceCalculator(solid, kPointsBetweenMidPoints,
                longestSide[0].Normal.multiply(-1.0));

            // one more step: Merge points with equal distances.
            distancePointToSolid = MergingEqualDistances(distancePointToSolid,0.001);
            
            var hasThread = ContainsThread(distancePointToSolid);
            // Plot:
            if (hasThread)
                PlotInMatlab(distancePointToSolid);

            return hasThread;
        }

        private static bool ContainsThread(List<double> distancePointToSolid)
        {
            var directionChange = new List<double>();
            for (var i = 0; i < distancePointToSolid.Count-1; i++)
            {
                var c = 0;
                for (var j = i + 1;
                    Math.Sign(distancePointToSolid[j] - distancePointToSolid[j - 1]) ==
                    Math.Sign(distancePointToSolid[i + 1] - distancePointToSolid[i]);
                    j++)
                {
                    c++;
                    if (j == distancePointToSolid.Count - 1) break;
                }
                directionChange.Add(c);
                i += (c - 1);
            }
            //PlotInMatlab(directionChange);
            return ContainsThreadSeries(directionChange);
        }

        private static bool ContainsThreadSeries(List<double> directionChange)
        {
            // key: 2*number of threads. value: total number of the points of the series. 
            var thread = new List<double>();
            var cumulativePoi = new List<double>();
            for (var i = 0; i < directionChange.Count - 1; i++)
            {
                if (directionChange[i] < 2) continue;
                var c = 1;
                var cumulativePoints = directionChange[i];
                for (var j = i + 1; Math.Abs(directionChange[j] - directionChange[j - 1]) < 10; j++)
                {
                    if (j == directionChange.Count - 1) break;
                    if (directionChange[j] < 2) continue;
                    cumulativePoints += directionChange[j];
                    c++;
                }
                thread.Add(c);
                cumulativePoi.Add(cumulativePoints);
                i += (c - 1);
            }
            // I can use minimum common number of threads for this * 2 
            // if it is 5 and less, cumulativePoints must be less than 90 of the total number of points
            for (var i = 0; i < thread.Count; i++)
            {
                if (thread[i] > 4 && thread[i] < 6)
                    if (cumulativePoi[i] < directionChange.Sum() * 0.9)
                        return true;
                    else
                        continue;
                if (thread[i] >= 6) 
                    return true;
            }
            return false;
        }

        public static List<double> MergingEqualDistances(List<double> distancePointToSolid, double accuracy = 0.01)
        {
            // if the difference is less than 0.01 merge them
            for (var i = 0; i < distancePointToSolid.Count-1; i++)
            {
                var equals = new List<double>();
                for (var j = i + 1;
                    Math.Abs(distancePointToSolid[j] - distancePointToSolid[i]) < accuracy; j++)
                {
                    equals.Add(distancePointToSolid[j]);
                    if (j == distancePointToSolid.Count - 1)
                        break;
                }
                if (!equals.Any())
                    continue;
                var averageValue = equals.Sum()/(double) equals.Count;
                distancePointToSolid[i] = averageValue;
                distancePointToSolid.RemoveRange(i + 1, equals.Count);
                i--;
            }
            return distancePointToSolid;
        }

        private static List<double> PointToSolidDistanceCalculator(TessellatedSolid solid,
            List<double[]> kPointsBetweenMidPoints, double[] vector)
        {
            var distList = new List<double>();
            foreach (var point in kPointsBetweenMidPoints)
            {
                var ray = new Ray(new AssemblyEvaluation.Vertex(point), new Vector(vector));
                var minDis = double.PositiveInfinity;
                foreach (var face in solid.Faces)
                {
                    double[] hittingPoint;
                    if (!BasicGeometryFunctions.RayIntersectsWithFace(ray, face, out hittingPoint))
                        continue;
                    var dis = BasicGeometryFunctions.DistanceBetweenTwoVertices(hittingPoint, point);
                    if (dis < minDis) minDis = dis;
                }
                if (minDis != double.PositiveInfinity)
                    distList.Add(minDis);
            }
            // Normalizing:
            var longestDis = distList.Max();
            return distList.Select(d => d / longestDis).ToList();
        }

        private static List<double[]> KpointBtwPointsGenerator(double[] shortestEdgeMidPoint1, double[] shortestEdgeMidPoint2, int k)
        {
            // divide into k+1 equal sections
            var points =  new List<double[]>();
            var stepSize = (shortestEdgeMidPoint1.subtract(shortestEdgeMidPoint2)).divide(k + 1);
            for (var i = 0; i < k + 2; i++)
                points.Add(shortestEdgeMidPoint2.add(stepSize.multiply(i)));
            return points;
        }

        private static Vertex[] CornerEdgeFinder(PolygonalFace polygonalFace)
        {
            // We want to find the second long edge:
            var dist0 = BasicGeometryFunctions.DistanceBetweenTwoVertices(polygonalFace.Vertices[0].Position,
                polygonalFace.Vertices[1].Position);
            var dist1 = BasicGeometryFunctions.DistanceBetweenTwoVertices(polygonalFace.Vertices[0].Position,
                polygonalFace.Vertices[2].Position);
            var dist2 = BasicGeometryFunctions.DistanceBetweenTwoVertices(polygonalFace.Vertices[1].Position,
                polygonalFace.Vertices[2].Position);
            if ((dist0 > dist1 && dist0 < dist2) || (dist0 > dist2 && dist0 < dist1))
                return new[] {polygonalFace.Vertices[0], polygonalFace.Vertices[1]};
            if ((dist1 > dist0 && dist1 < dist2) || (dist1 > dist2 && dist1 < dist0))
                return new[] { polygonalFace.Vertices[0], polygonalFace.Vertices[2] };
            return new[] { polygonalFace.Vertices[1], polygonalFace.Vertices[2] };
        }

        private static double[] ShortestEdgeMidPointOfTriangle(PolygonalFace triangle)
        {
            var shortestEdge = new Vertex[2];
            var shortestDist = double.PositiveInfinity;
            for (var i = 0; i < triangle.Vertices.Count - 1; i++)
            {
                for (var j = i + 1; j < triangle.Vertices.Count; j++)
                {
                    var dis = BasicGeometryFunctions.DistanceBetweenTwoVertices(triangle.Vertices[i].Position,
                        triangle.Vertices[j].Position);
                    if (dis >= shortestDist) continue;
                    shortestDist = dis;
                    shortestEdge = new[] {triangle.Vertices[i], triangle.Vertices[j]};
                }
            }
            return (shortestEdge[0].Position.add(shortestEdge[1].Position)).divide(2.0);
        }

        public static void PlotInMatlab(List<double> distancePointToSolid)
        {
            var a = new List<double>();
            for (var i = 0; i < distancePointToSolid.Count; i++)
                a.Add(i);
            Matlabplot.Displacements(a.ToArray(), distancePointToSolid.ToArray());
        }
    }
}
