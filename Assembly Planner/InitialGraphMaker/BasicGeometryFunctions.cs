using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using StarMathLib;
using TVGL;

namespace Assembly_Planner
{
    public class BasicGeometryFunctions
    {
        public static double DistanceBetweenTwoVertices(double[] vertex1, double[] vertex2)
        {
            return
                Math.Sqrt((Math.Pow(vertex1[0] - vertex2[0], 2)) +
                          (Math.Pow(vertex1[1] - vertex2[1], 2)) +
                          (Math.Pow(vertex1[2] - vertex2[2], 2)));
        }

        public static bool RayIntersectsWithFace(Ray ray, PolygonalFace face, out double[] hittingPoint, out bool outer)
        {
            //if (ray.Direction.dotProduct(face.Normal) > -0.06) return false;
            var w = ray.Position.subtract(face.Vertices[0].Position);
            var s1 = (face.Normal.dotProduct(w)) / (face.Normal.dotProduct(ray.Direction));
            //var v = new double[] { w[0] + s1 * ray.Direction[0] + point[0], w[1] + s1 * ray.Direction[1] + point[1], w[2] + s1 * ray.Direction[2] + point[2] };
            //var v = new double[] { ray.Position[0] - s1 * ray.Direction[0], ray.Position[1] - s1 * ray.Direction[1], ray.Position[2] - s1 * ray.Direction[2] };
            var pointOnTrianglesPlane = new[] { ray.Position[0] - s1 * ray.Direction[0], ray.Position[1] - s1 * ray.Direction[1], ray.Position[2] - s1 * ray.Direction[2] };
            hittingPoint = pointOnTrianglesPlane;
            outer = true;
            var v0 = face.Vertices[0].Position.subtract(pointOnTrianglesPlane);
            var v1 = face.Vertices[1].Position.subtract(pointOnTrianglesPlane);
            var v2 = face.Vertices[2].Position.subtract(pointOnTrianglesPlane);
            var crossv0v1 = v0.crossProduct(v1);
            var crossv1v2 = v1.crossProduct(v2);
            var dot = crossv0v1.dotProduct(crossv1v2);
            if (dot < 0.0) return false;
            var crossv2v0 = v2.crossProduct(v0);
            dot = crossv1v2.dotProduct(crossv2v0);
            outer = !(ray.Direction.dotProduct(face.Normal) > -0.06);
            return (dot >= 0.0);
        }

        public static bool RayIntersectsWithFace(Ray ray, PolygonalFace face)
        {
            if (ray.Direction.dotProduct(face.Normal) > -0.06) return false;
            var w = ray.Position.subtract(face.Vertices[0].Position);
            var s1 = (face.Normal.dotProduct(w)) / (face.Normal.dotProduct(ray.Direction));
            //var v = new double[] { w[0] + s1 * ray.Direction[0] + point[0], w[1] + s1 * ray.Direction[1] + point[1], w[2] + s1 * ray.Direction[2] + point[2] };
            //var v = new double[] { ray.Position[0] - s1 * ray.Direction[0], ray.Position[1] - s1 * ray.Direction[1], ray.Position[2] - s1 * ray.Direction[2] };
            var pointOnTrianglesPlane = new[] { ray.Position[0] - s1 * ray.Direction[0], ray.Position[1] - s1 * ray.Direction[1], ray.Position[2] - s1 * ray.Direction[2] };
            var v0 = face.Vertices[0].Position.subtract(pointOnTrianglesPlane);
            var v1 = face.Vertices[1].Position.subtract(pointOnTrianglesPlane);
            var v2 = face.Vertices[2].Position.subtract(pointOnTrianglesPlane);
            var crossv0v1 = v0.crossProduct(v1);
            var crossv1v2 = v1.crossProduct(v2);
            var dot = crossv0v1.dotProduct(crossv1v2);
            if (dot < 0.0) return false;
            var crossv2v0 = v2.crossProduct(v0);
            dot = crossv1v2.dotProduct(crossv2v0);
            return (dot >= 0.0);
        }

        internal static double DistanceBetweenLineAndPoint(double[] vector, double[] pointOnLine, double[] p)
        {
            var cross = vector.crossProduct(pointOnLine.subtract(p));
            return Math.Sqrt(Math.Pow(cross[0], 2) + Math.Pow(cross[1], 2) + Math.Pow(cross[2], 2));
        }


        internal static List<int> ConvertCrossToSign(List<double[]> crossP)
        {
            var signs = new List<int> { 1 };
            var mainCross = crossP[0];
            for (var i = 1; i < crossP.Count; i++)
            {
                var cross2 = crossP[i];
                if ((Math.Sign(mainCross[0]) != Math.Sign(cross2[0]) ||
                     (Math.Sign(mainCross[0]) == 0 && Math.Sign(cross2[0]) == 0)) &&
                    (Math.Sign(mainCross[1]) != Math.Sign(cross2[1]) ||
                     (Math.Sign(mainCross[1]) == 0 && Math.Sign(cross2[1]) == 0)) &&
                    (Math.Sign(mainCross[2]) != Math.Sign(cross2[2]) ||
                     (Math.Sign(mainCross[2]) == 0 && Math.Sign(cross2[2]) == 0)))
                    signs.Add(-1);
                else
                    signs.Add(1);
            }
            return signs;
        }

        internal static double LongestLengthOfObb(BoundingBox boundingBox)
        {
            var dist1 = DistanceBetweenTwoVertices(boundingBox.CornerVertices[0].Position,
                boundingBox.CornerVertices[1].Position);
            var dist2 = DistanceBetweenTwoVertices(boundingBox.CornerVertices[0].Position,
                boundingBox.CornerVertices[3].Position);
            var dist3 = DistanceBetweenTwoVertices(boundingBox.CornerVertices[0].Position,
                boundingBox.CornerVertices[4].Position);
            return Math.Max(Math.Max(dist1, dist2), dist3);
        }

        internal static double MediumEdgeLengthOfTriangle(PolygonalFace triangle)
        {
            // shortest, medium, longest. this function returns the lenght of medium
            var dist1 = DistanceBetweenTwoVertices(boundingBox.CornerVertices[0].Position,
                boundingBox.CornerVertices[1].Position);
            var dist2 = DistanceBetweenTwoVertices(boundingBox.CornerVertices[0].Position,
                boundingBox.CornerVertices[3].Position);
            var dist3 = DistanceBetweenTwoVertices(boundingBox.CornerVertices[0].Position,
                boundingBox.CornerVertices[4].Position);
            return Math.Max(Math.Max(dist1, dist2), dist3);
        }
    }
}
