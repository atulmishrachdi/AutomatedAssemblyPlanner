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

            Parallel.ForEach(gDir, dir =>
            //foreach (var dir in gDir)
            {
                var direction = DisassemblyDirections.Directions[dir];
                var blockingsForDirection = new List<NonAdjacentBlockings>();

                //Parallel.ForEach(solids.Where(s => graph.nodes.Any(n => n.name == s.Name)), solid =>
                foreach (var solid in solids.Where(s => graph.nodes.Any(n => n.name == s.Name)))
                {
                    // now find the blocking parts
                    var rays = new List<Ray>();
                    foreach (var vertex in solid.ConvexHullVertices)
                        rays.Add(
                            new Ray(
                                new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1], vertex.Position[2]),
                                new Vector(direction[0], direction[1], direction[2])));

                    foreach (var solidBlocking in
                        solids.Where(s => graph.nodes.Any(n => n.name == s.Name) // it is not fastener
                                          && s != solid // it is not the same as current solid 
                                          &&
                                          !graph.arcs.Any(a => // there is no arc between the current and the candidate
                                              (a.From.name == solid.Name && a.To.name == s.Name) ||
                                              (a.From.name == s.Name && a.To.name == solid.Name))))
                    {
                        /*if (!BoundingBoxBlocking(direction, solidBlocking, solid))
                            continue; //there is a problem with this code
                        var distanceToTheClosestFace = double.PositiveInfinity;
                        var overlap = false;
                        foreach (var ray in rays)
                        {
                            if (solidBlocking.ConvexHullFaces.Any(f => RayIntersectsWithFace2(ray, f)))
                                //now find the faces that intersect with the ray and find the distance between them
                            {
                                foreach (
                                    var blockingPolygonalFace in
                                        solidBlocking.Faces.Where(f => RayIntersectsWithFace2(ray, f)).ToList())
                                {
                                    overlap = true;
                                    var d = DistanceToTheFace(ray.Position, blockingPolygonalFace);
                                    if (d < distanceToTheClosestFace) distanceToTheClosestFace = d;
                                }
                            }
                        }
                        if (overlap)
                            blockingsForDirection.Add(new NonAdjacentBlockings
                            {
                                blockingSolids = new[] {solid, solidBlocking},
                                blockingDistance = distanceToTheClosestFace
                            });
                    }
                }
                NonAdjacentBlocking.Add(dir, blockingsForDirection);*/
                        if (!BoundingBoxBlocking(direction, solidBlocking, solid)) continue;
                        var distanceToTheClosestFace = double.PositiveInfinity;
                        var overlap = false;
                        if (BlockingDetermination.ConvexHullOverlap(solid, solidBlocking))
                        {
                            foreach (var ray in rays)
                            {
                                //var m = solidBlocking.Faces.Where(f => RayIntersectsWithFace3(ray, f)).ToList();
                                //var f55 = new List<double>();
                                //foreach (var d in m)
                                //{
                                //    f55.Add(ray.Direction.dotProduct(d.Normal));
                                //}
                                foreach (
                                    var blockingPolygonalFace in
                                        solidBlocking.Faces.Where(f => RayIntersectsWithFace3(ray, f)).ToList())
                                {
                                    var d = DistanceToTheFace(ray.Position, blockingPolygonalFace);
                                    //if (d < distanceToTheClosestFace) distanceToTheClosestFace = d;
                                    if (d < 0) continue;
                                    overlap = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            foreach (var ray in rays)
                            {
                                if (solidBlocking.ConvexHullFaces.Any(f => RayIntersectsWithFace3(ray, f)))
                                //now find the faces that intersect with the ray and find the distance between them
                                {
                                    //var m = solidBlocking.Faces.Where(f => RayIntersectsWithFace3(ray, f)).ToList();
                                    //var f55 = new List<double>();
                                    //foreach (var d in m)
                                    //{
                                    //    f55.Add(ray.Direction.dotProduct(d.Normal));
                                    //}
                                    foreach (
                                        var blockingPolygonalFace in
                                            solidBlocking.Faces.Where(f => RayIntersectsWithFace3(ray, f)).ToList())
                                    {
                                        var d = DistanceToTheFace(ray.Position, blockingPolygonalFace);
                                        //if (d < distanceToTheClosestFace) distanceToTheClosestFace = d;
                                        if (d < 0) continue;
                                        overlap = true;
                                        break;
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
                //);
                lock (NonAdjacentBlocking)
                NonAdjacentBlocking.Add(dir, blockingsForDirection);
            }
            );
        }


        private static bool BoundingBoxBlocking(double[] v, TessellatedSolid partBlo, TessellatedSolid partMov)
        {
            if (BlockingDetermination.BoundingBoxOverlap(partBlo, partMov)) return true;
            var blockingBoundingBox = new[] { partBlo.XMin, partBlo.XMax, partBlo.YMin, partBlo.YMax, partBlo.ZMin, partBlo.ZMax };
            var movingBoundingBox = new[] { partMov.XMin, partMov.XMax, partMov.YMin, partMov.YMax, partMov.ZMin, partMov.ZMax };
            return BoundingBoxBlocking(v, blockingBoundingBox, movingBoundingBox);
        }

        private static bool BoundingBoxBlocking(double[] v, double[] blockingBox, double[] movingPartBox)
        {    // think of facingCornerIndices as the corner of a unit cube that spans from {0,0,0} to {1,1,1}
            // in the loop below we find what corner of the blockBox in nearest the moving some like {0,1,0}
            var facingCornerIndices = new int[3];
            lock (facingCornerIndices)
            {
                for (int i = 0; i < 3; i++)    //for each direction: x, y, z
                {
                    int signOfElement = Math.Sign(v[i]);     //get the sign of the direction in each x, y, z
                    if (signOfElement > 0 && movingPartBox[2 * i] > blockingBox[2 * i + 1])
                        return false;  // if sign of x (or y or z) is positive and moving is already on the
                                       // positive side of reference then there is no way to interact
                    if (signOfElement < 0 && movingPartBox[2 * i + 1] < blockingBox[2 * i])
                        return false;  // if sign of x (or y or z) is negative and moving is already on the
                    // negative side of reference then there is no way to interact
                    if (signOfElement > 0) facingCornerIndices[i] = 1;      // put a one in the spot if the direction is positive.
                    
                }
                // the complementaryCornerIndices is the opposite corner if facingCornerIndices = {0,1,0} then 
                // complementaryCornerIndices = {1,0,1}
                var complementaryCornerIndices = new[] { (1 - facingCornerIndices[0]), (1 - facingCornerIndices[1]), (1 - facingCornerIndices[2]) };
                // get the corner coordinates on the two boxes
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
                // if the movingDxPlaneMin is greater than the blockingDxPlaneMax,
                // then the moving is already along a direction that won't interfere with the blocking     
                if (movingDxPlaneMin > blockingDxPlaneMax) return false;

                // set up a single face with the normal along the direction v in which the other 6 corner of the
                // block make some sort of hexagonal face
                var superficialBlockingFace = new DefaultConvexFace<AssemblyEvaluation.Vertex>
                {
                    Vertices = new AssemblyEvaluation.Vertex[6],
                    Normal = v
                };
                var index = 0;
                for (int i = 0; i < 8; i++)
                {
                    var vertexIndices = binaryFaceIndices[i];
                    if (vertexIndices[0] == facingCornerIndices[0] && 
                        vertexIndices[1] == facingCornerIndices[1] &&
                        vertexIndices[2] == facingCornerIndices[2]) continue;
                    if (vertexIndices[0] == complementaryCornerIndices[0] &&
                        vertexIndices[1] == complementaryCornerIndices[1] &&
                        vertexIndices[2] == complementaryCornerIndices[2]) continue;
                    superficialBlockingFace.Vertices[index] = new AssemblyEvaluation.Vertex(new[]
                    {
                        blockingBox[vertexIndices[0]],
                        blockingBox[vertexIndices[1] + 2],
                        blockingBox[vertexIndices[2] + 4]
                    });
                    index++;
                }
                // find the 6 corners of the moving that are not the extreme corners found above and
                // check if each ray from that corner pass through the above face
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
                    // bug: the following function will only work if the face is convex (which it is) and the
                    // points are provided in the proper order (which they are not). Furthermore, the six
                    // corners are not part of a plane, so I'm not sure this will work out.
                    if (STLGeometryFunctions.RayIntersectsWithFace(ray, superficialBlockingFace)) return true;
                }
                return false;
            }
        }

        public static bool RayIntersectsWithFace(Ray ray, PolygonalFace face)
        {
            if (Math.Abs(ray.Direction.dotProduct(face.Normal)) < AssemblyEvaluation.Constants.NearlyParallelFace) return false;
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
                if (crossProductsFrom_i_To_j.norm2(true) < AssemblyEvaluation.Constants.NearlyOnLine) return false;
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
            if (ray.Direction.dotProduct(face.Normal) > -0.06) return false;
            var raysPointOnFacePlane = MiscFunctions.PointOnPlaneFromRay(face.Normal,
                face.Vertices[0].Position.dotProduct(face.Normal), ray.Position, ray.Direction);
            if (raysPointOnFacePlane == null) return false;

            var crossProductsToCorners = new List<double[]>();
            for (int i = 0; i < 3; i++)
            {
                var j = (i == 2) ? 0 : i + 1;
                var crossProductsFrom_i_To_j =
                    face.Vertices[i].Position.subtract(raysPointOnFacePlane).normalize()
                        .crossProduct(face.Vertices[j].Position.subtract(ray.Position).normalize());
                if (crossProductsFrom_i_To_j.norm2(true) < AssemblyEvaluation.Constants.NearlyOnLine) return false;
                crossProductsToCorners.Add(crossProductsFrom_i_To_j);
            }
            for (int i = 0; i < 3; i++)
            {
                var j = (i == 2) ? 0 : i + 1;
                if (crossProductsToCorners[i].dotProduct(crossProductsToCorners[j], 3) <= 0) return false; // 0.15
            }
            return true;
        }

        public static bool RayIntersectsWithFace3(Ray ray, PolygonalFace face)
        {
            if (ray.Direction.dotProduct(face.Normal) > -0.04) return false;
            //var point = new double[] { (face.Vertices[0].Position[0] + face.Vertices[1].Position[0] + face.Vertices[2].Position[0]) / 3.0, 
            //    (face.Vertices[0].Position[1] + face.Vertices[1].Position[1] + face.Vertices[2].Position[2]) / 3.0, 
            //    (face.Vertices[0].Position[2] + face.Vertices[1].Position[2] + face.Vertices[2].Position[2]) / 3.0 };
            var point = face.Vertices[0].Position;
            var w = new double[] { ray.Position[0] - point[0], ray.Position[1] - point[1], ray.Position[2] - point[2] };
            var s1 = (face.Normal.dotProduct(w)) / (face.Normal.dotProduct(ray.Direction));
            //var v = new double[] { w[0] + s1 * ray.Direction[0] + point[0], w[1] + s1 * ray.Direction[1] + point[1], w[2] + s1 * ray.Direction[2] + point[2] };
            var v = new double[] { ray.Position[0] - s1 * ray.Direction[0], ray.Position[1] - s1 * ray.Direction[1], ray.Position[2] - s1 * ray.Direction[2] };
            
            if (v == null) return false;
            foreach (var corner in face.Vertices)
            {
                var otherCorners = face.Vertices.Where(ver => ver != corner).ToList();
                var v1 = otherCorners[0].Position.subtract(corner.Position);
                var v2 = otherCorners[1].Position.subtract(corner.Position);
                var v0 = v.subtract(corner.Position);
                if (v1.crossProduct(v0).dotProduct(v2.crossProduct(v0)) > -0.09)
                    return false;
            }
            return true;

        }

        private static double DistanceToTheFace(double[] p, PolygonalFace blockingPolygonalFace)
        {
            return
                blockingPolygonalFace.Normal.dotProduct(p.subtract(blockingPolygonalFace.Vertices[0].Position));
        }



        internal static void FiniteDirectionsBetweenConnectedParts(TessellatedSolid solid1, TessellatedSolid solid2, List<int> localDirInd, out List<int> finDirs, out List<int> infDirs)
        {
            // solid1 is Reference and solid2 is Moving
            finDirs = new List<int>();
            infDirs = new List<int>();
            foreach (var dir in localDirInd)
            {
                var direction = DisassemblyDirections.Directions[dir];
                var rays = new List<Ray>();
                foreach (var vertex in solid2.Vertices)
                    rays.Add(new Ray(new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1], vertex.Position[2]),
                                    new Vector(direction[0], direction[1], direction[2])));
                var direction2 = DisassemblyDirections.Directions[dir].multiply(-1.0);
                var rays2 = new List<Ray>();
                foreach (var vertex in solid1.Vertices)
                    rays2.Add(new Ray(new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1], vertex.Position[2]),
                                    new Vector(direction2[0], direction2[1], direction2[2])));
                if (rays.Any(ray => solid1.Faces.Any(f => RayIntersectsWithFace3(ray, f) && DistanceToTheFace(ray.Position, f) > 0))||
                    rays2.Any(ray => solid2.Faces.Any(f => RayIntersectsWithFace3(ray, f) && DistanceToTheFace(ray.Position, f) > 0)))
                {
                   finDirs.Add(dir);
                }
                else
                    infDirs.Add(dir);
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
