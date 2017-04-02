using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BaseClasses;
using BaseClasses.Representation;
using Fastener_Detection;
using Geometric_Reasoning;
using StarMathLib;
using TVGL;

namespace Graph_Generation
{
    class Program
    {
        public static Dictionary<string, List<TessellatedSolid>> SolidsNoFastenerSimplified = new Dictionary<string, List<TessellatedSolid>>();
        public static Dictionary<string, List<TessellatedSolid>> SimplifiedSolids = new Dictionary<string, List<TessellatedSolid>>();
        private static designGraph AssemblyGraph;
        static void Main(string[] args)
        {
            var Solids = StartProcess.Solids;
            var solidsNoFastener = StartProcess.SolidsNoFastener;
            //PrintOutSomeInitialStats();
            var globalDirPool = new List<int>();
            // Detect gear mates
            //------------------------------------------------------------------------------------------
            var gears = GearDetector.Run(StartProcess.PartsWithOneGeom, StartProcess.SolidPrimitive);
            var sw = new Stopwatch();
            sw.Start();

            // Add the solids as nodes to the graph. Exclude the fasteners 
            //------------------------------------------------------------------------------------------
            //DisassemblyDirections.Solids = new List<TessellatedSolid>(solidsNoFastener);
            AddingNodesToGraph(AssemblyGraph, solidsNoFastener); //, gears, screwsAndBolts);

            // Implementing region octree for every solid
            //------------------------------------------------------------------------------------------
            PartitioningSolid.Partitions = new Dictionary<TessellatedSolid, Partition[]>();
            PartitioningSolid.PartitionsAABB = new Dictionary<TessellatedSolid, PartitionAABB[]>();
            PartitioningSolid.CreatePartitions(solidsNoFastener);

            // Part to part interaction to obtain removal directions between every connected pair
            //------------------------------------------------------------------------------------------

            Console.WriteLine(" \n\nAdjacent Blocking Determination ...");
            var width = 55;
            //LoadingBar.start(width, 0);

            BlockingDetermination.OverlappingSurfaces = new List<OverlappedSurfaces>();
            var solidNofastenerList = solidsNoFastener.ToList();
            long totalTriTobeChecked = 0;
            var overlapCheck = new HashSet<KeyValuePair<string, List<TessellatedSolid>>[]>();
            for (var i = 0; i < solidsNoFastener.Count - 1; i++)
            {
                var subAssem1 = solidNofastenerList[i];
                for (var j = i + 1; j < solidsNoFastener.Count; j++)
                {
                    var subAssem2 = solidNofastenerList[j];
                    overlapCheck.Add(new[] { subAssem1, subAssem2 });
                    var tri2Sub1 = subAssem1.Value.Sum(s => s.Faces.Length);
                    var tri2Sub2 = subAssem2.Value.Sum(s => s.Faces.Length);
                    totalTriTobeChecked += tri2Sub1 * tri2Sub2;
                }
            }
            var total = overlapCheck.Count;
            var refresh = (int)Math.Ceiling(((float)total) / ((float)(width * 4)));
            var check = 0;
            long counter = 0;

            //foreach (var each in overlapCheck)
            Parallel.ForEach(overlapCheck, each =>
            {
                if (check % refresh == 0)
                {
                    //LoadingBar.refresh(width, ((float)check) / ((float)total));
                }
                check++;
                var localDirInd = new List<int>();
                for (var t = 0; t < StartProcess.Directions.Count; t++)
                    localDirInd.Add(t);
                var connected = false;
                var certainty = 0.0;
                foreach (var solid1 in each[0].Value)
                {
                    foreach (var solid2 in each[1].Value)
                    {
                        counter += solid1.Faces.Length * solid2.Faces.Length;
                        double localCertainty;
                        var blocked = BlockingDetermination.DefineBlocking(solid1, solid2, globalDirPool,
                            localDirInd, out localCertainty);
                        if (connected == false)
                            connected = blocked;
                        if (localCertainty > certainty)
                            certainty = localCertainty;
                    }
                }
                if (connected)
                {
                    // I wrote the code in a way that "solid1" is always "Reference" and "solid2" is always "Moving".
                    // Update the romoval direction if it is a gear mate:
                    localDirInd = GearDetector.UpdateRemovalDirectionsIfGearMate(each[0].Value,
                        each[1].Value, gears, localDirInd);
                    List<int> finDirs, infDirs;
                    NonadjacentBlockingDetermination.FiniteDirectionsBetweenConnectedPartsWithPartitioning(
                        each[0].Value, each[1].Value, localDirInd, out finDirs, out infDirs);
                    lock (AssemblyGraph)
                    {
                        var from = AssemblyGraph[each[1].Key]; // Moving
                        var to = AssemblyGraph[each[0].Key]; // Reference
                        AssemblyGraph.addArc((node)from, (node)to, "", typeof(Connection));
                        var a = (Connection)AssemblyGraph.arcs.Last();
                        a.Certainty = certainty;
                        AddInformationToArc(a, finDirs, infDirs);
                    }
                }
            }//
            );
            //LoadingBar.refresh(width, 1);
            Console.WriteLine("\n");
            FastenerFunctions.AddFastenersInformation(AssemblyGraph, solidsNoFastener, StartProcess.SolidPrimitive);
            // create oppositeDirections for global direction pool.
            FindingOppositeDirectionsForGlobalPool(globalDirPool);

            // Simplify the solids, before doing anything
            //------------------------------------------------------------------------------------------
            foreach (var solid in solidsNoFastener)
                SolidsNoFastenerSimplified.Add(solid.Key, SimplifiedSolids[solid.Key]);
            SimplifySolids(SimplifiedSolids, 0.7);

            // Implementing region octree for every solid
            //------------------------------------------------------------------------------------------
            PartitioningSolid.Partitions = new Dictionary<TessellatedSolid, Partition[]>();
            PartitioningSolid.PartitionsAABB = new Dictionary<TessellatedSolid, PartitionAABB[]>();
            PartitioningSolid.CreatePartitions(SimplifiedSolids);

            CheckToHaveConnectedGraph(AssemblyGraph);
            ///return globalDirPool;
        }

        private static Dictionary<TessellatedSolid, List<TessellatedSolid>> DuplicatePartsDetector(List<TessellatedSolid> solids)
        {
            // If the number of vertcies and number of faces are exactly the same and also the volumes are equal.
            // Not only we need to detect the repeated parts, but also we need to store their transformation matrix
            // We need the transformatiuon matrix to transform information we get from primitive classification.
            // Is it really worth it? yes. Because we will most likely detect fasteners after this step, so we will
            // have a lot of similar parts.

            // When we are detecting duplicate parts, we will only do it for the parts with one geomtery. Why?
            //  because these duplicates are only used in fastener detection. And fasteners cannot be seen in
            //  parts with more than one geometry
            //Bridge.StatusReporter.ReportStatusMessage("Detecting Duplicated Solids ...", 1);
            //Bridge.StatusReporter.ReportProgress(0);
            var multipleRefs = new Dictionary<TessellatedSolid, List<TessellatedSolid>>();
            for (var i = 0; i < solids.Count; i++)
            {
                var solid = solids[i];
                var exist = multipleRefs.Keys.Where(
                    k =>
                        (Math.Abs(k.Faces.Count() - solid.Faces.Count()) / Math.Max(k.Faces.Count(), solid.Faces.Count()) < 0.01) &&
                        Math.Abs(k.Vertices.Count() - solid.Vertices.Count()) / Math.Max(k.Vertices.Count(), solid.Vertices.Count()) < 0.01 &&
                        (Math.Max(k.SurfaceArea, solid.SurfaceArea) - Math.Min(k.SurfaceArea, solid.SurfaceArea)) /
                        Math.Max(k.SurfaceArea, solid.SurfaceArea) < 0.001).ToList();
                if (exist.Count == 0)
                {
                    var d = new List<TessellatedSolid>();
                    multipleRefs.Add(solid, d);
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


        private static void SimplifySolids(Dictionary<string, List<TessellatedSolid>> subAssems, double percentage)
        {
            foreach (var sa in subAssems)
            {
                foreach (var ts in sa.Value)
                {
                    if (ts.Errors == null || ((ts.Errors.EdgesThatDoNotLinkBackToFace == null ||
                                               ts.Errors.EdgesThatDoNotLinkBackToFace.Count < 2) &&
                                              (ts.Errors.SingledSidedEdges == null ||
                                               ts.Errors.SingledSidedEdges.Count < 5)))
                    {
                        //gCounter++;
                        try
                        {
                            ts.SimplifyByPercentage(percentage);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
            }
        }

        private static void AddingNodesToGraph(designGraph assemblyGraph, Dictionary<string, List<TessellatedSolid>> solids)//,
        //Dictionary<TessellatedSolid, double[]> gears)
        {
            foreach (var solidName in solids.Keys)
            {
                assemblyGraph.addNode(solidName, typeof(Component));
                var c = (Component)assemblyGraph.nodes.Last();
                c.CenterOfMass = COMCalculator(solids[solidName]);
                c.Volume = solids[solidName].Sum(s => s.Volume);
                c.Mass = solids[solidName].Sum(s => s.Volume);
                //if (gears.Keys.Contains(solid))
                //{
                //    Component.localLabels.Add(DisConstants.Gear);
                //    Component.localVariables.Add(DisConstants.GearNormal);
                //    Component.localVariables.AddRange(gears[solid]);
                //}
            }
        }

        private static double[] COMCalculator(List<TessellatedSolid> geometries)
        {
            var totalMass = geometries.Sum(s => s.Volume);
            var sumCenterOfMass = new[] { 0.0, 0.0, 0.0 };
            foreach (var geom in geometries)
            {
                sumCenterOfMass = sumCenterOfMass.add(geom.Center.multiply(geom.Volume));
            }
            return sumCenterOfMass.divide(totalMass);
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
                        iniDirs.Add(StartProcess.Directions[(int)arc.localVariables[i]]);
                else
                    for (var i = indexL0 + 1; i < indexU0; i++)
                        iniDirs.Add((StartProcess.Directions[(int)arc.localVariables[i]]).multiply(-1));
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
                        iniDirs.Add(StartProcess.Directions[(int)arc.localVariables[i]]);
                else
                    for (var i = indexL0 + 1; i < indexU0; i++)
                        iniDirs.Add((StartProcess.Directions[(int)arc.localVariables[i]]).multiply(-1));
                dirsG.Add(iniDirs);
            }
            return dirsG[0].Where(dir => dirsG.All(dirs => dirs.Any(d => Math.Abs(1 - d.dotProduct(dir)) < OverlappingFuzzification.CheckWithGlobDirsParall))).ToList();
        }


        private static List<TessellatedSolid> RemoveFastenersFromTheSolidsList(List<TessellatedSolid> solids, HashSet<TessellatedSolid> screwsAndBolts)
        {
            var solidsNoFastener = new List<TessellatedSolid>(solids);
            foreach (var bolt in screwsAndBolts)
                solidsNoFastener.Remove(bolt);
            foreach (var fastener in FastenerDetector.Fasteners)
                solidsNoFastener.Remove(fastener.Solid);
            foreach (var nuts in FastenerDetector.Nuts)
                solidsNoFastener.Remove(nuts.Solid);
            foreach (var washer in FastenerDetector.Washers)
                solidsNoFastener.Remove(washer.Solid);
            return solidsNoFastener;
        }

        private static void FindingOppositeDirections()
        {
            StartProcess.DirectionsAndOpposits = new Dictionary<int, int>();
            for (int i = 0; i < StartProcess.Directions.Count; i++)
            {
                var dir = StartProcess.Directions[i];
                var oppos = StartProcess.Directions.First(d => d[0] == -dir[0] && d[1] == -dir[1] && d[2] == -dir[2]);
                StartProcess.DirectionsAndOpposits.Add(i, StartProcess.Directions.IndexOf(oppos));
            }
        }


        internal static void FindingOppositeDirectionsForGlobalPool(List<int> globalDirPool)
        {
            StartProcess.DirectionsAndOppositsForGlobalpool = new Dictionary<int, int>();
            var toBeAddedToGDir = new List<int>();
            for (int i = 0; i < globalDirPool.Count; i++)
            {
                var dir = StartProcess.Directions[globalDirPool[i]];
                var temp =
                    globalDirPool.Where(
                        d =>
                            Math.Abs(1 +
                            StartProcess.Directions[d].dotProduct(
                                StartProcess.Directions[globalDirPool[i]])) < 0.01).ToList();
                if (temp.Any())
                    StartProcess.DirectionsAndOppositsForGlobalpool.Add(globalDirPool[i], temp[0]);
                else
                {
                    StartProcess.Directions.Add(dir.multiply(-1));
                    StartProcess.DirectionsAndOppositsForGlobalpool.Add(globalDirPool[i], StartProcess.Directions.Count - 1);
                    toBeAddedToGDir.Add(StartProcess.Directions.Count - 1);
                }
            }
            foreach (var newD in toBeAddedToGDir)
            {
                globalDirPool.Add(newD);
                var key = StartProcess.DirectionsAndOppositsForGlobalpool.Where(k => k.Value == newD).ToList();
                StartProcess.DirectionsAndOppositsForGlobalpool.Add(newD, key[0].Key);
            }
        }
        private static void CheckToHaveConnectedGraph(designGraph assemblyGraph)
        {
            // The code will crash if the graph is not connected
            // let's take a look:
            var batches = new List<HashSet<Component>>();
            var stack = new Stack<Component>();
            var visited = new HashSet<Component>();
            var globalVisited = new HashSet<Component>();
            foreach (Component Component in assemblyGraph.nodes.Where(n => !globalVisited.Contains(n)))
            {
                stack.Clear();
                visited.Clear();
                stack.Push(Component);
                while (stack.Count > 0)
                {
                    var pNode = stack.Pop();
                    visited.Add(pNode);
                    globalVisited.Add(pNode);
                    List<Connection> a2;
                    lock (pNode.arcs)
                        a2 = pNode.arcs.Where(a => a is Connection).Cast<Connection>().ToList();

                    foreach (Connection arc in a2)
                    {
                        if (!assemblyGraph.nodes.Contains(arc.From) || !assemblyGraph.nodes.Contains(arc.To)) continue;
                        var otherNode = (Component)(arc.From == pNode ? arc.To : arc.From);
                        if (visited.Contains(otherNode))
                            continue;
                        stack.Push(otherNode);
                    }
                }
                if (visited.Count == assemblyGraph.nodes.Count)
                {
                    return;
                }
                batches.Add(new HashSet<Component>(visited));
            }
            Console.WriteLine("\nSome of the assembly parts are not connected to the rest of the model.");
            var referenceBatch = batches[0];
            var c = false;
            var visits = 0;
            var loop = 0;
            while (referenceBatch.Count < assemblyGraph.nodes.Count)
            {
                loop++;
                if (loop >= 15) break;
                foreach (var rb in referenceBatch)
                {
                    for (var j = 1; j < batches.Count; j++)
                    {
                        foreach (var b in batches[j])
                        {
                            foreach (var p1 in StartProcess.Solids[rb.name])
                            {
                                foreach (var p2 in StartProcess.Solids[b.name])
                                {
                                    if (BlockingDetermination.BoundingBoxOverlap(p1, p2))
                                    {
                                        if (BlockingDetermination.ConvexHullOverlap(p1, p2))
                                        {
                                            visits++;
                                            if (visits == 1)
                                                Console.WriteLine(
                                                    "\n   * Since the graph needs to be connected, the following connections are added by the software:");
                                            // add a connection with low cetainty between them
                                            var lastAdded = (Connection)assemblyGraph.addArc(rb, b, "", typeof(Connection));
                                            lastAdded.Certainty = 0.1;
                                            referenceBatch.UnionWith(batches[j]);
                                            batches.RemoveAt(j);
                                            c = true;
                                            Console.WriteLine("\n      - " + lastAdded.XmlFrom + lastAdded.XmlTo);
                                        }
                                    }
                                    if (c) break;
                                }
                                if (c) break;
                            }
                            if (c) break;
                        }
                        if (c) break;
                    }
                    if (c) break;
                }
            }
            if (loop < 15)
                Console.WriteLine(
                    "\n   * When you are reviewing the connections, please pay a closer attention to the connections above");
            else
                Console.WriteLine("\n   * Some connections must be added manually between the following batches");
            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                Console.WriteLine("\n      - Batch " + i + ":");
                foreach (var component in batch)
                    Console.WriteLine("         + " + component.name);
            }
        }
        internal static bool GraphIsConnected(designGraph assemblyGraph)
        {
            var batches = new List<HashSet<Component>>();
            var stack = new Stack<Component>();
            var visited = new HashSet<Component>();
            var globalVisited = new HashSet<Component>();
            foreach (Component Component in assemblyGraph.nodes.Where(n => !globalVisited.Contains(n)))
            {
                stack.Clear();
                visited.Clear();
                stack.Push(Component);
                while (stack.Count > 0)
                {
                    var pNode = stack.Pop();
                    visited.Add(pNode);
                    globalVisited.Add(pNode);
                    List<Connection> a2;
                    lock (pNode.arcs)
                        a2 = pNode.arcs.Where(a => a is Connection).Cast<Connection>().ToList();

                    foreach (Connection arc in a2)
                    {
                        if (!assemblyGraph.nodes.Contains(arc.From) || !assemblyGraph.nodes.Contains(arc.To)) continue;
                        var otherNode = (Component)(arc.From == pNode ? arc.To : arc.From);
                        if (visited.Contains(otherNode))
                            continue;
                        stack.Push(otherNode);
                    }
                }
                if (visited.Count == assemblyGraph.nodes.Count)
                {
                    return true;
                }
                batches.Add(new HashSet<Component>(visited));
            }
            Console.WriteLine("\nThe reviewed graph is not connected! The following batch(es) are discunnected from the rest of the model.");
            var sorted = batches.OrderBy(o => o.Count).ToList();
            for (var i = 0; i < sorted.Count - 1; i++)
            {
                Console.WriteLine("\n   * Batch 1:");
                foreach (var part in sorted[i])
                    Console.WriteLine("      - " + part.name);
            }
            Console.WriteLine("\nPlease add the missing connections.");
            return false;
        }
    }
}
