using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using StarMathLib;
using TVGL;
using Vertex = TVGL.Vertex;

namespace Assembly_Planner
{
    public class GeometryFunctions
    {
        public static double DistanceBetweenTwoVertices(double[] vertex1, double[] vertex2)
        {
            return
                Math.Sqrt((Math.Pow(vertex1[0] - vertex2[0], 2)) +
                          (Math.Pow(vertex1[1] - vertex2[1], 2)) +
                          (Math.Pow(vertex1[2] - vertex2[2], 2)));
        }

        internal static double DistanceBetweenLineAndVertex(double[] vector, double[] pointOnLine, double[] p)
        {
            var cross = vector.crossProduct(pointOnLine.subtract(p));
            return Math.Sqrt(Math.Pow(cross[0], 2) + Math.Pow(cross[1], 2) + Math.Pow(cross[2], 2));
        }

        internal static double DistanceBetweenVertexAndPlane(double[] ver, PolygonalFace plane)
        {
            // create a vector from a vertex on plane to the ver
            var vector = ver.subtract(plane.Vertices[0].Position);
            // distance is the dot product of the normal and the vector:
            return vector.dotProduct(plane.Normal);
        }

        internal static double DistanceBetweenVertexAndPlane(double[] ver, double[] planeNormal, double[] pointOnLine)
        {
            // create a vector from a vertex on plane to the ver
            var vector = ver.subtract(pointOnLine);
            // distance is the dot product of the normal and the vector:
            return vector.dotProduct(planeNormal);
        }

        internal static double DistanceBetweenTwoPlanes(PolygonalFace plane1, PolygonalFace plane2)
        {
            return
                Math.Abs((plane1.Vertices[0].Position.subtract(plane2.Vertices[0].Position)).dotProduct(plane1.Normal));
        }

        internal static List<int> ConvertCrossProductToSign(List<double[]> crossP)
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

        internal static double[] SortedEdgeLengthOfTriangle(PolygonalFace triangle)
        {
            // shortest, medium, longest. this function returns the lenght of medium
            var lengths = new List<double>
            {
                DistanceBetweenTwoVertices(triangle.Vertices[0].Position, triangle.Vertices[1].Position),
                DistanceBetweenTwoVertices(triangle.Vertices[0].Position, triangle.Vertices[2].Position),
                DistanceBetweenTwoVertices(triangle.Vertices[1].Position, triangle.Vertices[2].Position)
            };
            lengths.Sort();
            return lengths.ToArray();
        }

        internal static double[] SortedLengthOfObbEdges(BoundingBox boundingBox)
        {
            var lengths = new List<double>
            {
                DistanceBetweenTwoVertices(boundingBox.CornerVertices[0].Position,
                boundingBox.CornerVertices[1].Position),
                DistanceBetweenTwoVertices(boundingBox.CornerVertices[0].Position,
                boundingBox.CornerVertices[3].Position),
                DistanceBetweenTwoVertices(boundingBox.CornerVertices[0].Position,
                boundingBox.CornerVertices[4].Position)
            };
            lengths.Sort();
            return lengths.ToArray();
        }

        internal static Vertex[][] SortedEdgesOfTriangle(PolygonalFace triangle)
        {
            // the first one is the shortest edge, the last one is the longest
            var sorted = new Vertex[3][];
            var dic = new Dictionary<double, Vertex[]>
            {
                {
                    DistanceBetweenTwoVertices(triangle.Vertices[0].Position, triangle.Vertices[1].Position),
                    new[] {triangle.Vertices[0], triangle.Vertices[1]}
                }
            };
            var dis1 = DistanceBetweenTwoVertices(triangle.Vertices[0].Position, triangle.Vertices[2].Position);
            var dis2 = DistanceBetweenTwoVertices(triangle.Vertices[1].Position, triangle.Vertices[2].Position);
            if (dic.ContainsKey(dis1)) dis1 += 1e-6;
            dic.Add(dis1, new[] { triangle.Vertices[0], triangle.Vertices[2] });
            if (dic.ContainsKey(dis2)) dis2 += 1e-6;
            dic.Add(dis2, new[] { triangle.Vertices[1], triangle.Vertices[2] });
            var sortedKey = dic.Keys.ToList();
            sortedKey.Sort();
            sorted[0] = dic[sortedKey[0]];
            sorted[1] = dic[sortedKey[1]];
            sorted[2] = dic[sortedKey[2]];
            return sorted;
        }

        public static double[] RayIntersectionPointWithFace(Ray ray, PolygonalFace face)
        {
            // this is exclusively for fastener detection -> Partitioning bounding box and cylinder
            var w = ray.Position.subtract(face.Vertices[0].Position);
            var s1 = (face.Normal.dotProduct(w)) / (face.Normal.dotProduct(ray.Direction));
            return new[]
            {
                ray.Position[0] - s1*ray.Direction[0],
                ray.Position[1] - s1*ray.Direction[1],
                ray.Position[2] - s1*ray.Direction[2]
            };
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
            if (pointOnTrianglesPlane.subtract(ray.Position).dotProduct(ray.Direction) < 0.001)
                return false; // on the opposite side
            if (ray.Direction.dotProduct(face.Normal) > -0.06) return false;
            var v0 = face.Vertices[0].Position.subtract(pointOnTrianglesPlane);
            var v1 = face.Vertices[1].Position.subtract(pointOnTrianglesPlane);
            var v2 = face.Vertices[2].Position.subtract(pointOnTrianglesPlane);
            var crossv0v1 = v0.crossProduct(v1).normalize();
            var crossv1v2 = v1.crossProduct(v2).normalize();
            var dot = crossv0v1.dotProduct(crossv1v2);
            if (dot < 0 || double.IsNaN(dot)) return false;
            var crossv2v0 = v2.crossProduct(v0).normalize();
            dot = crossv1v2.dotProduct(crossv2v0);
            return (dot >= 0);
        }

        public static bool RayIntersectsWithFace(Ray ray, PolygonalFace face)
        {
            if (ray.Direction.dotProduct(face.Normal) > -0.06) return false;
            var w = ray.Position.subtract(face.Vertices[0].Position);
            var s1 = (face.Normal.dotProduct(w)) / (face.Normal.dotProduct(ray.Direction));
            //var v = new double[] { w[0] + s1 * ray.Direction[0] + point[0], w[1] + s1 * ray.Direction[1] + point[1], w[2] + s1 * ray.Direction[2] + point[2] };
            //var v = new double[] { ray.Position[0] - s1 * ray.Direction[0], ray.Position[1] - s1 * ray.Direction[1], ray.Position[2] - s1 * ray.Direction[2] };
            var pointOnTrianglesPlane = new[]
            {
                ray.Position[0] - s1*ray.Direction[0], ray.Position[1] - s1*ray.Direction[1],
                ray.Position[2] - s1*ray.Direction[2]
            };
            if (pointOnTrianglesPlane.subtract(ray.Position).dotProduct(ray.Direction) < 0.001) return false; // on the opposite side
            var v0 = face.Vertices[0].Position.subtract(pointOnTrianglesPlane);
            var v1 = face.Vertices[1].Position.subtract(pointOnTrianglesPlane);
            var v2 = face.Vertices[2].Position.subtract(pointOnTrianglesPlane);
            var crossv0v1 = v0.crossProduct(v1);
            var crossv1v2 = v1.crossProduct(v2);
            var dot = crossv0v1.dotProduct(crossv1v2);
            if (dot < 2.1) return false;
            var crossv2v0 = v2.crossProduct(v0);
            dot = crossv1v2.dotProduct(crossv2v0);
            return (dot >= 2.1);
        }

        public static bool RayIntersectsWithFaceFinInf(Ray ray, PolygonalFace face)
        {
            if (ray.Direction.dotProduct(face.Normal) > -0.15) return false;
            var w = ray.Position.subtract(face.Vertices[0].Position);
            var s1 = (face.Normal.dotProduct(w)) / (face.Normal.dotProduct(ray.Direction));
            //var v = new double[] { w[0] + s1 * ray.Direction[0] + point[0], w[1] + s1 * ray.Direction[1] + point[1], w[2] + s1 * ray.Direction[2] + point[2] };
            //var v = new double[] { ray.Position[0] - s1 * ray.Direction[0], ray.Position[1] - s1 * ray.Direction[1], ray.Position[2] - s1 * ray.Direction[2] };
            var pointOnTrianglesPlane = new[]
            {
                ray.Position[0] - s1*ray.Direction[0], ray.Position[1] - s1*ray.Direction[1],
                ray.Position[2] - s1*ray.Direction[2]
            };
            if (pointOnTrianglesPlane.subtract(ray.Position).dotProduct(ray.Direction) < 0)
                return false; // on the opposite side
            var v0 = face.Vertices[0].Position.subtract(pointOnTrianglesPlane);
            var v1 = face.Vertices[1].Position.subtract(pointOnTrianglesPlane);
            var v2 = face.Vertices[2].Position.subtract(pointOnTrianglesPlane);
            var crossv0v1 = v0.crossProduct(v1).normalize();
            var crossv1v2 = v1.crossProduct(v2).normalize();
            var dot = crossv0v1.dotProduct(crossv1v2);
            if (dot < -0.001 || double.IsNaN(dot)) return false;
            var crossv2v0 = v2.crossProduct(v0).normalize();
            dot = crossv1v2.dotProduct(crossv2v0);
            if (dot < -0.001) return false;
            // if the projected point in on an edge, return false
            var vec0 = face.Vertices[0].Position.subtract(face.Vertices[1].Position);
            var vec0L = DistanceBetweenTwoVertices(face.Vertices[0].Position, face.Vertices[1].Position);
            if (DistanceBetweenLineAndVertex(vec0,
                    face.Vertices[0].Position, pointOnTrianglesPlane) < 0.01 * vec0L)
                return false;
            var vec1 = face.Vertices[0].Position.subtract(face.Vertices[2].Position);
            var vec1L = DistanceBetweenTwoVertices(face.Vertices[0].Position, face.Vertices[2].Position);
            if (DistanceBetweenLineAndVertex(vec1,
                    face.Vertices[0].Position, pointOnTrianglesPlane) < 0.01 * vec1L)
                return false;
            var vec2 = face.Vertices[1].Position.subtract(face.Vertices[2].Position);
            var vec2L = DistanceBetweenTwoVertices(face.Vertices[1].Position, face.Vertices[2].Position);
            if (
                DistanceBetweenLineAndVertex(vec2,
                    face.Vertices[1].Position, pointOnTrianglesPlane) < 0.01 * vec2L)
                return false;
            return true;
        }

        public static bool RayIntersectsWithFaceNABD(Ray ray, PolygonalFace face)
        {
            if (ray.Direction.dotProduct(face.Normal) > -0.15) return false;
            var w = ray.Position.subtract(face.Vertices[0].Position);
            var s1 = (face.Normal.dotProduct(w)) / (face.Normal.dotProduct(ray.Direction));
            //var v = new double[] { w[0] + s1 * ray.Direction[0] + point[0], w[1] + s1 * ray.Direction[1] + point[1], w[2] + s1 * ray.Direction[2] + point[2] };
            //var v = new double[] { ray.Position[0] - s1 * ray.Direction[0], ray.Position[1] - s1 * ray.Direction[1], ray.Position[2] - s1 * ray.Direction[2] };
            var pointOnTrianglesPlane = new[]
            {
                ray.Position[0] - s1*ray.Direction[0], ray.Position[1] - s1*ray.Direction[1],
                ray.Position[2] - s1*ray.Direction[2]
            };
            if (pointOnTrianglesPlane.subtract(ray.Position).dotProduct(ray.Direction) < 0.001)
                return false; // on the opposite side
            var v0 = face.Vertices[0].Position.subtract(pointOnTrianglesPlane);
            var v1 = face.Vertices[1].Position.subtract(pointOnTrianglesPlane);
            var v2 = face.Vertices[2].Position.subtract(pointOnTrianglesPlane);
            var crossv0v1 = v0.crossProduct(v1).normalize();
            var crossv1v2 = v1.crossProduct(v2).normalize();
            var dot = crossv0v1.dotProduct(crossv1v2);
            if (dot < -0.001 || double.IsNaN(dot)) return false;
            var crossv2v0 = v2.crossProduct(v0).normalize();
            dot = crossv1v2.dotProduct(crossv2v0);
            return (dot >= -0.001);
        }

        internal static PolygonalFace[] LongestPlaneOfObbDetector(BoundingBox obb, out PolygonalFace facePrepToRD1,
    out PolygonalFace facePrepToRD2)
        {
            // it returns longest side. (two adjacent triangles)
            var dis1 = DistanceBetweenTwoVertices(obb.CornerVertices[0].Position, obb.CornerVertices[1].Position);
            var dis2 = DistanceBetweenTwoVertices(obb.CornerVertices[0].Position, obb.CornerVertices[3].Position);
            var dis3 = DistanceBetweenTwoVertices(obb.CornerVertices[0].Position, obb.CornerVertices[4].Position);
            var cornerVer = obb.CornerVertices;
            if (dis1 >= dis2 && dis1 >= dis3)
            {
                facePrepToRD1 =
                    new PolygonalFace(new[] { new Vertex(cornerVer[0].Position), new Vertex(cornerVer[3].Position),
                        new Vertex(cornerVer[7].Position) }, ((cornerVer[0].Position.subtract(cornerVer[3].Position)).crossProduct(
                            cornerVer[7].Position.subtract(cornerVer[3].Position))).normalize());
                facePrepToRD2 =
                    new PolygonalFace(new[] { new Vertex(cornerVer[1].Position), new Vertex(cornerVer[2].Position),
                        new Vertex(cornerVer[6].Position) }, ((cornerVer[6].Position.subtract(cornerVer[2].Position)).crossProduct(
                            cornerVer[1].Position.subtract(cornerVer[2].Position))).normalize());
                return new[]
                {
                    new PolygonalFace(new[] {new Vertex(cornerVer[0].Position), new Vertex(cornerVer[1].Position),
                        new Vertex(cornerVer[3].Position)}, ((cornerVer[3].Position.subtract(cornerVer[0].Position)).crossProduct(
                            cornerVer[1].Position.subtract(cornerVer[0].Position))).normalize()),
                    new PolygonalFace(new[] {new Vertex(cornerVer[1].Position),new Vertex(cornerVer[2].Position),
                        new Vertex(cornerVer[3].Position)}, ((cornerVer[1].Position.subtract(cornerVer[2].Position)).crossProduct(
                            cornerVer[3].Position.subtract(cornerVer[2].Position))).normalize())
                };
            }
            if (dis2 >= dis1 && dis2 >= dis3)
            {
                facePrepToRD1 =
                    new PolygonalFace(new[] { new Vertex(cornerVer[1].Position), new Vertex(cornerVer[0].Position),
                        new Vertex(cornerVer[4].Position) }, ((cornerVer[1].Position.subtract(cornerVer[0].Position)).crossProduct(
                            cornerVer[4].Position.subtract(cornerVer[0].Position))).normalize());
                facePrepToRD2 =
                    new PolygonalFace(new[] { new Vertex(cornerVer[2].Position), new Vertex(cornerVer[3].Position),
                        new Vertex(cornerVer[7].Position) }, ((cornerVer[7].Position.subtract(cornerVer[3].Position)).crossProduct(
                            cornerVer[2].Position.subtract(cornerVer[3].Position))).normalize());
                return new[]
                {
                    new PolygonalFace(new[] {new Vertex(cornerVer[0].Position), new Vertex(cornerVer[1].Position),
                        new Vertex(cornerVer[3].Position)}, ((cornerVer[3].Position.subtract(cornerVer[0].Position)).crossProduct(
                            cornerVer[1].Position.subtract(cornerVer[0].Position))).normalize()),
                    new PolygonalFace(new[] {new Vertex(cornerVer[1].Position), new Vertex(cornerVer[2].Position),
                        new Vertex(cornerVer[3].Position)}, ((cornerVer[1].Position.subtract(cornerVer[2].Position)).crossProduct(
                            cornerVer[3].Position.subtract(cornerVer[2].Position))).normalize())
                };
            }
            if (dis3 >= dis2 && dis3 >= dis1)
            {
                facePrepToRD1 =
                    new PolygonalFace(new[] { new Vertex(cornerVer[0].Position), new Vertex(cornerVer[1].Position), new Vertex(cornerVer[3].Position) },
                        ((cornerVer[3].Position.subtract(cornerVer[0].Position)).crossProduct(
                            cornerVer[1].Position.subtract(cornerVer[0].Position))).normalize());
                facePrepToRD2 =
                    new PolygonalFace(new[] { new Vertex(cornerVer[4].Position), new Vertex(cornerVer[5].Position), new Vertex(cornerVer[7].Position) },
                        ((cornerVer[5].Position.subtract(cornerVer[4].Position)).crossProduct(
                            cornerVer[7].Position.subtract(cornerVer[4].Position))).normalize());
                return new[]
                {
                    new PolygonalFace(new[] {new Vertex(cornerVer[1].Position), new Vertex(cornerVer[0].Position), new Vertex(cornerVer[4].Position)},
                        ((cornerVer[1].Position.subtract(cornerVer[0].Position)).crossProduct(
                            cornerVer[4].Position.subtract(cornerVer[0].Position))).normalize()),
                    new PolygonalFace(new[] {new Vertex(cornerVer[1].Position), new Vertex(cornerVer[5].Position), new Vertex(cornerVer[4].Position)},
                        ((cornerVer[4].Position.subtract(cornerVer[5].Position)).crossProduct(
                            cornerVer[1].Position.subtract(cornerVer[5].Position))).normalize())
                };
            }
            facePrepToRD1 = null;
            facePrepToRD2 = null;
            return null;
        }

        internal static PolygonalFace[] LongestPlaneOfObbDetector(double[][] obb, bool clockWise, out PolygonalFace facePrepToRD1,
            out PolygonalFace facePrepToRD2)
        {
            // I am not using this function anymore
            // it returns longest side. (two adjacent triangles)
            var dis1 = DistanceBetweenTwoVertices(obb[0], obb[1]);
            var dis2 = DistanceBetweenTwoVertices(obb[0], obb[3]);
            var dis3 = DistanceBetweenTwoVertices(obb[0], obb[4]);
            var a = clockWise ? 10 : 20;
            if (dis1 >= dis2 && dis1 >= dis3)
            {
                var normal1 = ((obb[3].subtract(obb[0])).crossProduct(obb[4].subtract(obb[0]))).normalize();
                facePrepToRD1 =
                    new PolygonalFace(new[] { new Vertex(obb[0]), new Vertex(obb[3]), new Vertex(obb[4]) },
                        clockWise ? normal1.multiply(-1.0) : normal1);
                var normal2 = ((obb[5].subtract(obb[1])).crossProduct(obb[2].subtract(obb[1]))).normalize();
                facePrepToRD2 =
                    new PolygonalFace(new[] { new Vertex(obb[1]), new Vertex(obb[2]), new Vertex(obb[5]) },
                        clockWise ? normal2.multiply(-1.0) : normal2);
                var normal3 = ((obb[1].subtract(obb[0])).crossProduct(obb[3].subtract(obb[0]))).normalize();
                var normal4 = ((obb[3].subtract(obb[2])).crossProduct(obb[1].subtract(obb[2]))).normalize();
                return new[]
                {
                    new PolygonalFace(new[] {new Vertex(obb[0]), new Vertex(obb[1]), new Vertex(obb[3])},
                        clockWise ? normal3.multiply(-1.0) : normal3),
                    new PolygonalFace(new[] {new Vertex(obb[1]), new Vertex(obb[2]), new Vertex(obb[3])},
                        clockWise ? normal4.multiply(-1.0) : normal4)
                };
            }
            if (dis2 >= dis1 && dis2 >= dis3)
            {
                var normal1 = ((obb[4].subtract(obb[0])).crossProduct(obb[1].subtract(obb[0]))).normalize();
                facePrepToRD1 =
                    new PolygonalFace(new[] { new Vertex(obb[0]), new Vertex(obb[1]), new Vertex(obb[4]) },
                        clockWise ? normal1.multiply(-1.0) : normal1);
                var normal2 = ((obb[2].subtract(obb[3])).crossProduct(obb[7].subtract(obb[3]))).normalize();
                facePrepToRD2 =
                    new PolygonalFace(new[] { new Vertex(obb[2]), new Vertex(obb[3]), new Vertex(obb[7]) },
                        clockWise ? normal2.multiply(-1.0) : normal2);
                var normal3 = ((obb[1].subtract(obb[0])).crossProduct(obb[3].subtract(obb[0]))).normalize();
                var normal4 = ((obb[3].subtract(obb[2])).crossProduct(obb[1].subtract(obb[2]))).normalize();
                return new[]
                {
                    new PolygonalFace(new[] {new Vertex(obb[0]), new Vertex(obb[1]), new Vertex(obb[3])},
                        clockWise ? normal3.multiply(-1.0) : normal3),
                    new PolygonalFace(new[] {new Vertex(obb[1]), new Vertex(obb[2]), new Vertex(obb[3])},
                        clockWise ? normal4.multiply(-1.0) : normal4)
                };
            }
            if (dis3 >= dis2 && dis3 >= dis1)
            {
                var normal1 = ((obb[1].subtract(obb[0])).crossProduct(obb[3].subtract(obb[0]))).normalize();
                facePrepToRD1 =
                    new PolygonalFace(new[] { new Vertex(obb[0]), new Vertex(obb[1]), new Vertex(obb[3]) },
                        clockWise ? normal1.multiply(-1.0) : normal1);
                var normal2 = ((obb[7].subtract(obb[4])).crossProduct(obb[5].subtract(obb[4]))).normalize();
                facePrepToRD2 =
                    new PolygonalFace(new[] { new Vertex(obb[5]), new Vertex(obb[4]), new Vertex(obb[7]) },
                        clockWise ? normal2.multiply(-1.0) : normal2);
                var normal3 = ((obb[4].subtract(obb[0])).crossProduct(obb[1].subtract(obb[0]))).normalize();
                var normal4 = ((obb[1].subtract(obb[5])).crossProduct(obb[4].subtract(obb[5]))).normalize();
                return new[]
                {
                    new PolygonalFace(new[] {new Vertex(obb[0]), new Vertex(obb[1]), new Vertex(obb[4])},
                       clockWise ? normal3.multiply(-1.0) : normal3),
                    new PolygonalFace(new[] {new Vertex(obb[1]), new Vertex(obb[4]), new Vertex(obb[5])},
                        clockWise ? normal4.multiply(-1.0) : normal4)
                };
            }
            facePrepToRD1 = null;
            facePrepToRD2 = null;
            return null;
        }

        internal static PolygonalFace BoundingCylindertoFaceOnObbRespectiveSide(TessellatedSolid solid,
            out Vertex[] startingVerts, out double[] partnCreationVect)
        {
            var bCy = BoundingGeometry.BoundingCylinderDic[solid];
            // Find a triangle in OBB which has the middle edge length equal to cylinder length.
            var obbCornerVer = BoundingGeometry.OrientedBoundingBoxDic[solid].CornerVertices;
            var dis1 = DistanceBetweenTwoVertices(obbCornerVer[0].Position, obbCornerVer[1].Position);
            var dis2 = DistanceBetweenTwoVertices(obbCornerVer[0].Position, obbCornerVer[3].Position);
            var dis3 = DistanceBetweenTwoVertices(obbCornerVer[0].Position, obbCornerVer[4].Position);
            if (Math.Abs(dis1 - bCy.Length) < 1e-5)
            {
                startingVerts = new[] { new Vertex(obbCornerVer[0].Position), new Vertex(obbCornerVer[3].Position) };
                partnCreationVect = obbCornerVer[1].Position.subtract(obbCornerVer[0].Position);
                return new PolygonalFace(new[]
                {
                    new Vertex(obbCornerVer[0].Position), new Vertex(obbCornerVer[1].Position),
                    new Vertex(obbCornerVer[3].Position)
                }, ((obbCornerVer[3].Position.subtract(obbCornerVer[0].Position)).crossProduct(
                    obbCornerVer[1].Position.subtract(obbCornerVer[0].Position))).normalize());
            }
            if (Math.Abs(dis2 - bCy.Length) < 1e-5)
            {
                startingVerts = new[] { new Vertex(obbCornerVer[0].Position), new Vertex(obbCornerVer[1].Position) };
                partnCreationVect = obbCornerVer[3].Position.subtract(obbCornerVer[0].Position);
                return new PolygonalFace(new[]
                {
                    new Vertex(obbCornerVer[0].Position), new Vertex(obbCornerVer[1].Position),
                    new Vertex(obbCornerVer[3].Position)
                }, ((obbCornerVer[3].Position.subtract(obbCornerVer[0].Position)).crossProduct(
                    obbCornerVer[1].Position.subtract(obbCornerVer[0].Position))).normalize());
            }
            if (Math.Abs(dis3 - bCy.Length) > 1e-5)
                throw new Exception("Length of the Bounding Cylinder is not equal to the length of any OBB edges");

            startingVerts = new[] { new Vertex(obbCornerVer[0].Position), new Vertex(obbCornerVer[1].Position) };
            partnCreationVect = obbCornerVer[4].Position.subtract(obbCornerVer[0].Position);
            return
                new PolygonalFace(
                    new[]
                    {
                        new Vertex(obbCornerVer[1].Position), new Vertex(obbCornerVer[0].Position),
                        new Vertex(obbCornerVer[4].Position)
                    },
                    ((obbCornerVer[1].Position.subtract(obbCornerVer[0].Position)).crossProduct(
                        obbCornerVer[4].Position.subtract(obbCornerVer[0].Position))).normalize());
        }

        public static double FindMaxPlaneHeightInDirection(IEnumerable<Vertex> points, double[] direction)
        {
            return points.Max(point => direction.dotProduct(point.Position));
        }
        public static double FindMinPlaneHeightInDirection(IEnumerable<Vertex> points, double[] direction)
        {
            return points.Min(point => direction.dotProduct(point.Position));
        }
    }
}
