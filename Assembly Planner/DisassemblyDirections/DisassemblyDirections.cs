using System;
using System.Collections.Generic;
using System.IO;
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
    internal class DisassemblyDirections
    {
        public static List<double[]> Directions = new List<double[]>(); 
        internal static void Run(designGraph assemblyGraph)
        {
            Directions = Icosahedron.DirectionGeneration();
            var dirPool = new List<int>();
            var solids = new List<TessellatedSolid>();
            var solidPrimitive = PrimitiveMaker(solids);
            foreach (var solid1 in solids)
            {
                var part1Primitives = solidPrimitive[solid1];
                if (!assemblyGraph.nodes.Exists(n => n.name == solid1.Name))
                    assemblyGraph.addNode(solid1.Name);
                foreach (var solid2 in solids)
                {
                    var part2Primitives = solidPrimitive[solid2];
                    if (!assemblyGraph.nodes.Exists(n => n.name == solid1.Name))
                        assemblyGraph.addNode(solid1.Name);
                    if (DefineBlocking(solid1, solid2, part1Primitives, part2Primitives, dirPool))
                        // I still dont know which one is moving, which one is ref
                    {
                        var from = assemblyGraph[solid1.Name];
                        var to = assemblyGraph[solid2.Name];
                        assemblyGraph.addArc((node)from, (node)to);
                    }
                }
            }
        }

        private static Dictionary<TessellatedSolid, List<PrimitiveSurface>> PrimitiveMaker(List<TessellatedSolid> parts)
        {
            var partPrimitive = new Dictionary<TessellatedSolid, List<PrimitiveSurface>>(); 
            foreach (var solid in parts)
            {
                var solidPrim = TesselationToPrimitives.Run(solid);
                partPrimitive.Add(solid,solidPrim);
            }
            return partPrimitive;
        }
        
        internal static bool DefineBlocking(TessellatedSolid a, TessellatedSolid b, List<PrimitiveSurface> aP,
            List<PrimitiveSurface> bP, List<int> dirPool)
        {
            if (BoundingBoxOverlappingCheck(a, b))
            {
                if (ConvexHullOverlappingCheck(a, b))
                {
                    var dirInd = new List<int>();
                    for (var i = 0; i < Directions.Count; i++)
                        dirInd.Add(i);
                    if (PrimitivePrimitiveInteractions.PrimitiveOverlapping(aP, bP, dirInd))
                    {
                        // dirInd is the list of directions that must be added to the arc between part1 and part2
                        // I also need to creat the pool of directions
                        dirPool.AddRange(dirInd.Where(d=> !dirPool.Contains(d)));
                        return true;
                    }
                }
            }
            return false;
        }

    }
}
