using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using StarMathLib;
using TVGL;
using TVGL.Tessellation;

namespace Assembly_Planner
{
    internal class DisassemblyDirections
    {
        public static List<double[]> Directions = new List<double[]>();
        internal static List<TessellatedSolid> Solids;
        internal static List<int> Run(designGraph assemblyGraph, List<TessellatedSolid> solids)
        {
            Solids = new List<TessellatedSolid>(solids);
            Directions = IcosahedronPro.DirectionGeneration();
            var globalDirPool = new List<int>();
            var solidPrimitive = BlockingDetermination.PrimitiveMaker(solids);
            
            //var screwsAndBolts = BoltAndGearDetection.ScrewAndBoltDetector(solidPrimitive);
            //var gears = BoltAndGearDetection.GearDetector(solidPrimitive);
            
            var solidsNoFastener = new List<TessellatedSolid>(solids);
            //solidsNoFastener.RemoveRange(screwsAndBolts);

            AddingNodesToGraph(assemblyGraph, solidsNoFastener);//, gears, screwsAndBolts);
            
            for (var i = 0; i < solidsNoFastener.Count - 1; i++)
            {
                var solid1 = solidsNoFastener[i];
                var solid1Primitives = solidPrimitive[solid1];
                for (var j = i + 1; j < solidsNoFastener.Count; j++)
                {
                    var solid2 = solidsNoFastener[j];
                    var solid2Primitives = solidPrimitive[solid2];
                    List<int> localDirInd;
                    if (BlockingDetermination.DefineBlocking(assemblyGraph, solid1, solid2, solid1Primitives, solid2Primitives,
                        globalDirPool, out localDirInd))
                    {
                        // I wrote the code in a way that "solid1" is always "Reference" and "solid2" is always "Moving".
                        List<int> finDirs, infDirs;
                        UnconnectedBlockingDetermination.FiniteDirectionsBetweenConnectedParts(solid1, solid2, localDirInd, out finDirs, out infDirs);
                        var from = assemblyGraph[solid2.Name]; // Moving
                        var to = assemblyGraph[solid1.Name];   // Reference
                        assemblyGraph.addArc((node) from, (node) to);
                        var a = assemblyGraph.arcs.Last();
                        AddInformationToArc(a, localDirInd);
                        //if (localDirInd.Count == 2)
                        //{
                        //    var m = Directions[localDirInd[0]];
                        //    var n = Directions[localDirInd[1]];
                        //}
                    }
                }
            }
            //Fastener.AddFastenersInformation(assemblyGraph, screwsAndBolts, solidsNoFastener, solidPrimitive);
            foreach (var node in assemblyGraph.nodes)
            {
                var freeDirs = FreeDirectionFinder(node, assemblyGraph);
                UnconnectedBlockingDetermination.FiniteDirectionsBetweenUnconnectedParts(node, solids, freeDirs, assemblyGraph);
            }

            return globalDirPool;
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

        private static void AddingNodesToGraph(designGraph assemblyGraph, List<TessellatedSolid> solids)//,
            //Dictionary<TessellatedSolid, double[]> gears)
        {
            foreach (var solid in solids)
            {
                var node = assemblyGraph.addNode(solid.Name);
                //if (gears.Keys.Contains(solid))
                //{
                //    node.localLabels.Add(DisConstants.Gear);
                //    node.localVariables.Add(DisConstants.GearNormal);
                //    node.localVariables.AddRange(gears[solid]);
                //}
            }
        }

        private static List<int> FreeDirectionFinder(node node, designGraph assemblyGraph)
        {
            throw new NotImplementedException();
        }

    }
}
