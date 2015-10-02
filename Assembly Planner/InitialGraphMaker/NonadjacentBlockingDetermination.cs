using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AssemblyEvaluation;
using Assembly_Planner.GraphSynth.BaseClasses;
using GeometryReasoning;
using GraphSynth.Representation;
using MIConvexHull;
using StarMathLib;
using TVGL;
using Vertex = TVGL.Vertex;

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

            //Parallel.ForEach(gDir, dir =>
            foreach (var dir in gDir)
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
                                new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1],
                                    vertex.Position[2]),
                                new Vector(direction[0], direction[1], direction[2])));
                    // add more vertices to the ray
                    rays.AddRange(
                        AddingMoreRays(solid.ConvexHullEdges.Where(e => e != null && e.Length > 2).ToArray(),
                            direction));

                    foreach (var solidBlocking in
                        solids.Where(s => graph.nodes.Any(n => n.name == s.Name) // it is not fastener
                                          && s != solid // it is not the same as current solid 
                                          &&
                                          !graph.arcs.Any(a => // there is no arc between the current and the candidate
                                              (a.From.name == solid.Name && a.To.name == s.Name) ||
                                              (a.From.name == s.Name && a.To.name == solid.Name))))
                    {
                         
                        if (!BoundingBoxBlocking(direction, solidBlocking, solid)) continue;
                        var distanceToTheClosestFace = double.PositiveInfinity;
                        var overlap = false;

                        if (BlockingDetermination.ConvexHullOverlap(solid, solidBlocking))
                        {
                            overlap = ConvexHullOverlappNonAdjacent(rays, solid, solidBlocking, direction);
                            continue;
                        }
                        else
                        {
                            overlap = DontConcexHullsOverlapNonAdjacent(rays, solidBlocking);
                        }
                        if (overlap)
                            blockingsForDirection.Add(new NonAdjacentBlockings
                            {
                                blockingSolids = new[] {solid, solidBlocking},
                                blockingDistance = distanceToTheClosestFace
                            });
                    }
                }
                //);
              //  lock (NonAdjacentBlocking)
                    NonAdjacentBlocking.Add(dir, blockingsForDirection);
            }
             //   );
            // To be fixed later:
            //    This ConvertToSecondary can be later added directly after generation.
            //ConvertToSecondaryArc(graph, NonAdjacentBlocking);
        }

        private static bool DontConcexHullsOverlapNonAdjacent(List<Ray> rays, TessellatedSolid solidBlocking)
        {
            var overlap = false;
            foreach (var ray in rays)
            {
                if (solidBlocking.ConvexHullFaces.Any(f => RayIntersectsWithFace3(ray, f)))
                //now find the faces that intersect with the ray and find the distance between them
                {
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
                    if (overlap) break;
                }
            }
            return overlap;
        }

        private static bool ConvexHullOverlappNonAdjacent(List<Ray> rays, TessellatedSolid solid, TessellatedSolid solidBlocking, double[] direction)
        {
            var boo = false;
            foreach (var ray in rays)
            {
                foreach (
                    var blockingPolygonalFace in
                        solidBlocking.Faces.Where(f => RayIntersectsWithFace3(ray, f)).ToList())
                {
                    var d = DistanceToTheFace(ray.Position, blockingPolygonalFace);
                    //if (d < distanceToTheClosestFace) distanceToTheClosestFace = d;
                    if (d < 0) continue;
                    boo = true;
                    break;
                }
                if (boo)
                {
                    var blocked = _2DProjectionCheck(solid, solidBlocking, direction);
                    if (!blocked) 
                        boo = false;
                    break;
                }
            }
            return boo;
        }

        internal static bool _2DProjectionCheck(TessellatedSolid solid, TessellatedSolid solidBlocking, double[] direction)
        {
            var movingProj = _3Dto2D.Get2DProjectionPoints(solid.Vertices, direction);
            var moving2D = new _3Dto2D
            {
                ThreeD = solid,
                Points = movingProj,
                Edges = _3Dto2D.Get2DEdges(solid, movingProj)
            };
            var referenceProj = _3Dto2D.Get2DProjectionPoints(solidBlocking.Vertices, direction);
            var reference2D = new _3Dto2D
            {
                ThreeD = solidBlocking,
                Points = referenceProj,
                Edges = _3Dto2D.Get2DEdges(solidBlocking, referenceProj)
            };
            var blocked =
                moving2D.Edges.Any(
                    movEdge =>
                        reference2D.Edges.Any(
                            refEdge =>
                                NonadjacentBlockingDeterminationPro.DoIntersect(movEdge, refEdge)));
            return blocked;
        }

        private static void ConvertToSecondaryArc(designGraph graph, Dictionary<int, List<NonAdjacentBlockings>> NonAdjacentBlocking)
        {
            foreach (var key in NonAdjacentBlocking.Keys.ToList())
            {
                var dirs = (from gDir in DisassemblyDirections.Directions
                    where
                        1 - Math.Abs(gDir.dotProduct(DisassemblyDirections.Directions[key])) <
                        ConstantsPrimitiveOverlap.CheckWithGlobDirsParall
                    select DisassemblyDirections.Directions.IndexOf(gDir)).ToList();
                var oppositeDir = dirs.Where(d => d != key).ToList();
                foreach (var blockings in NonAdjacentBlocking[key])
                {
                    var from = graph.nodes.Where(n => n.name == blockings.blockingSolids[0].Name).Cast<Component>().ToList()[0]; // blocked
                    var to = graph.nodes.Where(n => n.name == blockings.blockingSolids[1].Name).Cast<Component>().ToList()[0];  // blocking
                    
                    // if the opprosite direction exists for to and from instead of from and to, do not add the arc.
                    // if a is blocked by b in z direction, b is blocked by a in -z direction. So no need to add an additional arc.

                    if (dirs.Count > 1)
                    {
                        if (
                            graph.arcs.Where(arc => arc is SecondaryConnection)
                                .Cast<SecondaryConnection>()
                                .Any(
                                    SC =>
                                        (SC.From == to && SC.To == from && SC.Directions.Contains(oppositeDir[0])) ||
                                        (SC.From == from && SC.To == to && SC.Directions.Contains(key))))
                            continue;
                    }

                    var existing = graph.arcs.OfType<SecondaryConnection>()
                        .Where(exist => (exist.From == from && exist.To == to) || (exist.To == from && exist.From == to))
                        .ToList();
                    if (existing.Any())
                    {
                        if (existing[0].From == from && existing[0].To == to && !existing[0].Directions.Contains(key))
                        {
                            existing[0].Directions.Add(key);
                        }
                        else
                        {
                            if (oppositeDir.Any() && !existing[0].Directions.Contains(oppositeDir[0]))
                            {
                                existing[0].Directions.Add(oppositeDir[0]);
                            }
                        }
                    }
                    else
                    {
                        graph.addArc(from, to, "", typeof(SecondaryConnection));
                        var a = (SecondaryConnection)graph.arcs.Last();
                        a.Directions.Add(key);
                        //a.Distance = blockings.blockingDistance;
                    }
                }
            }
        }

        internal static IEnumerable<Ray> AddingMoreRays(Edge[] edges, double[] dir)
        {
            //For the case that two objects are nonadacently blocking each other but the rays shot from the corner
            //vertices cannot detect them, we will add more vertices here to solve this issue.
            var sections = edges.Select(edge => new[] {new AssemblyEvaluation.Vertex(edge.From.Position), new AssemblyEvaluation.Vertex(edge.To.Position)}).ToList();
            var newRays = new List<Ray>();
            var counter = 0;
            while (counter < 1)
            {
                counter++;
                var section2 = new List<AssemblyEvaluation.Vertex[]>();
                foreach (var sec in sections)
                {
                    var aP = sec[0];
                    var bP = sec[1];
                    var midPoint = new AssemblyEvaluation.Vertex((aP.Position[0] + bP.Position[0]) / 2.0, (aP.Position[1] + bP.Position[1]) / 2.0, (aP.Position[2] + bP.Position[2]) / 2.0);
                    newRays.Add(new Ray(midPoint, new Vector(dir)));
                    section2.Add(new[] { aP, midPoint });
                    section2.Add(new[] { midPoint, bP });
                }
                sections = new List<AssemblyEvaluation.Vertex[]>(section2);
            }
            return newRays;
        }


        internal static bool BoundingBoxBlocking(double[] v, TessellatedSolid partBlo, TessellatedSolid partMov)
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
                return newApproachBoundingBox(v,blockingBox,movingPartBox);
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

        private static bool newApproachBoundingBox(double[] v, double[] bB, double[] mB)
        {
            var mV1 = new TVGL.Vertex(new[] { mB[0], mB[2], mB[4] });
            var mV2 = new TVGL.Vertex(new[] { mB[0], mB[3], mB[4] });
            var mV3 = new TVGL.Vertex(new[] { mB[1], mB[3], mB[4] });
            var mV4 = new TVGL.Vertex(new[] { mB[1], mB[2], mB[4] });
            var mV5 = new TVGL.Vertex(new[] { mB[0], mB[2], mB[5] });
            var mV6 = new TVGL.Vertex(new[] { mB[0], mB[3], mB[5] });
            var mV7 = new TVGL.Vertex(new[] { mB[1], mB[3], mB[5] });
            var mV8 = new TVGL.Vertex(new[] { mB[1], mB[2], mB[5] });
            var movingVer = new List<Vertex> {mV1, mV2, mV3, mV4, mV5, mV6, mV7, mV8};
            var mE1 = new Edge(mV1, mV2, true);
            var mE2 = new Edge(mV1, mV4, true);
            var mE3 = new Edge(mV1, mV5, true);
            var mE4 = new Edge(mV2, mV3, true);
            var mE5 = new Edge(mV2, mV6, true);
            var mE6 = new Edge(mV7, mV6, true);
            var mE7 = new Edge(mV7, mV3, true);
            var mE8 = new Edge(mV7, mV8, true);
            var mE9 = new Edge(mV5, mV6, true);
            var mE10 = new Edge(mV5, mV8, true);
            var mE11 = new Edge(mV4, mV3, true);
            var mE12 = new Edge(mV4, mV8, true);
            var movingEdg = new List<Edge> { mE1, mE2, mE3, mE4, mE5, mE6, mE7, mE8, mE9, mE10, mE11, mE12 };

            var bV1 = new TVGL.Vertex(new[] { bB[0], bB[2], bB[4] });
            var bV2 = new TVGL.Vertex(new[] { bB[0], bB[3], bB[4] });
            var bV3 = new TVGL.Vertex(new[] { bB[1], bB[3], bB[4] });
            var bV4 = new TVGL.Vertex(new[] { bB[1], bB[2], bB[4] });
            var bV5 = new TVGL.Vertex(new[] { bB[0], bB[2], bB[5] });
            var bV6 = new TVGL.Vertex(new[] { bB[0], bB[3], bB[5] });
            var bV7 = new TVGL.Vertex(new[] { bB[1], bB[3], bB[5] });
            var bV8 = new TVGL.Vertex(new[] { bB[1], bB[2], bB[5] });
            var blockingVer = new List<Vertex> {bV1, bV2, bV3, bV4, bV5, bV6, bV7, bV8};
            var bE1 = new Edge(bV1, bV2, true);
            var bE2 = new Edge(bV1, bV4, true);
            var bE3 = new Edge(bV1, bV5, true);
            var bE4 = new Edge(bV2, bV3, true);
            var bE5 = new Edge(bV2, bV6, true);
            var bE6 = new Edge(bV7, bV6, true);
            var bE7 = new Edge(bV7, bV3, true);
            var bE8 = new Edge(bV7, bV8, true);
            var bE9 = new Edge(bV5, bV6, true);
            var bE10 = new Edge(bV5, bV8, true);
            var bE11 = new Edge(bV4, bV3, true);
            var bE12 = new Edge(bV4, bV8, true);
            var blockingEdg = new List<Edge> { bE1, bE2, bE3, bE4, bE5, bE6, bE7, bE8, bE9, bE10, bE11, bE12 };

            var movingProj = _3Dto2D.Get2DProjectionPoints(movingVer, v);
            var blockingProj = _3Dto2D.Get2DProjectionPoints(blockingVer, v);
            var moving2D = new _3Dto2D { Points = movingProj, Edges = _3Dto2D.Get2DEdges2(movingEdg,movingVer, movingProj) };
            var blocking2D = new _3Dto2D { Points = blockingProj, Edges = _3Dto2D.Get2DEdges2(blockingEdg, blockingVer, movingProj) };
            return moving2D.Edges.Any(movEdge => blocking2D.Edges.Any(refEdge => NonadjacentBlockingDeterminationPro.DoIntersect(movEdge, refEdge)));
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
            if (ray.Direction.dotProduct(face.Normal) > -0.06) return false;
            var w = ray.Position.subtract(face.Vertices[0].Position);
            var s1 = (face.Normal.dotProduct(w)) / (face.Normal.dotProduct(ray.Direction));
            //var v = new double[] { w[0] + s1 * ray.Direction[0] + point[0], w[1] + s1 * ray.Direction[1] + point[1], w[2] + s1 * ray.Direction[2] + point[2] };
            //var v = new double[] { ray.Position[0] - s1 * ray.Direction[0], ray.Position[1] - s1 * ray.Direction[1], ray.Position[2] - s1 * ray.Direction[2] };
            var pointOnTrianglesPlane = new [] { ray.Position[0] - s1 * ray.Direction[0], ray.Position[1] - s1 * ray.Direction[1], ray.Position[2] - s1 * ray.Direction[2] };
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
            /*foreach (var corner in face.Vertices)
            {
                var otherCorners = face.Vertices.Where(ver => ver != corner).ToList();
                var v1 = otherCorners[0].Position.subtract(corner.Position);
                var v2 = otherCorners[1].Position.subtract(corner.Position);
                var v0 = v.subtract(corner.Position);
                if (v1.crossProduct(v0).dotProduct(v2.crossProduct(v0)) > -0.15) //   -0.09 
                    return false;
            }
            return true;*/

        }

        internal static double DistanceToTheFace(double[] p, PolygonalFace blockingPolygonalFace)
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
                foreach (var vertex in solid2.ConvexHullVertices)
                    rays.Add(new Ray(new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1], vertex.Position[2]),
                                    new Vector(direction[0], direction[1], direction[2])));
                var direction2 = DisassemblyDirections.Directions[dir].multiply(-1.0);
                var rays2 = new List<Ray>();
                foreach (var vertex in solid1.ConvexHullVertices)
                    rays2.Add(new Ray(new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1], vertex.Position[2]),
                                    new Vector(direction2[0], direction2[1], direction2[2])));
                if (rays.Any(ray => solid1.Faces.Any(f => RayIntersectsWithFace3(ray, f) && DistanceToTheFace(ray.Position, f) > 0)))
                {
                   finDirs.Add(dir);
                }
                else if (rays2.Any(ray => solid2.Faces.Any(f => RayIntersectsWithFace3(ray, f) && DistanceToTheFace(ray.Position, f) > 0)))
                    finDirs.Add(dir);
                else
                    infDirs.Add(dir);
            }
        }

        internal static void FiniteDirectionsBetweenConnectedPartsWithPartitioning(TessellatedSolid solid1,
            TessellatedSolid solid2, List<int> localDirInd, out List<int> finDirs, out List<int> infDirs)
        {
            // solid1 is Reference and solid2 is Moving
            finDirs = new List<int>();
            infDirs = new List<int>();
            var boo = false;

            foreach (var dir in localDirInd)
            {
                var finite = false;
                
                var direction = DisassemblyDirections.Directions[dir];
                var rays = new List<Ray>();
                foreach (var vertex in solid2.ConvexHullVertices)
                    rays.Add(new Ray(new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1], vertex.Position[2]),
                                    new Vector(direction[0], direction[1], direction[2])));
                finite = IsTheLocalDirectionFinite(solid1, rays);
                
                if (finite)
                {
                    finDirs.Add(dir);
                    continue;
                }
                
                var direction2 = DisassemblyDirections.Directions[dir].multiply(-1.0);
                var rays2 = new List<Ray>();
                foreach (var vertex in solid1.ConvexHullVertices)
                    rays2.Add(new Ray(new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1], vertex.Position[2]),
                                    new Vector(direction2[0], direction2[1], direction2[2])));
                finite = IsTheLocalDirectionFinite(solid2, rays2);

                if (finite)
                    finDirs.Add(dir);
                else 
                    infDirs.Add(dir);
            }
        }

        private static bool IsTheLocalDirectionFinite(TessellatedSolid blockingSolid, List<Ray> rays)
        {
            foreach (var ray in rays)
            {
                var memoFace = new HashSet<PolygonalFace>();
                var affectedPartitions = NonadjacentBlockingWithPartitioning.AffectedPartitionsWithRayCvhOverlaps(blockingSolid, ray);
                foreach (var prtn in affectedPartitions)
                {
                    foreach (var t in prtn.SolidTriangles.Where(t=>!memoFace.Contains(t)))
                    {
                        memoFace.Add(t);
                        if (RayIntersectsWithFace3(ray, t)) return true;
                    }
                }
            }
            return false;
        }
    }

    [Serializable]
    [XmlRoot("NonAdjacent")]
    public class NonAdjacentBlockings
    {
      /// <summary>
      /// blockingSolids[0] is blocked by blockingSolids[1]
      /// </summary>
      /// <value>
      /// blockingSolids
      /// </value>
      [XmlIgnore]
        public TessellatedSolid[] blockingSolids { get; set; }
      /// <summary>
      /// for each blockingSolids[], a double is added to this list. So, 
      /// blockingDistance is the distance between blockingSolids[0]
      /// and blockingSolids[1]
      /// </summary>
      /// <value>
      /// blockingDistance
      /// </value>
        public double blockingDistance { get; set; }

        //public int[] blockingSolidIndices { get; set; }
    }
}
