using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GeometryReasoning;
using GraphSynth.Representation;
using MIConvexHull;
using StarMathLib;
using TVGL;
using Constants = AssemblyEvaluation.Constants;

namespace Assembly_Planner
{
    internal class NonadjacentBlockingDetermination
    {
        internal static Dictionary<int, List<NonAdjacentBlockings>> NonAdjacentBlocking =
            new Dictionary<int, List<NonAdjacentBlockings>>();

        static readonly List<int[]> binaryFaceIndices = new List<int[]>
        {
           new []{0,0,0}, new []{0,0,1},new []{0,1,0},new []{0,1,1},
           new []{1,0,0}, new []{1,0,1},new []{1,1,0},new []{1,1,1},
        };

        internal static void Run(designGraph graph,
            List<TessellatedSolid> solids, List<int> gDir)
        {
            
            foreach (var dir in gDir)
            {
                var direction = DisassemblyDirections.Directions[dir];
                var blockingsForDirection = new List<NonAdjacentBlockings>();
                foreach (var solid in solids.Where(s=> graph.nodes.Any(n=>n.name == s.Name)))
                {
                    // now find the blocking parts
                    var rays = new List<Ray>();
                    foreach (var vertex in solid.ConvexHullVertices)
                        rays.Add(new Ray(new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1], vertex.Position[2]),
                                        new Vector(direction[0], direction[1], direction[2])));

                    foreach (var solidBlocking in 
                        solids.Where(s => graph.nodes.Any(n => n.name == s.Name) // it is not fastener
                                          && s != solid // it is not the same as current solid 
                                          &&
                                          !graph.arcs.Any(a => // there is no arc between the current and the candidate
                                              (a.From.name == solid.Name && a.To.name == s.Name) ||
                                              (a.From.name == s.Name && a.To.name == solid.Name))))
                    {
                        if (!BoundingBoxBlocking(direction, solidBlocking, solid)) continue; //there is a problem with this code
                        var distanceToTheClosestFace = double.PositiveInfinity;
                        var overlap = false;
                        if (BlockingDetermination.ConvexHullOverlap(solid, solidBlocking))
                        {
                            foreach (var ray in rays)
                            {
                                foreach (
                                    var blockingPolygonalFace in
                                        solidBlocking.Faces.Where(f => RayIntersectsWithFace(ray, f)).ToList())
                                {
                                    overlap = true;
                                    var d = DistanceToTheFace(ray.Position, blockingPolygonalFace);
                                    if (d < distanceToTheClosestFace) distanceToTheClosestFace = d;
                                }
                            }
                        }
                        else
                        {
                            foreach (var ray in rays)
                            {
                                if (solidBlocking.ConvexHullFaces.Any(f => RayIntersectsWithFace(ray, f)))
                                //now find the faces that intersect with the ray and find the distance between them
                                {
                                    foreach (
                                        var blockingPolygonalFace in
                                            solidBlocking.Faces.Where(f => RayIntersectsWithFace(ray, f)).ToList())
                                    {
                                        overlap = true;
                                        var d = DistanceToTheFace(ray.Position, blockingPolygonalFace);
                                        if (d < distanceToTheClosestFace) distanceToTheClosestFace = d;
                                    }
                                }
                            }
                        }
                        if (overlap)
                            blockingsForDirection.Add(new NonAdjacentBlockings
                            {
                                blockingSolids = new[] { solid, solidBlocking },
                                blockingDistance = distanceToTheClosestFace
                            });
                    }
                }
                NonAdjacentBlocking.Add(dir, blockingsForDirection);
            }
        }


        private static bool BoundingBoxBlocking(double[] v, TessellatedSolid partBlo, TessellatedSolid partMov)
        {
            var blockingBoundingBox = new[] { partBlo.XMin, partBlo.XMax, partBlo.YMin, partBlo.YMax, partBlo.ZMin, partBlo.ZMax };
            var movingBoundingBox = new[] { partMov.XMin, partMov.XMax, partMov.YMin, partMov.YMax, partMov.ZMin, partMov.ZMax };
            return BoundingBoxBlocking(v, blockingBoundingBox, movingBoundingBox);
        }

        private static bool BoundingBoxBlocking(double[] v, double[] blockingBox, double[] movingPartBox)
        {
            // 1. if bounding box overlap, return true?
            if (!(movingPartBox[0] > blockingBox[1] + 0.1) && !(movingPartBox[2] > blockingBox[3] + 0.1) &&
                !(movingPartBox[4] > blockingBox[5] + 0.1) &&
                !(blockingBox[0] > movingPartBox[1] + 0.1) && !(blockingBox[2] > movingPartBox[3] + 0.1) &&
                !(blockingBox[4] > movingPartBox[5] + 0.1))
                return true;

            var facingCornerIndices = new int[3];
            lock (facingCornerIndices)
            {
                for (int i = 0; i < 3; i++)
                {
                    var signOfElement = Math.Sign(v[i]);
                    if (signOfElement > 0 && movingPartBox[2 * i] > blockingBox[2 * i + 1])
                        return false;
                    if (signOfElement < 0 && movingPartBox[2 * i + 1] < blockingBox[2 * i])
                        return false;
                    if (signOfElement > 0) facingCornerIndices[i] = 1;
                }
                var complementaryCornerIndices = new[] { (1 - facingCornerIndices[0]), (1 - facingCornerIndices[1]), (1 - facingCornerIndices[2]) };

                var movingCompleCorner = new[]
                {
                    movingPartBox[complementaryCornerIndices[0]],
                    movingPartBox[complementaryCornerIndices[1] + 2],
                    movingPartBox[complementaryCornerIndices[2] + 4]
                };
                var blockingFacingCorner = new[]
                {
                    blockingBox[facingCornerIndices[0]],
                    blockingBox[facingCornerIndices[1] + 2],
                    blockingBox[facingCornerIndices[2] + 4]
                };

                var movingDxPlaneMin = v.dotProduct(movingCompleCorner, 3);
                var blockingDxPlaneMax = v.dotProduct(blockingFacingCorner, 3);
                if (movingDxPlaneMin > blockingDxPlaneMax) return false;
                var superficialBloackingFace = new DefaultConvexFace<AssemblyEvaluation.Vertex>
                {
                    Vertices = new AssemblyEvaluation.Vertex[6],
                    Normal = v
                };
                var index = 0;
                for (int i = 0; i < 8; i++)
                {
                    var vertexIndices = binaryFaceIndices[i];
                    if (vertexIndices[0] == facingCornerIndices[0] && vertexIndices[1] == facingCornerIndices[1] &&
                        vertexIndices[2] == facingCornerIndices[2]) continue;
                    if (vertexIndices[0] == complementaryCornerIndices[0] &&
                        vertexIndices[1] == complementaryCornerIndices[1] &&
                        vertexIndices[2] == complementaryCornerIndices[2]) continue;
                    superficialBloackingFace.Vertices[index] = new AssemblyEvaluation.Vertex(new[]
                    {
                        blockingBox[vertexIndices[0]],
                        blockingBox[vertexIndices[1] + 2],
                        blockingBox[vertexIndices[2] + 4]
                    });
                    index++;
                }

                for (int i = 0; i < 8; i++)
                {
                    var vertexIndices = binaryFaceIndices[i];
                    if (vertexIndices[0] == facingCornerIndices[0] && vertexIndices[1] == facingCornerIndices[1] &&
                        vertexIndices[2] == facingCornerIndices[2]) continue;
                    if (vertexIndices[0] == complementaryCornerIndices[0] &&
                        vertexIndices[1] == complementaryCornerIndices[1] &&
                        vertexIndices[2] == complementaryCornerIndices[2]) continue;
                    var ray = new Ray(new AssemblyEvaluation.Vertex(new[]
                    {
                        movingPartBox[vertexIndices[0]],
                        movingPartBox[vertexIndices[1] + 2],
                        movingPartBox[vertexIndices[2] + 4]
                    }),
                        new Vector(v[0], v[1], v[2]));
                    if (STLGeometryFunctions.RayIntersectsWithFace(ray, superficialBloackingFace)) return true;
                }
                return false;
            }
        }

        public static bool RayIntersectsWithFace(Ray ray, PolygonalFace face)
        {
            if (Math.Abs(ray.Direction.dotProduct(face.Normal)) < Constants.NearlyParallelFace) return false;
            var inPlaneVerts = new AssemblyEvaluation.Vertex[3];
            var negativeDirCounter = 3;
            for (var i = 0; i < 3; i++)
            {
                var vectFromRayToFacePt = new Vector(ray.Position);
                vectFromRayToFacePt = vectFromRayToFacePt.MakeVectorTo(face.Vertices[i]); // Interesting
                var dxtoPlane = ray.Direction.dotProduct(vectFromRayToFacePt.Position);
                if (dxtoPlane < 0) negativeDirCounter--;
                if (negativeDirCounter == 0) return false;
                inPlaneVerts[i] = new AssemblyEvaluation.Vertex(face.Vertices[i].Position.add(StarMath.multiply(-dxtoPlane, ray.Direction)));
            }
            if (inPlaneVerts.Min(v => v.Position[0]) > ray.Position[0]) return false;
            if (inPlaneVerts.Max(v => v.Position[0]) < ray.Position[0]) return false;
            if (inPlaneVerts.Min(v => v.Position[1]) > ray.Position[1]) return false;
            if (inPlaneVerts.Max(v => v.Position[1]) < ray.Position[1]) return false;
            if (inPlaneVerts.Min(v => v.Position[2]) > ray.Position[2]) return false;
            if (inPlaneVerts.Max(v => v.Position[2]) < ray.Position[2]) return false;
            if (inPlaneVerts.GetLength(0) > 3) return true;
            var crossProductsToCorners = new List<double[]>();
            for (int i = 0; i < 3; i++)
            {
                var j = (i == 2) ? 0 : i + 1;
                var crossProductsFrom_i_To_j =
                    inPlaneVerts[i].Position.subtract(ray.Position)
                        .normalizeInPlace(3)
                        .crossProduct(inPlaneVerts[j].Position.subtract(ray.Position).normalizeInPlace(3));
                if (crossProductsFrom_i_To_j.norm2(true) < Constants.NearlyOnLine) return false;
                crossProductsToCorners.Add(crossProductsFrom_i_To_j);
            }
            for (int i = 0; i < 3; i++)
            {
                var j = (i == 2) ? 0 : i + 1;
                if (crossProductsToCorners[i].dotProduct(crossProductsToCorners[j], 3) <= 0) return false; // 0.15
            }
            return true;
        }

        public static bool RayIntersectsWithFace2(Ray ray, PolygonalFace face)
        {
            var raysPointOnFacePlane = MiscFunctions.PointOnPlaneFromRay(face.Normal,
                face.Center.dotProduct(face.Normal), ray.Position, ray.Direction);
            if (raysPointOnFacePlane == null) return false;

            var crossProductsToCorners = new List<double[]>();
            for (int i = 0; i < 3; i++)
            {
                var j = (i == 2) ? 0 : i + 1;
                var crossProductsFrom_i_To_j =
                    face.Vertices[i].Position.subtract(raysPointOnFacePlane).normalize()
                        .crossProduct(face.Vertices[j].Position.subtract(ray.Position).normalize());
                if (crossProductsFrom_i_To_j.norm2(true) < Constants.NearlyOnLine) return false;
                crossProductsToCorners.Add(crossProductsFrom_i_To_j);
            }
            for (int i = 0; i < 3; i++)
            {
                var j = (i == 2) ? 0 : i + 1;
                if (crossProductsToCorners[i].dotProduct(crossProductsToCorners[j], 3) <= 0) return false; // 0.15
            }
            return true;
        }


        private static double DistanceToTheFace(double[] p, PolygonalFace blockingPolygonalFace)
        {
            return
                Math.Abs(blockingPolygonalFace.Normal.dotProduct(p.subtract(blockingPolygonalFace.Vertices[0].Position)));
        }



        internal static void FiniteDirectionsBetweenConnectedParts(TessellatedSolid solid1, TessellatedSolid solid2, List<int> localDirInd, out List<int> finDirs, out List<int> infDirs)
        {
            // solid1 is Reference and solid2 is Moving
            finDirs = new List<int>();
            infDirs = new List<int>();

            var aa = new List<double>();
            foreach (var dir in localDirInd)
            {
                var direction = DisassemblyDirections.Directions[dir];
                var rays = new List<Ray>();
                foreach (var vertex in solid2.Vertices)
                    rays.Add(new Ray(new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1], vertex.Position[2]),
                                    new Vector(direction[0], direction[1], direction[2])));
                //if (rays.Any(ray => solid1.Faces.Any(f => RayIntersectsWithFace(ray, f))))
                foreach (var ray in rays)
                {
                    foreach (var f in solid1.Faces)
                    {
                        if (RayIntersectsWithFace(ray, f))
                        {
                            finDirs.Add(dir);
                            aa.Add(DistanceToTheFace(ray.Position, f));
                        }
                    }
                }
                //{
                //   finDirs.Add(dir);
                //    var a = DistanceToTheFace()
                // }
                // else
                //    infDirs.Add(dir);
            }
        }
    }


    internal class NonAdjacentBlockings
    {
      /// <summary>
      /// blockingSolids[0] is blocked by blockingSolids[1]
      /// </summary>
      /// <value>
      /// blockingSolids
      /// </value>
      internal TessellatedSolid[] blockingSolids { get; set; }
      /// <summary>
      /// for each blockingSolids[], a double is added to this list. So, 
      /// blockingDistance is the distance between blockingSolids[0]
      /// and blockingSolids[1]
      /// </summary>
      /// <value>
      /// blockingDistance
      /// </value>
      internal double blockingDistance { get; set; }
    }
}
