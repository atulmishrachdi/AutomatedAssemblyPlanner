using System;
using System.Collections.Generic;
using System.Linq;
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
        internal static void Run(TessellatedSolid solid)
        {
            double[][] direction;
            var obb = OBB.BuildUsingPoints(solid.Vertices.ToList(), out direction);
            foreach (var cVer in obb)
            {
                var cVerv = new Vertex(cVer);
                var prtn = new Partition();
                var cornerVer = new List<Vertex> { cVerv };
                foreach (var cVerOther in obb.Where(v => v != cVerv.Position))
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
                    foreach (var dir in direction)
                    {
                        foreach (var ver2 in cornerVer.Where(v => v != ver1))
                        {
                            // if they are on one line (considering the direction)
                            if (
                                Math.Abs((ver2.Position[0] - ver1.Position[0]) / dir[0] -
                                         (ver2.Position[1] - ver1.Position[1]) / dir[1]) < 0.00001 &&
                                Math.Abs((ver2.Position[1] - ver1.Position[1]) / dir[1] -
                                         (ver2.Position[2] - ver1.Position[2]) / dir[2]) < 0.00001)
                            {
                                visVer.Add(ver2);
                                otherVers.Add(ver2);
                                //var ed = new Edge(ver1, ver2, null, null);
                                break;
                            }
                        }
                    }
                    //if (otherVers.Count != 3) throw new Exception("There is an issue with Oriented Bounding Box");
                    for (var i = 0; i < otherVers.Count - 1; i++)
                    {
                        for (var j = i + 1; j < otherVers.Count; j++)
                        {
                            // these three points can make 2 different normals. One of them must be chosen
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
            }
        }
    }

    internal class Partition
    {
        public Vertex[] CornerVertices;
        public Edge[] Edges;
        public PolygonalFace[] Faces;
        public List<PolygonalFace> Triangles;
    }
}
