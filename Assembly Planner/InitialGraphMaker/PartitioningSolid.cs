using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Assembly_Planner.GeometryReasoning;
using StarMathLib;
using TVGL;

namespace Assembly_Planner
{
    internal static class PartitioningSolid
    {
        // This class is originally made for nonadjacent blocking determination.
        // The purpose of this class is to divide the solid space (using OBB) 
        // into "K" number of partitionas, then for ray casting, we don't need 
        // to check every triangle. We can only the triangles that are inside 
        // of the affected partitions. 
        public static int ccc = 0;
        internal static Partition[] Run(TessellatedSolid solid)
        {
            double[][] direction;
            var obb = OBB.BuildUsingPoints(solid.Vertices.ToList(), out direction);
            var partitions = new List<Partition>();
            //var obb = MinimumEnclosure.OrientedBoundingBox(solid);
            foreach (var cVer in obb)//obb.CornerVertices)
            {
                var cVerv = new Vertex(cVer);//cVer;
                var prtn = new Partition();
                var cornerVer = new List<Vertex> { cVerv };
                foreach (var cVerOther in obb.Where(v => v != cVerv.Position))//obb.CornerVertices.Where(v => v != cVerv))
                {
                    var midVer =
                        new Vertex(new[]
                        {
                            (cVerOther[0] + cVerv.Position[0])/2.0, 
                            (cVerOther[1] + cVerv.Position[1])/2.0,
                            (cVerOther[2] + cVerv.Position[2])/2.0
                        });
                    cornerVer.Add(midVer);
                }
                prtn.CornerVertices = cornerVer.ToArray();

                var visVer = new List<Vertex>();
                var faces = new List<PolygonalFace>();
                foreach (var ver1 in cornerVer.Where(v => !visVer.Contains(v)))
                {
                    var otherVers = new List<Vertex>();
                    visVer.Add(ver1);
                    foreach (var dir in direction)//obb.Directions)
                    {
                        foreach (var ver2 in cornerVer.Where(v => v != ver1))
                        {
                            // if they are on one line (considering the direction)
                            if (
                                Math.Abs((ver2.Position[0] - ver1.Position[0]) / dir[0] -
                                         (ver2.Position[1] - ver1.Position[1]) / dir[1]) < 10 &&
                                Math.Abs((ver2.Position[1] - ver1.Position[1]) / dir[1] -
                                         (ver2.Position[2] - ver1.Position[2]) / dir[2]) < 10)
                            {
                                visVer.Add(ver2);
                                otherVers.Add(ver2);
                                //var ed = new Edge(ver1, ver2, null, null);
                                break;
                            }
                        }
                    }
                    if (otherVers.Count != 3)
                    {
                        ccc++;
                    }//throw new Exception("There is an issue with Oriented Bounding Box");
                    for (var i = 0; i < otherVers.Count - 1; i++)
                    {
                        for (var j = i + 1; j < otherVers.Count; j++)
                        {
                            // these three points can make 2 different normals. One of them must is correct
                            var normal1 =
                                (otherVers[i].Position.subtract(ver1.Position)).crossProduct(
                                    otherVers[j].Position.subtract(ver1.Position));
                            var normal2 =
                                (otherVers[j].Position.subtract(ver1.Position)).crossProduct(
                                    otherVers[i].Position.subtract(ver1.Position));
                            PolygonalFace polFace;
                            if (cornerVer.Any(v => normal1.dotProduct(v.Position.subtract(ver1.Position)) > 0))
                                polFace = new PolygonalFace(new[] { ver1, otherVers[i], otherVers[j] }, normal2);
                            else
                                polFace = new PolygonalFace(new[] { ver1, otherVers[i], otherVers[j] }, normal1);
                            faces.Add(polFace);
                        }
                    }
                }
                prtn.Faces = faces.ToArray();
                prtn.SolidVertices = solid.Vertices.Where(vertex => IsVertexInsidePartition(prtn, vertex)).ToArray();
                partitions.Add(prtn);
            }
            return partitions.ToArray();
        }
        internal static Partition[] Run2(TessellatedSolid solid)
        {
            //var obb = OBB.BuildUsingPoints(solid.Vertices.ToList(), out direction);
            var obb = MinimumEnclosure.OrientedBoundingBox(solid);
            var partitions = new List<Partition>();
            var ddds = solid.Vertices.Max(v => v.Position[1]);
            for (var k = 0; k < obb.CornerVertices.Length; k++) //obb.CornerVertices)
            {
                var verK = new Vertex(obb.CornerVertices[k].Position); //cVer;
                var prtn = new Partition();
                var cornerVer = new List<Vertex>();
                for (var j = 0; j < obb.CornerVertices.Length; j++)
                {
                    var verJ = new Vertex(obb.CornerVertices[j].Position); //cVer;
                    var midVer = new Vertex((verJ.Position.add(verK.Position)).divide(2.0));
                    cornerVer.Add(midVer);
                }
                prtn.CornerVertices = cornerVer.ToArray();
                var faces = new List<PolygonalFace>
                {
                    new PolygonalFace(new[] {cornerVer[0], cornerVer[1], cornerVer[2]},
                        ((cornerVer[2].Position.subtract(cornerVer[0].Position)).crossProduct(
                            cornerVer[1].Position.subtract(cornerVer[0].Position))).normalize()),

                    new PolygonalFace(new[] {cornerVer[1], cornerVer[2], cornerVer[3]},
                        ((cornerVer[1].Position.subtract(cornerVer[3].Position)).crossProduct(
                            cornerVer[2].Position.subtract(cornerVer[3].Position))).normalize()),

                    new PolygonalFace(new[] {cornerVer[1], cornerVer[0], cornerVer[4]},
                        ((cornerVer[1].Position.subtract(cornerVer[0].Position)).crossProduct(
                            cornerVer[4].Position.subtract(cornerVer[0].Position))).normalize()),

                    new PolygonalFace(new[] {cornerVer[1], cornerVer[5], cornerVer[4]},
                        ((cornerVer[4].Position.subtract(cornerVer[5].Position)).crossProduct(
                            cornerVer[1].Position.subtract(cornerVer[5].Position))).normalize()),

                    new PolygonalFace(new[] {cornerVer[2], cornerVer[3], cornerVer[7]},
                        ((cornerVer[2].Position.subtract(cornerVer[3].Position)).crossProduct(
                            cornerVer[7].Position.subtract(cornerVer[3].Position))).normalize()),

                    new PolygonalFace(new[] {cornerVer[2], cornerVer[6], cornerVer[7]},
                        ((cornerVer[7].Position.subtract(cornerVer[6].Position)).crossProduct(
                            cornerVer[2].Position.subtract(cornerVer[6].Position))).normalize()),

                    new PolygonalFace(new[] {cornerVer[5], cornerVer[6], cornerVer[7]},
                        ((cornerVer[6].Position.subtract(cornerVer[7].Position)).crossProduct(
                            cornerVer[5].Position.subtract(cornerVer[7].Position))).normalize()),

                    new PolygonalFace(new[] {cornerVer[4], cornerVer[5], cornerVer[6]},
                        ((cornerVer[5].Position.subtract(cornerVer[4].Position)).crossProduct(
                            cornerVer[6].Position.subtract(cornerVer[4].Position))).normalize()),

                    new PolygonalFace(new[] {cornerVer[2], cornerVer[4], cornerVer[6]},
                        ((cornerVer[2].Position.subtract(cornerVer[6].Position)).crossProduct(
                            cornerVer[4].Position.subtract(cornerVer[6].Position))).normalize()),

                    new PolygonalFace(new[] {cornerVer[0], cornerVer[2], cornerVer[4]},
                        ((cornerVer[4].Position.subtract(cornerVer[0].Position)).crossProduct(
                            cornerVer[2].Position.subtract(cornerVer[0].Position))).normalize()),

                    new PolygonalFace(new[] {cornerVer[1], cornerVer[5], cornerVer[7]},
                        ((cornerVer[1].Position.subtract(cornerVer[5].Position)).crossProduct(
                            cornerVer[7].Position.subtract(cornerVer[5].Position))).normalize()),
                    new PolygonalFace(new[] {cornerVer[1], cornerVer[3], cornerVer[7]},
                        ((cornerVer[7].Position.subtract(cornerVer[3].Position)).crossProduct(
                            cornerVer[1].Position.subtract(cornerVer[3].Position))).normalize())
                };

                prtn.Faces = faces.ToArray();
                prtn.SolidVertices = solid.Vertices.Where(vertex => IsVertexInsidePartition(prtn, vertex)).ToArray();
                var tris = new List<PolygonalFace>();
                foreach (var ver in prtn.SolidVertices)
                {
                    tris.AddRange(ver.Faces.Where(f => !tris.Contains(f)));
                }
                prtn.SolidTriangles = tris.ToArray();
                partitions.Add(prtn);
            }
            //var s = partitions.Sum(a => a.SolidVertices.Count())-solid.Vertices.Count();
            return partitions.ToArray();
        }
        internal static bool IsVertexInsidePartition(Partition partition, Vertex ver)
        {
            return partition.Faces.All(pFace => !(pFace.Normal.dotProduct(pFace.Vertices[0].Position.subtract(ver.Position)) < -0.00000001));
        }
    }

    internal class Partition
    {
        public Vertex[] CornerVertices;
        public Edge[] Edges;
        public PolygonalFace[] Faces;
        public Vertex[] SolidVertices;
        public PolygonalFace[] SolidTriangles;
    }
}
