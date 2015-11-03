using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
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
            PolygonalFace f1;
            PolygonalFace f2;
            var longestSide = BoltAndGearDetection.LongestPlaneOfObbDetector(obb, out f1, out f2);
            // 1. Take the middle point of the smallest edge of each triangle. 
            // 2. Generate k points between them with equal distances. 
            // 3. Generate rays using generate points. 
            var shortestEdgeMidPoint1 = ShortestEdgeMidPointOfTriangle(longestSide[0]);
            var shortestEdgeMidPoint2 = ShortestEdgeMidPointOfTriangle(longestSide[1]);
            
            var kPointsBetweenMidPoints = KpointBtwMidPointsGenerator(shortestEdgeMidPoint1, shortestEdgeMidPoint2, 500);

            var distancePointToSolid = PointToSolidDistanceCalculator(solid, kPointsBetweenMidPoints,
                longestSide[0].Normal.multiply(-1.0));
            var a = new List<double>();
            for (var i = 0; i < distancePointToSolid.Count; i++)
                a.Add(i);
            Matlabplot.Displacements(a.ToArray(), distancePointToSolid.ToArray());

            return false;
        }

        private static List<double> PointToSolidDistanceCalculator(TessellatedSolid solid,
            List<double[]> kPointsBetweenMidPoints, double[] vector)
        {
            var distList = new List<double>();
            foreach (var point in kPointsBetweenMidPoints)
            {
                var ray = new Ray(new AssemblyEvaluation.Vertex(point), new Vector(vector));
                foreach (var face in solid.Faces)
                {
                    double[] hittingPoint;
                    if (!BasicGeometryFunctions.RayIntersectsWithFace(ray, face, out hittingPoint))
                        continue;
                    distList.Add(BasicGeometryFunctions.DistanceBetweenTwoVertices(hittingPoint, point));
                    break;
                }
            }
            return distList;
        }

        private static List<double[]> KpointBtwMidPointsGenerator(double[] shortestEdgeMidPoint1, double[] shortestEdgeMidPoint2, int k)
        {
            // divide into k+1 equal sections
            var points =  new List<double[]>();
            var stepSize = (shortestEdgeMidPoint1.subtract(shortestEdgeMidPoint2)).divide(k + 1);
            for (var i = 0; i < k + 2; i++)
                points.Add(shortestEdgeMidPoint2.add(stepSize.multiply(i)));
            return points;
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
    }
}
