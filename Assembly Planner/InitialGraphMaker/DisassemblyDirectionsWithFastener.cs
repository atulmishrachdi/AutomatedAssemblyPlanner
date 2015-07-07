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
    internal class DisassemblyDirectionsWithFastener
    {
        public static List<double[]> Directions = new List<double[]>();
        internal static List<TessellatedSolid> Solids;
        internal static Dictionary<int, List<node[]>> NonAdjacentBlocking = new Dictionary<int, List<node[]>>(); //node[0] is blocked by node[1]
        internal static List<int> Run(designGraph assemblyGraph, List<TessellatedSolid> solids)
        {
            Solids = new List<TessellatedSolid>(solids);
            Directions = IcosahedronPro.DirectionGeneration();
            DisassemblyDirections.Directions = new List<double[]>(Directions);
            
            var globalDirPool = new List<int>();
            var solidPrimitive = BlockingDetermination.PrimitiveMaker(solids);
            var screwsAndBolts = BoltAndGearDetection.ScrewAndBoltDetector(solidPrimitive);
            //var gears = BoltAndGearDetection.GearDetector(solidPrimitive);

            var solidsNoFastener = new List<TessellatedSolid>(solids);
            foreach (var bolt in screwsAndBolts)
                solidsNoFastener.Remove(bolt);
            DisassemblyDirections.Solids = new List<TessellatedSolid>(solidsNoFastener);
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
                        //List<int> finDirs, infDirs;
                        //UnconnectedBlockingDetermination.FiniteDirectionsBetweenConnectedParts(solid1, solid2, localDirInd, out finDirs, out infDirs);
                        var from = assemblyGraph[solid2.Name]; // Moving
                        var to = assemblyGraph[solid1.Name];   // Reference
                        assemblyGraph.addArc((node)from, (node)to);
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
            Fastener.AddFastenersInformation(assemblyGraph, screwsAndBolts, solidsNoFastener, solidPrimitive);
            //foreach (var node in assemblyGraph.nodes)
            //{
            //    var freeDirs = FreeDirectionFinder(node);
            //    var freeDirInd = (from dir in freeDirs from gDir in Directions where dir[0] == gDir[0] && dir[1] == gDir[1] && dir[2] == gDir[2] select Directions.IndexOf(gDir)).ToList();
            //    UnconnectedBlockingDetermination.FiniteDirectionsBetweenUnconnectedParts(node, solidsNoFastener, freeDirInd, assemblyGraph);
            //}
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

        private static List<double[]> FreeDirectionFinder(node node)
        {
            var dirsG = new List<List<double[]>>();
            foreach (arc arc in node.arcs.Where(a => a is arc))
            {
                var iniDirs = new List<double[]>();
                var indexL0 = arc.localVariables.IndexOf(GraphConstants.DirIndLowerBound);
                var indexU0 = arc.localVariables.IndexOf(GraphConstants.DirIndUpperBound);
                if (node == arc.From)
                    for (var i = indexL0 + 1; i < indexU0; i++)
                        iniDirs.Add(Directions[(int)arc.localVariables[i]]);
                else
                    for (var i = indexL0 + 1; i < indexU0; i++)
                        iniDirs.Add((Directions[(int)arc.localVariables[i]]).multiply(-1));
                dirsG.Add(iniDirs);
            }
            return dirsG[0].Where(dir => dirsG.All(dirs => dirs.Any(d => Math.Abs(1 - d.dotProduct(dir)) < ConstantsPrimitiveOverlap.CheckWithGlobDirsParall))).ToList();
        }

    }
}
