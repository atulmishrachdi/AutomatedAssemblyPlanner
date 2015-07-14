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

            foreach (var dir in gDir)
            {
                var direction = DisassemblyDirections.Directions[dir];
                var blockingsForDirection = new List<NonAdjacentBlockings>();
                foreach (var solid in solids.Where(s => graph.nodes.Any(n => n.name == s.Name)))
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
