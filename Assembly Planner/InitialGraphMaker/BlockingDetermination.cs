using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using StarMathLib;
using TVGL;
using TVGL.Primitive_Surfaces.ClassifyTesselationAsPrimitives;
using TVGL.Tessellation;

namespace Assembly_Planner
{
    internal class BlockingDetermination
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

        internal static bool DefineBlocking(designGraph assemblyGraph, TessellatedSolid solid1, TessellatedSolid solid2,
            List<PrimitiveSurface> solid1P, List<PrimitiveSurface> solid2P, List<int> globalDirPool,
            out List<int> dirInd)
        {
            if (BoundingBoxOverlap(solid1, solid2))
            {
                if (ConvexHullOverlap(solid1, solid2))
                {
                    if (GearGear(assemblyGraph, solid1, solid2))
                    {
                        // one more condition to check. If Gear-Gear, check and see if the normals of the solids are parallel
                        if (!ParallelNormals(assemblyGraph, solid1, solid2))
                        {
                            dirInd = null;
                            return false;
                        }
                        dirInd = GearNormal(assemblyGraph, solid1);
                        return true;
                    }
                    var localDirInd = new List<int>();
                    for (var i = 0; i < DisassemblyDirections.Directions.Count; i++)
                        localDirInd.Add(i);
                    if (PrimitivePrimitiveInteractions.PrimitiveOverlap(solid1P, solid2P, localDirInd))
                    {
                        // dirInd is the list of directions that must be added to the arc between part1 and part2
                        Console.WriteLine(@"An overlap is detected between   " + solid1.Name + "   and   " + solid2.Name);
                        globalDirPool.AddRange(localDirInd.Where(d => !globalDirPool.Contains(d)));
                        foreach (var i in localDirInd)
                        {
                            Console.WriteLine(DisassemblyDirections.Directions[i][0] + " " +
                                              DisassemblyDirections.Directions[i][1] + " " +
                                              DisassemblyDirections.Directions[i][2]);
                        }
                        dirInd = localDirInd;
                        return true;
                    }
                }
            }
            dirInd = null;
            return false;
        }

        private static bool ParallelNormals(designGraph assemblyGraph, TessellatedSolid solid1, TessellatedSolid solid2)
        {
            var dir1 = VariableOfTheIndex(DisConstants.GearNormal, assemblyGraph[solid1.Name].localVariables);
            var dir2 = VariableOfTheIndex(DisConstants.GearNormal, assemblyGraph[solid1.Name].localVariables);
            return 1 - Math.Abs(dir1.dotProduct(dir2)) < ConstantsPrimitiveOverlap.EqualToZero;
        }

        private static double[] VariableOfTheIndex(double p, List<double> vars)
        {
            var ind = vars.IndexOf(p);
            return new[] {vars[ind + 1], vars[ind + 2], vars[ind + 3]};

        }

        private static List<int> GearNormal(designGraph assemblyGraph, TessellatedSolid solid1)
        {
            var dir = VariableOfTheIndex(DisConstants.GearNormal, assemblyGraph[solid1.Name].localVariables);
            return NormalIndexInGlobalDirns(dir);
        }

        private static bool GearGear(designGraph assemblyGraph, TessellatedSolid solid1, TessellatedSolid solid2)
        {
            return assemblyGraph[solid1.Name].localLabels.Contains(DisConstants.Gear) &&
                   assemblyGraph[solid2.Name].localLabels.Contains(DisConstants.Gear);
        }

        internal static bool ConvexHullOverlap(TessellatedSolid a, TessellatedSolid b)
        {
            foreach (var f in a.ConvexHullFaces)
            {
                var n = f.Normal;
                var dStar = n.dotProduct(f.Vertices[0].Position);
                if (b.ConvexHullVertices.All(pt => (n.dotProduct(pt.Position)) > dStar))
                {
                    return false;
                }
            }
            foreach (var f in b.ConvexHullFaces)
            {
                var n = f.Normal;
                var dStar = n.dotProduct(f.Vertices[0].Position);
                if (a.ConvexHullVertices.All(pt => (n.dotProduct(pt.Position)) > dStar))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool BoundingBoxOverlap(TessellatedSolid a, TessellatedSolid b)
        {
            return (!(a.XMin > b.XMax || a.YMin > b.YMax || a.ZMin > b.ZMax
                      || b.XMin > a.XMax|| b.YMin > a.YMax || b.ZMin > a.ZMax));
        }
        
        internal static List<int> NormalIndexInGlobalDirns(double[] p)
        {
            var dirs =
                DisassemblyDirections.Directions.Where(
                    globalDirn => 1 - Math.Abs(p.dotProduct(globalDirn)) < ConstantsPrimitiveOverlap.EqualToZero)
                    .ToList();
            return dirs.Select(dir => DisassemblyDirections.Directions.IndexOf(dir)).ToList();
        }
    }
}
