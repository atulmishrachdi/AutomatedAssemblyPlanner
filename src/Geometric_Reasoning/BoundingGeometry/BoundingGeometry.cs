using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace Geometric_Reasoning
{
    public class BoundingGeometry
    {
        public static Dictionary<TessellatedSolid, BoundingBox> OrientedBoundingBoxDic =
            new Dictionary<TessellatedSolid, BoundingBox>();

        public static Dictionary<TessellatedSolid, BoundingCylinder> BoundingCylinderDic =
            new Dictionary<TessellatedSolid, BoundingCylinder>();

        internal static void CreateOBB1(List<TessellatedSolid> solids)
        {
            var s = new Stopwatch();
            s.Start();
            Console.WriteLine();
            Console.WriteLine("OBBs are being Created ...");
            Parallel.ForEach(solids, solid =>
            {
                var bb = MinimumEnclosure.OrientedBoundingBox(solid);
                // change the orser of the corner vertices to clock wise:
                bb.CornerVertices = new[]
                {
                    bb.CornerVertices[2], bb.CornerVertices[0], bb.CornerVertices[1], bb.CornerVertices[3],
                    bb.CornerVertices[6], bb.CornerVertices[4], bb.CornerVertices[5], bb.CornerVertices[7]
                };
                lock (OrientedBoundingBoxDic)
                    OrientedBoundingBoxDic.Add(solid, bb);
            }
                );
            s.Stop();
            Console.WriteLine("OBB Creation is done in:" + "     " + s.Elapsed);
        }

        internal static void CreateOBB2(Dictionary<string, List<TessellatedSolid>> solids)
        {
            // This function uses my own OBB code not the one in TVGL
            // It has more accurate results
            //var s = new Stopwatch();
            //s.Start();
            //Console.WriteLine();
            //Console.WriteLine("OBBs are being Created ...");
            var solidGeometries = solids.SelectMany(s => s.Value).ToList();
            Parallel.ForEach(solidGeometries, solid =>
            {
                var obb = OBB.BuildUsingPoints(solid.Vertices.ToList());
                lock (OrientedBoundingBoxDic)
                {
                    OrientedBoundingBoxDic.Add(solid, obb);
                }
            }
                );
            //s.Stop();
            //Console.WriteLine("OBB Creation is done in:" + "     " + s.Elapsed);
        }

        internal static void CreateBoundingCylinder(Dictionary<string, List<TessellatedSolid>> solids)
        {
            // This function uses my own OBB code not the one in TVGL
            // It has more accurate results
            //var s = new Stopwatch();
            //s.Start();
            //Console.WriteLine();
            //Console.WriteLine("Bounding Cylinders are being Created ...");
            var solidGeometries = solids.SelectMany(s => s.Value).ToList();
            Parallel.ForEach(solidGeometries, solid =>
            {
                var bc = BoundingCylinder.Run(solid);
                lock (BoundingCylinderDic)
                {
                    BoundingCylinderDic.Add(solid, bc);
                }
            }
                );
            //s.Stop();
            //Console.WriteLine("Bounding Cylinder Creation is done in:" + "     " + s.Elapsed);
        }
    }
}
