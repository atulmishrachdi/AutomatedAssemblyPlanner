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
using TVGL.Tessellation;

namespace Assembly_Planner
{
    internal class UnconnectedBlockingDetermination
    {

        static readonly List<int[]> binaryFaceIndices = new List<int[]>
        {
           new []{0,0,0}, new []{0,0,1},new []{0,1,0},new []{0,1,1},
           new []{1,0,0}, new []{1,0,1},new []{1,1,0},new []{1,1,1},
        };

        internal static Dictionary<hyperarc, List<hyperarc>> Run(Dictionary<hyperarc, List<hyperarc>> dbgDictionary,
            Dictionary<hyperarc, List<hyperarc>> connectedButUnblocked, int cndDirInd)
        {
            // this is a really slow function. But it seems to be necessary
            foreach (var scc1 in dbgDictionary.Keys)
            {
                var blocked = false;
                foreach (var scc2 in dbgDictionary.Keys.Where(s => s != scc1 && !dbgDictionary[scc1].Contains(s) && !connectedButUnblocked[scc1].Contains(s)))
                {
                    var verticesScc1 = new List<TVGL.Tessellation.Vertex>();
                    var verticesScc2 = new List<TVGL.Tessellation.Vertex>();
                    var facesScc1 = new List<PolygonalFace>();
                    var facesScc2 = new List<PolygonalFace>();
                    foreach (var node in scc1.nodes)
                    {
                        foreach (var solid in DisassemblyDirections.Solids.Where(solid => solid.Name == node.name))
                        {
                            facesScc1.AddRange(solid.Faces);
                            break;
                        }
                        foreach (var solid in DisassemblyDirections.Solids.Where(solid => solid.Name == node.name))
                        {
                            verticesScc1.AddRange(solid.Vertices);
                            break;
                        }
                    }
                    foreach (var node in scc2.nodes)
                    {
                        foreach (var solid in DisassemblyDirections.Solids.Where(solid => solid.Name == node.name))
                        {
                            facesScc2.AddRange(solid.Faces);
                            break;
                        }
                        foreach (var solid in DisassemblyDirections.Solids.Where(solid => solid.Name == node.name))
                        {
                            verticesScc2.AddRange(solid.Vertices);
                            break;
                        }
                    }

                    var dir = DisassemblyDirections.Directions[cndDirInd];
                    foreach (var vertex in verticesScc1)
                    {
                        var ray =
                            new Ray(
                                new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1], vertex.Position[2]),
                                new Vector(dir[0], dir[1], dir[2]));
                        if (facesScc2.Any(f => RayIntersectsWithFace(ray, f)))
                            blocked = true;
                        if (blocked)
                            break;
                    }
                    if (!blocked)
                    {
                        foreach (var vertex in verticesScc2)
                        {
                            var ray =
                                new Ray(
                                    new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1], vertex.Position[2]),
                                    new Vector(-dir[0], -dir[1], -dir[2]));
                            if (facesScc1.Any(f => RayIntersectsWithFace(ray, f)))
                                blocked = true;
                            if (blocked)
                                break;
                        }
                    }
                    if (blocked)
                        dbgDictionary[scc1].Add(scc2);
                    blocked = false;
                }
            }
            return dbgDictionary;
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

        internal static void FiniteDirectionsBetweenUnconnectedParts(node node, List<TessellatedSolid> solids, List<int> freeDirs, designGraph assemblyGraph)
        {
            // Foreach direction find the sequence of the blocking parts. like this: it is first blocked by A, then B, then C ...
            var solid = solids.Where(s => s.Name == node.name).ToList()[0];
            //var nodeArcs = node.arcs.Where(a => a is arc) as List<arc>;
            foreach (var directionInd in freeDirs)
            {
                var direction = DisassemblyDirections.Directions[directionInd];
                var blockingPartsAndDistances = new Dictionary<TessellatedSolid, double>();
                var rays = new List<Ray>();
                foreach (var vertex in solid.ConvexHullVertices)
                    rays.Add(new Ray(new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1], vertex.Position[2]),
                                    new Vector(direction[0], direction[1], direction[2])));
                foreach (
                    var part in
                        solids.Where(
                            s =>
                                s != solid && // if it is not the same part
                                (!assemblyGraph.arcs.Any( // if there is no arc between the current and the candidate
                                    a =>
                                        (a.From.name == solid.Name && a.To.name == s.Name) ||
                                        (a.From.name == s.Name && a.To.name == solid.Name)))))
                {
                    if (!BoundingBoxBlocking(direction, part, solid)) continue;
                    var distanceToTheClosestFace = double.PositiveInfinity;
                    var overlap = false;
                    foreach (var ray in rays)
                    {
                        if (part.ConvexHullFaces.Any(f => RayIntersectsWithFace(ray, f)))
                            //now find the faces that intersect with the ray and find the distance between them
                        {
                            overlap = true;
                            foreach (
                                var blockingPolygonalFace in
                                    part.Faces.Where(f => RayIntersectsWithFace(ray, f)).ToList())
                            {
                                var d = DistanceToTheFace(ray.Position, blockingPolygonalFace);
                                if (d < distanceToTheClosestFace) distanceToTheClosestFace = d;
                            }
                        }
                    }
                    if (overlap) blockingPartsAndDistances.Add(part, distanceToTheClosestFace);
                }
                // by this point, I now know that for this direction, what parts are blocking the solid and in what order.
                if (blockingPartsAndDistances.Count == 0) continue;
                if (DisassemblyDirections.NonAdjacentBlocking.ContainsKey(directionInd))
                    DisassemblyDirections.NonAdjacentBlocking[directionInd].Add(new[]
                    {
                        node,
                        assemblyGraph.nodes.Where(n => n.name == blockingPartsAndDistances.Keys.ToList()[0].Name)
                            .ToList()[0]
                    });
                else
                    DisassemblyDirections.NonAdjacentBlocking.Add(directionInd,
                        new List<node[]>
                        {
                            new[]
                            {
                                node,
                                assemblyGraph.nodes.Where(n => n.name == blockingPartsAndDistances.Keys.ToList()[0].Name)
                                    .ToList()[0]
                            }
                        });
            }
        }

        private static double DistanceToTheFace(double[] p, PolygonalFace blockingPolygonalFace)
        {
            return
                Math.Abs(blockingPolygonalFace.Normal.dotProduct(p.subtract(blockingPolygonalFace.Vertices[0].Position)));
        }

        private static bool BoundingBoxBlocking(double[] v, TessellatedSolid partBlo, TessellatedSolid partMov)
        {
            var blockingBoundingBox = new[] { partBlo.XMin, partBlo.XMax, partBlo.YMin, partBlo.YMax, partBlo.ZMin, partBlo.ZMax };
            var movingBoundingBox = new[] { partMov.XMin, partMov.XMax, partMov.YMin, partMov.YMax, partMov.ZMin, partMov.ZMax };
            return BoundingBoxBlocking(v, blockingBoundingBox, movingBoundingBox);
        }

        private static bool BoundingBoxBlocking(double[] v, double[] blockingBox, double[] movingPartBox)
        {
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
            if (Math.Abs(ray.Direction.dotProduct(face.Normal, 3)) < Constants.NearlyParallelFace) return false;
            var inPlaneVerts = new AssemblyEvaluation.Vertex[3];
            var negativeDirCounter = 3;
            for (var i = 0; i < 3; i++)
            {
                var vectFromRayToFacePt = new Vector(ray.Position);
                vectFromRayToFacePt = vectFromRayToFacePt.MakeVectorTo(face.Vertices[i]);
                var dxtoPlane = ray.Direction.dotProduct(vectFromRayToFacePt.Position, 3);
                if (dxtoPlane < 0) negativeDirCounter--;
                if (negativeDirCounter == 0) return false;
                inPlaneVerts[i] = new AssemblyEvaluation.Vertex(face.Vertices[i].Position.add(StarMath.multiply(-dxtoPlane, ray.Direction, 3), 3));
            }
            if (inPlaneVerts.Min(v => v.Position[0]) > ray.Position[0] ) return false;
            if (inPlaneVerts.Max(v => v.Position[0]) < ray.Position[0] ) return false;
            if (inPlaneVerts.Min(v => v.Position[1]) > ray.Position[1] ) return false;
            if (inPlaneVerts.Max(v => v.Position[1]) < ray.Position[1] ) return false;
            if (inPlaneVerts.Min(v => v.Position[2]) > ray.Position[2] ) return false;
            if (inPlaneVerts.Max(v => v.Position[2]) < ray.Position[2] ) return false;
            if (inPlaneVerts.GetLength(0) > 3) return true;
            var crossProductsToCorners = new List<double[]>();
            for (int i = 0; i < 3; i++)
            {
                var j = (i == 2) ? 0 : i + 1;
                var crossProductsFrom_i_To_j =
                    inPlaneVerts[i].Position.subtract(ray.Position, 3)
                        .normalizeInPlace(3)
                        .crossProduct(inPlaneVerts[j].Position.subtract(ray.Position).normalizeInPlace(3));
                if (crossProductsFrom_i_To_j.norm2(true) < Constants.NearlyOnLine) return false;
                crossProductsToCorners.Add(crossProductsFrom_i_To_j);
            }
            for (int i = 0; i < 3; i++)
            {
                var j = (i == 2) ? 0 : i + 1;
                if (crossProductsToCorners[i].dotProduct(crossProductsToCorners[j], 3) <= 0.15) return false;
            }
            return true;
        }
    }
}
