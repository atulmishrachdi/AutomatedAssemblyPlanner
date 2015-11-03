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
            PolygonalFace triangle1;
            PolygonalFace triangle2;
            BoltAndGearDetection.LongestPlaneOfObbDetector(obb, out triangle1, out triangle2);
            // 1. Take the middle point of the smallest edge of each triangle. 
            // 2. Generate k points between them with equal distances. 
            // 3. Generate rays using generate points. 
            var shortestEdgeMidPoint1 = ShortestEdgeMidPointOfTriangle(triangle1);
            var shortestEdgeMidPoint2 = ShortestEdgeMidPointOfTriangle(triangle2);
            
            var kPointsBetweenMidPoints = KpointBtwMidPointsGenerator(shortestEdgeMidPoint1, shortestEdgeMidPoint2, 100);

            var distancePointToSolid = PointToSolidDistanceCalculator(solid, kPointsBetweenMidPoints,
                triangle1.Normal.multiply(-1.0));


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
                points.Add(shortestEdgeMidPoint1.add(stepSize.multiply(i)));
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
            return (shortestEdge[1].Position.add(shortestEdge[2].Position)).divide(2.0);
        }
    }
}
