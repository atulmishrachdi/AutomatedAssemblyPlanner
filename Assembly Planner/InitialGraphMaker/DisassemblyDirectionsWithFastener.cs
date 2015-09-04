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
using TVGL;
using Assembly_Planner.GraphSynth.BaseClasses;

namespace Assembly_Planner
{
    internal class DisassemblyDirectionsWithFastener
    {
        public static List<double[]> Directions = new List<double[]>();
        internal static List<TessellatedSolid> Solids;
        internal static Dictionary<int, List<Component[]>> NonAdjacentBlocking = new Dictionary<int, List<Component[]>>(); //Component[0] is blocked by Component[1]

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
            AddingNodesToGraph(assemblyGraph, solidsNoFastener); //, gears, screwsAndBolts);

            for (var i = 0; i < solidsNoFastener.Count - 1; i++)
            {
                var solid1 = solidsNoFastener[i];
                var solid1Primitives = solidPrimitive[solid1];
                for (var j = i + 1; j < solidsNoFastener.Count; j++)
                {
                    var solid2 = solidsNoFastener[j];
                    var solid2Primitives = solidPrimitive[solid2];
                    List<int> localDirInd;
                    if (BlockingDetermination.DefineBlocking(assemblyGraph, solid1, solid2, solid1Primitives,
                        solid2Primitives,
                        globalDirPool, out localDirInd))
                    {
                        // I wrote the code in a way that "solid1" is always "Reference" and "solid2" is always "Moving".
                        List<int> finDirs, infDirs;
                        NonadjacentBlockingDetermination.FiniteDirectionsBetweenConnectedParts(solid1, solid2,
                            localDirInd, out finDirs, out infDirs);
                        var from = assemblyGraph[solid2.Name]; // Moving
                        var to = assemblyGraph[solid1.Name]; // Reference
                        assemblyGraph.addArc((node) from, (node) to, "", typeof (Connection));
                        var a = (Connection) assemblyGraph.arcs.Last();
                        AddInformationToArc(a, finDirs, infDirs);
                    }
                }
            }
            Fastener.AddFastenersInformation(assemblyGraph, screwsAndBolts, solidsNoFastener, solidPrimitive);
            return globalDirPool;
        }

        private static void AddInformationToArc(Connection a, List<int> finDirs, List<int> infDirs)
        {
            a.FiniteDirections.AddRange(finDirs);
            a.InfiniteDirections.AddRange(infDirs);
        }

        private static void AddingNodesToGraph(designGraph assemblyGraph, List<TessellatedSolid> solids)//,
        //Dictionary<TessellatedSolid, double[]> gears)
        {
            foreach (var solid in solids)
            {
                var Component = assemblyGraph.addNode(solid.Name, typeof(Component));
                //if (gears.Keys.Contains(solid))
                //{
                //    Component.localLabels.Add(DisConstants.Gear);
                //    Component.localVariables.Add(DisConstants.GearNormal);
                //    Component.localVariables.AddRange(gears[solid]);
                //}
            }
        }

        public static List<double[]> FreeGlobalDirectionFinder(Component Component)
        {
            var dirsG = new List<List<double[]>>();
            foreach (Connection arc in Component.arcs.Where(a => a is Connection))
            {
                var iniDirs = new List<double[]>();
                var indexL0 = arc.localVariables.IndexOf(DisConstants.DirIndLowerBound);
                var indexU0 = arc.localVariables.IndexOf(DisConstants.DirIndUpperBound);
                if (Component == arc.From)
                    for (var i = indexL0 + 1; i < indexU0; i++)
                        iniDirs.Add(Directions[(int)arc.localVariables[i]]);
                else
                    for (var i = indexL0 + 1; i < indexU0; i++)
                        iniDirs.Add((Directions[(int)arc.localVariables[i]]).multiply(-1));
                dirsG.Add(iniDirs);
            }
            return dirsG[0].Where(dir => dirsG.All(dirs => dirs.Any(d => Math.Abs(1 - d.dotProduct(dir)) < ConstantsPrimitiveOverlap.CheckWithGlobDirsParall))).ToList();
        }
        
        public static List<double[]> FreeLocalDirectionFinder(Component Component, List<Component> subgraph)
        {
            if (!subgraph.Contains(Component)) return null;
            var dirsG = new List<List<double[]>>();
            foreach (Connection arc in Component.arcs.Where(a => a is Connection))
            {
                if (!subgraph.Contains(arc.From) || !subgraph.Contains(arc.To)) continue;
                var iniDirs = new List<double[]>();
                var indexL0 = arc.localVariables.IndexOf(DisConstants.DirIndLowerBound);
                var indexU0 = arc.localVariables.IndexOf(DisConstants.DirIndUpperBound);
                if (Component == arc.From)
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
