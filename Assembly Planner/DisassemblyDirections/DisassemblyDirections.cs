using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using StarMathLib;
using TVGL.Tessellation;

namespace Assembly_Planner
{
    internal class DisassemblyDirections
    {
        public static List<double[]> Directions = new List<double[]>(); 
        internal static void Run(designGraph assemblyGraph)
        {
            Directions = Icosahedron.DirectionGeneration();
            var globalDirPool = new List<int>();
            var solids = new List<TessellatedSolid>();
            var solidPrimitive = BlockingDetermination.PrimitiveMaker(solids);
            AddingNodesToGraph(assemblyGraph, solids);
            for (var i = 0; i < solids.Count; i++)
            {
                var solid1 = solids[i];
                var part1Primitives = solidPrimitive[solid1];
                for (var j = i+1; i < solids.Count; i++)
                {
                    var solid2 = solids[j];
                    var part2Primitives = solidPrimitive[solid2];
                    var localDirInd = new List<int>();
                    if (BlockingDetermination.DefineBlocking(solid1, solid2, part1Primitives, part2Primitives, globalDirPool, Directions, out localDirInd))
                    {
                        // I still dont know which one is moving, which one is ref
                        var from = assemblyGraph[solid1.Name];
                        var to = assemblyGraph[solid2.Name];
                        assemblyGraph.addArc((node)from, (node)to);
                        var a = assemblyGraph.arcs.Last();
                        AddInformationToTheArc(a, localDirInd);
                    }
                }
            }
        }

        private static void AddInformationToTheArc(arc a, List<int> localDirInd)
        {
            a.localVariables.Add(GraphConstants.DirectionInd);
            foreach (var dir in localDirInd)
            {
                a.localVariables.Add(dir);
            }
            a.localVariables.Add(GraphConstants.DirectionInd);
        }

        private static void AddingNodesToGraph(designGraph assemblyGraph, List<TessellatedSolid> solids)
        {
            foreach (var solid in solids)
            {
                assemblyGraph.addNode(solid.Name);
            }
        }

    }
}
