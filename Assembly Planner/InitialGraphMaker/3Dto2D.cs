using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;
using TVGL;
using TVGL;

namespace Assembly_Planner
{
    class _3Dto2D
    {
        /// <summary>
        /// Gets or sets the 3D solid.
        /// </summary>
        /// <value>The 3D.</value>
        public TessellatedSolid ThreeD { get; set; }

        /// <summary>
        /// Gets or sets the Vertices of 2D.
        /// </summary>
        /// <value>The Vertices of 2D.</value>
        public Point[] Points { get; set; }


        /// <summary>
        /// Gets or sets the Edges of 2D.
        /// </summary>
        /// <value>The Edges of 2D.</value>
        public List<Point[]> Edges { get; set; }


        public static List<Point[]> Get2DEdges(TessellatedSolid solid, Point[] Points2D)
        {
            var edges3D = solid.Edges;
            var vertices = solid.Vertices.ToList();
            var edges2D = new List<Point[]>();
            foreach (var edge in edges3D)
            {
                var edge2D = new Point[2];
                edge2D[0] = Points2D[vertices.IndexOf(edge.From)];
                edge2D[1] = Points2D[vertices.IndexOf(edge.To)];
                edges2D.Add(edge2D);
            }
            return edges2D;
        }

        public static List<Point[]> Get2DEdges2(List<Edge> edges3D, List<TVGL.Vertex> vertices, TVGL.Point[] points2D)
        {
            var edges2D = new List<Point[]>();
            foreach (var edge in edges3D)
            {
                var edge2D = new Point[2];
                edge2D[0] = points2D[vertices.IndexOf(edge.From)];
                edge2D[1] = points2D[vertices.IndexOf(edge.To)];
                edges2D.Add(edge2D);
            }
            return edges2D;
        }

        public static Point[] Get2DProjectionPoints(IList<Vertex> vertices, double[] direction,
            Boolean MergeDuplicateReferences = false)
        {
            var transform = TransformToXYPlane(direction);
            return Get2DProjectionPoints(vertices, transform, MergeDuplicateReferences);
        }

        private static double[,] TransformToXYPlane(double[] direction)
        {
            double[,] backTransformStandIn;
            return TransformToXYPlane(direction, out backTransformStandIn);
        }
        private static double[,] TransformToXYPlane(double[] direction, out double[,] backTransform)
        {
            var xDir = direction[0];
            var yDir = direction[1];
            var zDir = direction[2];

            double[,] rotateX, rotateY, backRotateX, backRotateY;
            if (xDir == 0 && zDir == 0)
            {
                rotateX = StarMath.RotationX(Math.Sign(yDir) * Math.PI / 2, true);
                backRotateX = StarMath.RotationX(-Math.Sign(yDir) * Math.PI / 2, true);
                backRotateY = rotateY = StarMath.makeIdentity(4);
            }
            else if (zDir == 0)
            {
                rotateY = StarMath.RotationY(-Math.Sign(xDir) * Math.PI / 2, true);
                backRotateY = StarMath.RotationY(Math.Sign(xDir) * Math.PI / 2, true);
                var rotXAngle = Math.Atan(yDir / Math.Abs(xDir));
                rotateX = StarMath.RotationX(rotXAngle, true);
                backRotateX = StarMath.RotationX(-rotXAngle, true);
            }
            else
            {
                var rotYAngle = Math.Atan(xDir / zDir);
                rotateY = StarMath.RotationY(-rotYAngle, true);
                backRotateY = StarMath.RotationY(rotYAngle, true);
                var baseLength = Math.Sqrt(xDir * xDir + zDir * zDir);
                var rotXAngle = Math.Atan(yDir / baseLength);
                rotateX = StarMath.RotationX(rotXAngle, true);
                backRotateX = StarMath.RotationX(-rotXAngle, true);
            }
            backTransform = backRotateY.multiply(backRotateX);
            return rotateX.multiply(rotateY);
        }

        public static Point[] Get2DProjectionPoints(IList<Vertex> vertices, double[,] transform,
            Boolean MergeDuplicateReferences = false)
        {
            var points = new List<Point>();
            var pointAs4 = new[] { 0.0, 0.0, 0.0, 1.0 };
            foreach (var vertex in vertices)
            {
                pointAs4[0] = vertex.Position[0];
                pointAs4[1] = vertex.Position[1];
                pointAs4[2] = vertex.Position[2];
                pointAs4 = transform.multiply(pointAs4);
                var point2D = new[] { pointAs4[0], pointAs4[1] };
                if (MergeDuplicateReferences)
                {
                    var sameIndex = points.FindIndex(p => p.Position2D.IsPracticallySame(point2D));
                    if (sameIndex >= 0)
                        points[sameIndex].References.Add(vertex);
                    points.Add(new Point(vertex, pointAs4[0], pointAs4[1], pointAs4[2]));
                }
                points.Add(new Point(vertex, pointAs4[0], pointAs4[1], pointAs4[2]));
            }
            return points.ToArray();
        }
    }
}
