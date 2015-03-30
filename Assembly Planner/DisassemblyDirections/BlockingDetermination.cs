using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;
using TVGL;
using TVGL.Primitive_Surfaces.ClassifyTesselationAsPrimitives;
using TVGL.Tessellation;

namespace Assembly_Planner
{
    class BlockingDetermination
    {
        internal static Dictionary<TessellatedSolid, List<PrimitiveSurface>> PrimitiveMaker(List<TessellatedSolid> parts)
        {
            var partPrimitive = new Dictionary<TessellatedSolid, List<PrimitiveSurface>>();
            foreach (var solid in parts)
            {
                var solidPrim = TesselationToPrimitives.Run(solid);
                partPrimitive.Add(solid, solidPrim);
            }
            return partPrimitive;
        }

        internal static bool DefineBlocking(TessellatedSolid a, TessellatedSolid b, List<PrimitiveSurface> aP,
            List<PrimitiveSurface> bP, List<int> globalDirPool, List<double[]> directions, out List<int> dirInd)
        {
            if (BoundingBoxOverlap(a, b))
            {
                if (ConvexHullOverlap(a, b))
                {
                    var localDirInd = new List<int>();
                    for (var i = 0; i < directions.Count; i++)
                        localDirInd.Add(i);
                    if (PrimitivePrimitiveInteractions.PrimitiveOverlap(aP, bP, localDirInd))
                    {
                        // dirInd is the list of directions that must be added to the arc between part1 and part2
                        // I also need to creat the pool of directions
                        globalDirPool.AddRange(localDirInd.Where(d => !globalDirPool.Contains(d)));
                        dirInd = localDirInd;
                        return true;
                    }
                }
            }
            dirInd = null;
            return false;
        }

        private static bool ConvexHullOverlap(TessellatedSolid a, TessellatedSolid b)
        {
            foreach (var f in a.ConvexHullFaces)
            {
                var n = f.Normal;
                var dStar = n.dotProduct(f.Vertices[0].Position, 3);
                if (b.ConvexHullVertices.All(pt => (n.dotProduct(pt.Position, 3)) > dStar))
                {
                    return false;
                }
            }
            foreach (var f in b.ConvexHullFaces)
            {
                var n = f.Normal;
                var dStar = n.dotProduct(f.Vertices[0].Position, 3);
                if (a.ConvexHullVertices.All(pt => (n.dotProduct(pt.Position, 3)) > dStar))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool BoundingBoxOverlap(TessellatedSolid a, TessellatedSolid b)
        {
            return (!(a.XMin > b.XMax || a.YMin > b.YMax || a.ZMin > b.ZMax
                      || b.XMin > a.XMax|| b.YMin > a.YMax || b.ZMin > a.ZMax));
        }
    }
}
