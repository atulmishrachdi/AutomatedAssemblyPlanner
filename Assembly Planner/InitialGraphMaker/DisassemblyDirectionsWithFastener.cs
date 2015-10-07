using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
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

        internal static List<int> Run(designGraph assemblyGraph, List<TessellatedSolid> solids, bool classifyFastener = false)
        {
            var s = Stopwatch.StartNew();
            s.Start();
            Solids = new List<TessellatedSolid>(solids);

            // Generate a good number of directions on the surface of a sphere
            //------------------------------------------------------------------------------------------
            Directions = IcosahedronPro.DirectionGeneration();
            DisassemblyDirections.Directions = new List<double[]>(Directions);
            var globalDirPool = new List<int>();
            
            // From repeated parts take only one of them, and do the primitive classification on that:
            //------------------------------------------------------------------------------------------
            var multipleRefs = DuplicatePartsDetector(solids);
            var solidPrimitive = BlockingDetermination.PrimitiveMaker(multipleRefs.Keys.ToList());
            foreach (var mRef in multipleRefs.Keys)
                foreach (var duplicated in multipleRefs[mRef])
                    solidPrimitive.Add(duplicated, solidPrimitive[mRef]);

            s.Stop();
            Console.WriteLine("Primitive classification:" + "     " + s.Elapsed);
            
            // Creating OBB for every solid
            //------------------------------------------------------------------------------------------
            s.Restart();
            Parallel.ForEach(solids, PartitioningSolid.CreateOBB);
            s.Stop();
            Console.WriteLine("OBB Creation:" + "     " + s.Elapsed);
            
            // Detect fasteners and gear mates
            //------------------------------------------------------------------------------------------
            s.Restart();
            var screwsAndBolts = new HashSet<TessellatedSolid>();
            if (classifyFastener)
                screwsAndBolts = BoltAndGearDetection.ScrewAndBoltDetector(solidPrimitive);
            //var gears = BoltAndGearDetection.GearDetector(solidPrimitive);
            s.Stop();
            Console.WriteLine("Gear and Fastener Detection:" + "     " + s.Elapsed);
            
            // Add the solids as nodes to the graph. Excluede the fasteners 
            //------------------------------------------------------------------------------------------
            var solidsNoFastener = new List<TessellatedSolid>(solids);
            foreach (var bolt in screwsAndBolts)
                solidsNoFastener.Remove(bolt);
            DisassemblyDirections.Solids = new List<TessellatedSolid>(solidsNoFastener);
            AddingNodesToGraph(assemblyGraph, solidsNoFastener); //, gears, screwsAndBolts);

            // Implementing region octree for every solid
            //------------------------------------------------------------------------------------------
            s.Restart();
            Parallel.ForEach(solidsNoFastener, PartitioningSolid.CreatePartitions);
            s.Stop();
            Console.WriteLine("Octree Generation:" + "     " + s.Elapsed);
            
            // Part to part interaction to obtain removal directions between every connected pair
            //------------------------------------------------------------------------------------------
            s.Restart();
            for (var i = 0; i < solidsNoFastener.Count - 1; i++)
            {
                var solid1 = solidsNoFastener[i];
                var solid1Primitives = solidPrimitive[solid1];
                for (var j = i+1; j < solidsNoFastener.Count; j++)
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
                        NonadjacentBlockingDetermination.FiniteDirectionsBetweenConnectedPartsWithPartitioning(solid1, solid2,
                            localDirInd, out finDirs, out infDirs);
                        var from = assemblyGraph[solid2.Name]; // Moving
                        var to = assemblyGraph[solid1.Name]; // Reference
                        assemblyGraph.addArc((node) from, (node) to, "", typeof (Connection));
                        var a = (Connection) assemblyGraph.arcs.Last();
                        a.Certainty = (!infDirs.Any() || infDirs.Count == Directions.Count) ? 0.0 : 1.0;
                        AddInformationToArc(a, finDirs, infDirs);
                    }
                }
            }
            Fastener.AddFastenersInformation(assemblyGraph, screwsAndBolts, solidsNoFastener, solidPrimitive);
            s.Stop();
            Console.WriteLine("Blocking Determination:" + "     " + s.Elapsed);
            return globalDirPool;
        }

        private static Dictionary<TessellatedSolid, List<TessellatedSolid>> DuplicatePartsDetector(List<TessellatedSolid> solids)
        {
            // If the number of vertcies and number of faces are exactly the same and also the volumes are equal.
            var multipleRefs = new Dictionary<TessellatedSolid, List<TessellatedSolid>>();
            foreach (var solid in solids)
            {
                var exist = multipleRefs.Keys.Where(
                    k =>
                        k.Vertices.Count() == solid.Vertices.Count() && k.Faces.Count() == solid.Faces.Count() &&
                        Math.Abs(k.Volume - solid.Volume) < 0.001).ToList();
                if (exist.Count == 0)
                {
                    var d = new List<TessellatedSolid>();
                    multipleRefs.Add(solid,d);
                }
                else
                {
                    multipleRefs[exist[0]].Add(solid);
                }
            }
            return multipleRefs;
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
                assemblyGraph.addNode(solid.Name, typeof(Component));
                var c = (Component)assemblyGraph.nodes.Last();
                c.CenterOfMass = solid.Center;
                c.Volume = solid.Volume;
                c.Mass = solid.Volume;
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
