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

        internal static List<int> Run(designGraph assemblyGraph, List<TessellatedSolid> solids, bool classifyFastener = false, bool threaded = false)
        {
            Solids = new List<TessellatedSolid>(solids);

            // Generate a good number of directions on the surface of a sphere
            //------------------------------------------------------------------------------------------
            Directions = IcosahedronPro.DirectionGeneration();
            DisassemblyDirections.Directions = new List<double[]>(Directions);
            var globalDirPool = new List<int>();
            
            // From repeated parts take only one of them, and do the primitive classification on that:
            //------------------------------------------------------------------------------------------
            var multipleRefs = DuplicatePartsDetector(solids);
            var solidPrimitive = BlockingDetermination.PrimitiveMaker(solids);//multipleRefs.Keys.ToList());
            //foreach (var mRef in multipleRefs.Keys)
            //    foreach (var duplicated in multipleRefs[mRef])
            //        solidPrimitive.Add(duplicated, solidPrimitive[mRef]);
            
            // Creating OBB for every solid
            //------------------------------------------------------------------------------------------
            //PartitioningSolid.CreateOBB(solids);
            
            // Detect fasteners and gear mates
            //------------------------------------------------------------------------------------------
            var screwsAndBolts = new HashSet<TessellatedSolid>();
            if (classifyFastener)
                screwsAndBolts = BoltAndGearDetection.ScrewAndBoltDetector(solidPrimitive, multipleRefs,false, threaded);
            //var gears = BoltAndGearDetection.GearDetector(solidPrimitive);

            
            // Add the solids as nodes to the graph. Excluede the fasteners 
            //------------------------------------------------------------------------------------------
            var solidsNoFastener = new List<TessellatedSolid>(solids);
            foreach (var bolt in screwsAndBolts)
                solidsNoFastener.Remove(bolt);
            DisassemblyDirections.Solids = new List<TessellatedSolid>(solidsNoFastener);
            AddingNodesToGraph(assemblyGraph, solidsNoFastener); //, gears, screwsAndBolts);

            // Implementing region octree for every solid
            //------------------------------------------------------------------------------------------
            PartitioningSolid.CreatePartitions(solidsNoFastener);

            
            // Part to part interaction to obtain removal directions between every connected pair
            //------------------------------------------------------------------------------------------
            var s = Stopwatch.StartNew();
            s.Start();
            Console.WriteLine();
            Console.WriteLine("Blocking Determination is running ....");
            for (var i = 0; i < solidsNoFastener.Count - 1; i++)
            {
                var solid1 = solidsNoFastener[i];
                var solid1Primitives = solidPrimitive[solid1];
                for (var j = i+1; j < solidsNoFastener.Count; j++)
                {
                    var solid2 = solidsNoFastener[j];
                    var solid2Primitives = solidPrimitive[solid2];
                    List<int> localDirInd;
                    double certainty;
                    if (BlockingDetermination.DefineBlocking(assemblyGraph, solid1, solid2, solid1Primitives,
                        solid2Primitives, globalDirPool, out localDirInd, out certainty))
                    {
                        // I wrote the code in a way that "solid1" is always "Reference" and "solid2" is always "Moving".
                        List<int> finDirs, infDirs;
                        NonadjacentBlockingDetermination.FiniteDirectionsBetweenConnectedPartsWithPartitioning(solid1, solid2,
                           localDirInd, out finDirs, out infDirs);
                        var from = assemblyGraph[solid2.Name]; // Moving
                        var to = assemblyGraph[solid1.Name]; // Reference
                        assemblyGraph.addArc((node) from, (node) to, "", typeof (Connection));
                        var a = (Connection) assemblyGraph.arcs.Last();
                        a.Certainty = certainty;
                        AddInformationToArc(a, new List<int>(), localDirInd);
                    }
                }
            }
            Fastener.AddFastenersInformation(assemblyGraph, screwsAndBolts, solidsNoFastener, solidPrimitive);
            s.Stop();
            Console.WriteLine("Blocking Determination is done in:" + "     " + s.Elapsed);
            return globalDirPool;
        }

        private static Dictionary<TessellatedSolid, List<TessellatedSolid>> DuplicatePartsDetector(List<TessellatedSolid> solids)
        {
            // If the number of vertcies and number of faces are exactly the same and also the volumes are equal.
            // Not only we need to detect the repeated parts, but also we need to store their transformation matrix
            // We need the transformatiuon matrix to transform information we get from primitive classification.
            // Is it really worth it? yes. Because we will most likely detect fasteners after this step, so we will
            // have a lot of similar parts.
            var multipleRefs = new Dictionary<TessellatedSolid, List<TessellatedSolid>>();
            foreach (var solid in solids)
            {
                var exist = multipleRefs.Keys.Where(
                    k =>
                        k.Faces.Count() == solid.Faces.Count() &&
                        Math.Abs(k.Vertices.Count() - solid.Vertices.Count()) < 2 &&
                        (Math.Max(k.SurfaceArea, solid.SurfaceArea) - Math.Min(k.SurfaceArea, solid.SurfaceArea))/
                        Math.Max(k.SurfaceArea, solid.SurfaceArea) < 0.001).ToList();
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
            return dirsG[0].Where(dir => dirsG.All(dirs => dirs.Any(d => Math.Abs(1 - d.dotProduct(dir)) < OverlappingFuzzification.CheckWithGlobDirsParall))).ToList();
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
            return dirsG[0].Where(dir => dirsG.All(dirs => dirs.Any(d => Math.Abs(1 - d.dotProduct(dir)) < OverlappingFuzzification.CheckWithGlobDirsParall))).ToList();
        }

    }
}
