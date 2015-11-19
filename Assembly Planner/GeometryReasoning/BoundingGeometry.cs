using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace Assembly_Planner
{
    class BoundingGeometry
    {
        internal static Dictionary<TessellatedSolid, BoundingBox> OrientedBoundingBoxDic =
            new Dictionary<TessellatedSolid, BoundingBox>();

        internal static Dictionary<TessellatedSolid, BoundingCylinder> BoundingCylinderDic =
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
                    OrientedBoundingBoxDic.Add(solid, MinimumEnclosure.OrientedBoundingBox(solid));
            }
                );
            s.Stop();
            Console.WriteLine("OBB Creation is done in:" + "     " + s.Elapsed);
        }

        internal static void CreateOBB2(List<TessellatedSolid> solids)
        {
            // This function uses my own OBB code not the one in TVGL
            // It has more accurate results
            var s = new Stopwatch();
            s.Start();
            Console.WriteLine();
            Console.WriteLine("OBBs are being Created ...");
            Parallel.ForEach(solids, solid =>
            {
                lock (OrientedBoundingBoxDic)
                    OrientedBoundingBoxDic.Add(solid, OBB.BuildUsingPoints(solid.Vertices.ToList()));
            }
                );
            s.Stop();
            Console.WriteLine("OBB Creation is done in:" + "     " + s.Elapsed);
        }

        internal static void CreateBoundingCylinder(List<TessellatedSolid> solids)
        {
            // This function uses my own OBB code not the one in TVGL
            // It has more accurate results
            var s = new Stopwatch();
            s.Start();
            Console.WriteLine();
            Console.WriteLine("Bounding Cylinders are being Created ...");
            Parallel.ForEach(solids, solid =>
            {
                lock (BoundingCylinderDic)
                    BoundingCylinderDic.Add(solid, BoundingCylinder.Run(solid));
            }
                );
            s.Stop();
            Console.WriteLine("Bounding Cylinder Creation is done in:" + "     " + s.Elapsed);
        }
    }
}
