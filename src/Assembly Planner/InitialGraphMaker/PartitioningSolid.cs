using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Assembly_Planner;
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

        internal static Dictionary<TessellatedSolid, Partition[]> Partitions;
        internal static Dictionary<TessellatedSolid, PartitionAABB[]> PartitionsAABB;
        public static int ccc = 0;
        /*internal static Partition[] Run(TessellatedSolid solid)
        {
            double[][] direction;
            bool clockWise;
            var obb = OBB.BuildUsingPoints(solid.Vertices.ToList(), out direction, out clockWise);
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

                var visVer = new HashSet<Vertex>();
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
                prtn.Faces = faces;
                prtn.SolidVertices = new HashSet<Vertex>(solid.Vertices.Where(vertex => IsVertexInsidePartition(prtn, vertex)));
                partitions.Add(prtn);
            }
            return partitions.ToArray();
        }*/

        internal static Partition[] Run(HashSet<Vertex> solidVerts, HashSet<PolygonalFace> solidFaces, Vertex[] obbCorverVts)
        {
            // the corner vertices are all clock wise now.
            var partitions = new List<Partition>();
            //var ddds = solid.Vertices.Max(v => v.Position[1]);
            for (var k = 0; k < obbCorverVts.Length; k++) //obb.CornerVertices)
            {
                var verK = new Vertex(obbCorverVts[k].Position); //cVer;
                var prtn = new Partition();
                var cornerVer = new List<Vertex>();
                for (var j = 0; j < obbCorverVts.Length; j++)
                {
                    var verJ = new Vertex(obbCorverVts[j].Position); //cVer;
                    var midVer = new Vertex((verJ.Position.add(verK.Position)).divide(2.0));
                    cornerVer.Add(midVer);
                }
                prtn.CornerVertices = cornerVer.ToArray();
                prtn.Faces = TwelveFaceGenerator(prtn.CornerVertices);
                prtn.SolidVertices = new HashSet<Vertex>(solidVerts.Where(vertex => IsVertexInsidePartition(prtn, vertex)));
                prtn.SolidTriangles = PartitionTrianglesPro(prtn, solidFaces);
                // continue the octree or not?
                if (IsItWorthGoingDownTheOctree(prtn.SolidTriangles))
                {
                    prtn.InnerPartition = Run(prtn.SolidVertices, prtn.SolidTriangles, prtn.CornerVertices);
                }
                partitions.Add(prtn);
            }
            return partitions.ToArray();
        }

        internal static PartitionAABB[] RunForAABB(HashSet<Vertex> solidVerts, HashSet<PolygonalFace> solidFaces, Vertex[] corverVts)
        {
            // the corner vertices are all clock wise now.
            var partitions = new List<PartitionAABB>();
            //var ddds = solid.Vertices.Max(v => v.Position[1]);
            for (var k = 0; k < corverVts.Length; k++) //obb.CornerVertices)
            {
                var verK = new Vertex(corverVts[k].Position); //cVer;
                var prtn = new PartitionAABB();
                var cornerVer = new List<Vertex>();
                double Xmin = double.PositiveInfinity, Ymin = double.PositiveInfinity, Zmin = double.PositiveInfinity;
                double Xmax = double.NegativeInfinity, Ymax = double.NegativeInfinity, Zmax = double.NegativeInfinity;
                for (var j = 0; j < corverVts.Length; j++)
                {
                    var verJ = new Vertex(corverVts[j].Position); //cVer;
                    var midVer = new Vertex((verJ.Position.add(verK.Position)).divide(2.0));
                    cornerVer.Add(midVer);
                    if (midVer.Position[0] > Xmax) Xmax = midVer.Position[0];
                    if (midVer.Position[0] < Xmin) Xmin = midVer.Position[0];
                    if (midVer.Position[1] > Ymax) Ymax = midVer.Position[1];
                    if (midVer.Position[1] < Ymin) Ymin = midVer.Position[1];
                    if (midVer.Position[2] > Zmax) Zmax = midVer.Position[2];
                    if (midVer.Position[2] < Zmin) Zmin = midVer.Position[2];
                }
                prtn.X = new[] { Xmin, Xmax };
                prtn.Y = new[] { Ymin, Ymax };
                prtn.Z = new[] { Zmin, Zmax };
                prtn.CornerVertices = cornerVer.ToArray();
                prtn.SolidVertices = new HashSet<Vertex>(solidVerts.Where(vertex => IsVertexInsidePartition(prtn, vertex)));
                prtn.SolidTriangles = PartitionTrianglesPro(prtn, solidFaces);
                // continue the octree or not?
                if (prtn.SolidTriangles.Count > 200)
                {
                    prtn.InnerPartition = RunForAABB(prtn.SolidVertices, prtn.SolidTriangles, prtn.CornerVertices);
                }
                partitions.Add(prtn);
            }
            return partitions.ToArray();
        }

        private static bool IsItWorthGoingDownTheOctree(HashSet<PolygonalFace> faces)
        {
            if (faces.Count > 2000) return false;
            return ((faces.Count / 2.0 - 96) * (7e-5)) - ((2.35e-5) * faces.Count + 0.0012) > 0;
        }

        internal static List<PolygonalFace> TwelveFaceGenerator(Vertex[] cornerVer)
        {
            return new List<PolygonalFace>
            {
                new PolygonalFace(new[] {cornerVer[0], cornerVer[1], cornerVer[3]},
                    ((cornerVer[3].Position.subtract(cornerVer[0].Position)).crossProduct(
                        cornerVer[1].Position.subtract(cornerVer[0].Position))).normalize()),

                new PolygonalFace(new[] {cornerVer[1], cornerVer[2], cornerVer[3]},
                    ((cornerVer[1].Position.subtract(cornerVer[2].Position)).crossProduct(
                        cornerVer[3].Position.subtract(cornerVer[2].Position))).normalize()),

                new PolygonalFace(new[] {cornerVer[1], cornerVer[0], cornerVer[4]},
                    ((cornerVer[1].Position.subtract(cornerVer[0].Position)).crossProduct(
                        cornerVer[4].Position.subtract(cornerVer[0].Position))).normalize()),

                new PolygonalFace(new[] {cornerVer[1], cornerVer[5], cornerVer[4]},
                    ((cornerVer[4].Position.subtract(cornerVer[5].Position)).crossProduct(
                        cornerVer[1].Position.subtract(cornerVer[5].Position))).normalize()),

                new PolygonalFace(new[] {cornerVer[2], cornerVer[3], cornerVer[7]},
                    ((cornerVer[7].Position.subtract(cornerVer[3].Position)).crossProduct(
                        cornerVer[2].Position.subtract(cornerVer[3].Position))).normalize()),

                new PolygonalFace(new[] {cornerVer[2], cornerVer[6], cornerVer[7]},
                    ((cornerVer[2].Position.subtract(cornerVer[6].Position)).crossProduct(
                        cornerVer[7].Position.subtract(cornerVer[6].Position))).normalize()),

                new PolygonalFace(new[] {cornerVer[5], cornerVer[6], cornerVer[7]},
                    ((cornerVer[7].Position.subtract(cornerVer[6].Position)).crossProduct(
                        cornerVer[5].Position.subtract(cornerVer[6].Position))).normalize()),

                new PolygonalFace(new[] {cornerVer[4], cornerVer[5], cornerVer[7]},
                    ((cornerVer[5].Position.subtract(cornerVer[4].Position)).crossProduct(
                        cornerVer[7].Position.subtract(cornerVer[4].Position))).normalize()),

                new PolygonalFace(new[] {cornerVer[1], cornerVer[2], cornerVer[6]},
                    ((cornerVer[6].Position.subtract(cornerVer[2].Position)).crossProduct(
                        cornerVer[1].Position.subtract(cornerVer[2].Position))).normalize()),

                new PolygonalFace(new[] {cornerVer[1], cornerVer[5], cornerVer[6]},
                    ((cornerVer[1].Position.subtract(cornerVer[5].Position)).crossProduct(
                        cornerVer[6].Position.subtract(cornerVer[5].Position))).normalize()),

                new PolygonalFace(new[] {cornerVer[0], cornerVer[3], cornerVer[7]},
                    ((cornerVer[0].Position.subtract(cornerVer[3].Position)).crossProduct(
                        cornerVer[7].Position.subtract(cornerVer[3].Position))).normalize()),

                new PolygonalFace(new[] {cornerVer[0], cornerVer[4], cornerVer[7]},
                    ((cornerVer[7].Position.subtract(cornerVer[4].Position)).crossProduct(
                        cornerVer[0].Position.subtract(cornerVer[4].Position))).normalize())
            };
        }

        internal static List<PolygonalFace> TwelveFaceGenerator2(Vertex[] cornerVer)
        {
            // this is the old function to create twelve triangles of the OBB generated
            // using TVGL approach. I am trying not to use this function since I have changed
            // the order of the corner vertices of the OBBs to clock wise, so it will be similar to
            // my own OBB function.

            return new List<PolygonalFace>
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
        }
        internal static bool IsVertexInsidePartition(Partition partition, Vertex ver)
        {
            return partition.Faces.All(pFace => pFace.Normal.dotProduct(ver.Position.subtract(pFace.Vertices[0].Position)) < 0);
            //partition.Faces.All(
            //   pFace => !(pFace.Normal.dotProduct(pFace.Vertices[0].Position.subtract(ver.Position)) <0));
            //-0.00000001));
        }
        private static bool IsVertexInsidePartition(PartitionAABB prtn, Vertex vertex)
        {
            var verPos = vertex.Position;
            if (verPos[0] > prtn.X[1] || verPos[0] < prtn.X[0] ||
                verPos[1] > prtn.Y[1] || verPos[1] < prtn.Y[0] ||
                verPos[2] > prtn.Z[1] || verPos[2] < prtn.Z[0]) return false;
            return true;
        }

        private static HashSet<PolygonalFace> PartitionTrianglesPro(Partition partition, HashSet<PolygonalFace> solidTrgs)
        {
            var trigs = new HashSet<PolygonalFace>();
            foreach (var ver in partition.SolidVertices)
                trigs.UnionWith(ver.Faces.Where(f => !trigs.Contains(f)));
            // using seperating axis theorem (SAT). Among 6 faces of the box and the triangle candidate from the solid
            //    if any of them can seperate the 
            foreach (var pF in solidTrgs.Where(t => !trigs.Contains(t)))
            {
                var overlap = true;
                var dots =
                    partition.CornerVertices.Select(
                        corVer => (corVer.Position.subtract(pF.Vertices[0].Position)).dotProduct(pF.Normal)).ToList();
                if (dots.All(d => d >= 0) || dots.All(d => d <= 0))
                    continue;
                for (var i = 0; i < 12; i += 2)
                {
                    if (
                        pF.Vertices.All(
                            v =>
                                partition.Faces[i].Normal.dotProduct(
                                    v.Position.subtract(partition.Faces[i].Vertices[0].Position)) >= 0))
                    {
                        overlap = false;
                        break;
                    }
                }
                if (overlap) trigs.Add(pF);
            }
            return trigs;
        }

        private static HashSet<PolygonalFace> PartitionTrianglesPro(PartitionAABB partition, HashSet<PolygonalFace> solidTrgs)
        {
            var trigs = new HashSet<PolygonalFace>();
            foreach (var ver in partition.SolidVertices)
                trigs.UnionWith(ver.Faces.Where(f => !trigs.Contains(f)));
            // using seperating axis theorem (SAT). Among 6 faces of the box and the triangle candidate from the solid
            //    if any of them can seperate the 

            // normal of the partition planes and and a vertex on the plane 
            var normalsAndVertex = new Dictionary<double[], double[]>
            {
                {new[] {1.0, 0, 0}, new[] {partition.X[1], partition.Y[1], partition.Z[1]}},
                {new[] {-1.0, 0, 0}, new[] {partition.X[0], partition.Y[1], partition.Z[1]}},
                {new[] {0, 1.0, 0}, new[] {partition.X[1], partition.Y[1], partition.Z[1]}},
                {new[] {0, -1.0, 0}, new[] {partition.X[1], partition.Y[0], partition.Z[1]}},
                {new[] {0, 0, 1.0}, new[] {partition.X[1], partition.Y[1], partition.Z[1]}},
                {new[] {0, 0, -1.0}, new[] {partition.X[1], partition.Y[1], partition.Z[0]}}
            };
            foreach (var pF in solidTrgs.Where(t => !trigs.Contains(t)))
            {
                var dots =
                    partition.CornerVertices.Select(
                        corVer => (corVer.Position.subtract(pF.Vertices[0].Position)).dotProduct(pF.Normal)).ToList();
                if (dots.All(d => d >= 0) || dots.All(d => d <= 0))
                    continue;
                var overlap = normalsAndVertex.All(dic => !pF.Vertices.All(v => dic.Key.dotProduct(v.Position.subtract(dic.Value)) >= 0));
                if (overlap) trigs.Add(pF);
            }
            return trigs;
        }

        internal static HashSet<PolygonalFace> PartitionTriangles(Partition partition, HashSet<PolygonalFace> solidTrgs)
        {
            var trigs = new HashSet<PolygonalFace>();
            foreach (var ver in partition.SolidVertices)
                trigs.UnionWith(ver.Faces.Where(f => !trigs.Contains(f)));
            foreach (var pF in solidTrgs.Where(t => !trigs.Contains(t)))
            {
                var sign = 0;
                foreach (var corVer in partition.CornerVertices)
                {
                    sign = Math.Sign((corVer.Position.subtract(pF.Vertices[0].Position)).dotProduct(pF.Normal));
                    if (sign == 0) continue;
                    break;
                }
                if (sign == 1)
                {
                    if (partition.CornerVertices.Any(
                            c => Math.Sign((c.Position.subtract(pF.Vertices[0].Position)).dotProduct(pF.Normal)) == -1))
                        trigs.Add(pF);
                }
                else
                {
                    if (partition.CornerVertices.Any(
                        c => Math.Sign((c.Position.subtract(pF.Vertices[0].Position)).dotProduct(pF.Normal)) == 1))
                        trigs.Add(pF);
                }
            }
            return trigs;
        }

        internal static void CreatePartitions(Dictionary<string, List<TessellatedSolid>> solids)
        {
            Console.WriteLine("\nUpdating Bounding Geometries ....");
            int width = 55;
            int refresh = (int)Math.Ceiling(((float)solids.Count) / ((float)(width * 4)));
            int check = 0;
            LoadingBar.start(width, 0);
            var solidGeometries = solids.SelectMany(s => s.Value).ToList();
            var solidGeometries2 = Program.Solids.SelectMany(s => s.Value).ToList();
            var totalVerts = solidGeometries.Sum(s => s.Vertices.Count());
            foreach(var solid in solidGeometries)
            //Parallel.ForEach(solidGeometries, solid =>
            {
                if (check % refresh == 0)
                {
                    LoadingBar.refresh(width, ((float)check) / ((float)solidGeometries.Count));
                }
                check++;
                //solid.SimplifyByPercentage(0.5);
                /*Partition[] prtn;

                for ()
                {

                }*/
                Console.WriteLine(solid.Name);
                Console.Out.Flush();
                BoundingBox bad = new BoundingBox();
                BoundingBox val = bad;
                Partition[] prtn = new Partition[0];
                foreach (KeyValuePair<TessellatedSolid,BoundingBox> b 
                            in BoundingGeometry.OrientedBoundingBoxDic)
                {
                    if(b.Key.Name == solid.Name)
                    {
                        val = b.Value;
                    }
                    else
                    {
                        Console.Write("\n - ");
                        Console.Write(solid.Name);
                        Console.Write("\n");
                    }
                }
                if(!val.Equals(bad))
                {
                    prtn = Run(new HashSet<Vertex>(solid.Vertices), new HashSet<PolygonalFace>(solid.Faces),
                    val.CornerVertices.Select( cv => new Vertex(cv.Position)).ToArray());
                }
                else
                {
                    Console.Write(solid.Name);
                }
                /*
                prtn = Run(new HashSet<Vertex>(solid.Vertices), new HashSet<PolygonalFace>(solid.Faces),
                    BoundingGeometry.OrientedBoundingBoxDic.First(b=>b.Key.Name == solid.Name).Value.CornerVertices.Select(
                        cv => new Vertex(cv.Position)).ToArray());*/
                //lock (Partitions)
                //{
                    Partitions.Add(solid, prtn);
                //}
            }//);

            // partition of AABB:
            Parallel.ForEach(solidGeometries2, solid =>
            {
                var cornerVer = new[]
                {
                    new Vertex(new []{solid.XMin,solid.YMin, solid.ZMin}),
                    new Vertex(new []{solid.XMin,solid.YMin, solid.ZMax}),
                    new Vertex(new []{solid.XMin,solid.YMax, solid.ZMin}),
                    new Vertex(new []{solid.XMin,solid.YMax, solid.ZMax}),
                    new Vertex(new []{solid.XMax,solid.YMin, solid.ZMin}),
                    new Vertex(new []{solid.XMax,solid.YMin, solid.ZMax}),
                    new Vertex(new []{solid.XMax,solid.YMax, solid.ZMin}),
                    new Vertex(new []{solid.XMax,solid.YMax, solid.ZMax}),
                };
                var prtn = RunForAABB(new HashSet<Vertex>(solid.Vertices), new HashSet<PolygonalFace>(solid.Faces), cornerVer);
                lock (PartitionsAABB)
                {
                    PartitionsAABB.Add(solid, prtn);
                }
            });//
            LoadingBar.refresh(width, 1);
        }
    }

    public class Partition
    {
        public Partition[] InnerPartition;
        public Vertex[] CornerVertices;
        public Edge[] Edges;
        public List<PolygonalFace> Faces;
        public HashSet<Vertex> SolidVertices;
        public HashSet<PolygonalFace> SolidTriangles;
    }
    public class PartitionAABB
    {
        public PartitionAABB[] InnerPartition;
        public Vertex[] CornerVertices;
        public double[] X; // [Xmin, Xmax]
        public double[] Y;
        public double[] Z;
        public HashSet<Vertex> SolidVertices;
        public HashSet<PolygonalFace> SolidTriangles;
    }
}
