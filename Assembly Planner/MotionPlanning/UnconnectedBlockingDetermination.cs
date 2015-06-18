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
                    inPlaneVerts[i].Position.subtract(ray.Position, 3)
                        .normalizeInPlace(3)
                        .crossProduct(inPlaneVerts[j].Position.subtract(ray.Position).normalizeInPlace(3));
                if (crossProductsFrom_i_To_j.norm2(true) < Constants.NearlyOnLine) return false;
                crossProductsToCorners.Add(crossProductsFrom_i_To_j);
            }
            for (int i = 0; i < 3; i++)
            {
                var j = (i == 2) ? 0 : i + 1;
                if (crossProductsToCorners[i].dotProduct(crossProductsToCorners[j], 3) <= 0) return false;
            }
            return true;
        }
    }
}
