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
                var solid1Primitives = solidPrimitive[solid1];
                for (var j = i+1; j < solids.Count; j++)
                {
                    var solid2 = solids[j];
                    var solid2Primitives = solidPrimitive[solid2];
                    List<int> localDirInd;
                    if (BlockingDetermination.DefineBlocking(solid1, solid2, solid1Primitives, solid2Primitives,
                        globalDirPool, Directions, out localDirInd))
                    {
                        // I wrote the code in a way that "solid1" is always "Reference" and "solid2" is always "Moving".
                        var from = assemblyGraph[solid2.Name]; // Moving
                        var to = assemblyGraph[solid1.Name];   // Reference
                        assemblyGraph.addArc((node) from, (node) to);
                        var a = assemblyGraph.arcs.Last();
                        AddInformationToArc(a, localDirInd);
                    }
                }
            }
            DisassemblyProcess.Run(assemblyGraph, Directions, globalDirPool);
        }

        private static void AddInformationToArc(arc a, IEnumerable<int> localDirInd)
        {
            a.localVariables.Add(GraphConstants.DirIndLowerBound);
            foreach (var dir in localDirInd)
            {
                a.localVariables.Add(dir);
            }
            a.localVariables.Add(GraphConstants.DirIndUpperBound);
        }

        private static void AddingNodesToGraph(designGraph assemblyGraph, IEnumerable<TessellatedSolid> solids)
        {
            foreach (var solid in solids)
            {
                assemblyGraph.addNode(solid.Name);
            }
        }

    }
}
