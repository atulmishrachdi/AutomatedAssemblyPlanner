using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using StarMathLib;
using TVGL;
using Assembly_Planner.GraphSynth.BaseClasses;
using TVGL.IOFunctions;
using Component = Assembly_Planner.GraphSynth.BaseClasses.Component;

namespace Assembly_Planner
{
    internal class DisassemblyDirectionsWithFastener
    {
        public static List<double[]> Directions = new List<double[]>();

        internal static Dictionary<int, List<Component[]>> NonAdjacentBlocking =
            new Dictionary<int, List<Component[]>>(); //Component[0] is blocked by Component[1]

        internal static Dictionary<TessellatedSolid, List<PrimitiveSurface>> SolidPrimitive =
            new Dictionary<TessellatedSolid, List<PrimitiveSurface>>();
        internal static Dictionary<string, List<TessellatedSolid>> Solids;
        internal static Dictionary<string, List<TessellatedSolid>> SolidsNoFastener;
        internal static List<TessellatedSolid> PartsWithOneGeom;

        protected static int gCounter = 0;

        internal static void RunGeometricReasoning(Dictionary<string, List<TessellatedSolid>> solids)
        {
            // Generate a good number of directions on the surface of a sphere
            //------------------------------------------------------------------------------------------
            //SimplifySolids(solids);
            Directions = IcosahedronPro.DirectionGeneration(1);
            DisassemblyDirections.Directions = new List<double[]>(Directions);
            FindingOppositeDirections();
            // Creating Bounding Geometries for every solid
            //------------------------------------------------------------------------------------------
            //Bridge.StatusReporter.ReportStatusMessage("Creating Bounding Geometries ... ", 1);
            BoundingGeometry.OrientedBoundingBoxDic = new Dictionary<TessellatedSolid, BoundingBox>();
            BoundingGeometry.BoundingCylinderDic = new Dictionary<TessellatedSolid, BoundingCylinder>();
            BoundingGeometry.CreateOBB2(solids);
            BoundingGeometry.CreateBoundingCylinder(solids);
            //Bridge.StatusReporter.PrintMessage("BOUNDING GEOMETRIES ARE SUCCESSFULLY CREATED.", 0.5f);

            // Detecting Springs
            SpringDetector.DetectSprings(solids);

            // Primitive Classification
            //------------------------------------------------------------------------------------------
            // what parts to be classified?
            var partsForPC = BlockingDetermination.PartsTobeClassifiedIntoPrimitives(solids);
            SolidPrimitive = BlockingDetermination.PrimitiveMaker(partsForPC);
        }

        internal static void RunFastenerDetection(Dictionary<string, List<TessellatedSolid>> solids, int threaded)
        {
            PartsWithOneGeom = new List<TessellatedSolid>();
            foreach (var subAssem in solids.Values)
                if (subAssem.Count == 1)
                    PartsWithOneGeom.Add(subAssem[0]);
            // From repeated parts take only one of them:
            //------------------------------------------------------------------------------------------
            var multipleRefs = DuplicatePartsDetector(PartsWithOneGeom);

            // Detect fasteners
            //------------------------------------------------------------------------------------------
            FastenerDetector.Run(SolidPrimitive, multipleRefs, threaded, false);
        }

        internal static List<int> RunGraphGeneration(designGraph assemblyGraph, Dictionary<string, List<TessellatedSolid>> solidsNoFastener)
        {
            Solids = Program.Solids;
            solidsNoFastener = Program.SolidsNoFastener;
            //PrintOutSomeInitialStats();
            var globalDirPool = new List<int>();
            // Detect gear mates
            //------------------------------------------------------------------------------------------
            var gears = GearDetector.Run(PartsWithOneGeom, SolidPrimitive);
            var sw = new Stopwatch();
            sw.Start();

            // Add the solids as nodes to the graph. Exclude the fasteners 
            //------------------------------------------------------------------------------------------
            //DisassemblyDirections.Solids = new List<TessellatedSolid>(solidsNoFastener);
            AddingNodesToGraph(assemblyGraph, solidsNoFastener); //, gears, screwsAndBolts);

            // Implementing region octree for every solid
            //------------------------------------------------------------------------------------------
            PartitioningSolid.Partitions = new Dictionary<TessellatedSolid, Partition[]>();
            PartitioningSolid.PartitionsAABB = new Dictionary<TessellatedSolid, PartitionAABB[]>();
            PartitioningSolid.CreatePartitions(solidsNoFastener);

            // Part to part interaction to obtain removal directions between every connected pair
            //------------------------------------------------------------------------------------------
            //var s = Stopwatch.StartNew();
            //s.Start();
            //Console.WriteLine();
            //Console.WriteLine("Blocking Determination is running ....");
            //Bridge.StatusReporter.ReportStatusMessage("Generating the Liaison Graph - Adjacent Blocking Determination ...", 0.5f);
            //Bridge.StatusReporter.ReportProgress(0);
            BlockingDetermination.OverlappingSurfaces = new List<OverlappedSurfaces>();
            var solidNofastenerList = solidsNoFastener.ToList();
            //var totalCases = ((solidsNoFastener.Count - 1) * (solidsNoFastener.Count)) / 2.0;
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
            long counter = 0;
            foreach (var each in overlapCheck)
            //Parallel.ForEach(overlapCheck, each =>
            {
                var localDirInd = new List<int>();
                for (var t = 0; t < DisassemblyDirections.Directions.Count; t++)
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
                    var from = assemblyGraph[each[1].Key]; // Moving
                    var to = assemblyGraph[each[0].Key]; // Reference
                    assemblyGraph.addArc((node)from, (node)to, "", typeof(Connection));
                    var a = (Connection)assemblyGraph.arcs.Last();
                    a.Certainty = certainty;
                    AddInformationToArc(a, finDirs, infDirs);
                }
                //if (counter < totalTriTobeChecked)
                    //Bridge.StatusReporter.ReportProgress(counter / (float)totalTriTobeChecked);
            }//);
            //Bridge.StatusReporter.ReportProgress(1);
            //if (Bridge.SavedSessionIsStarted) return globalDirPool;
            Fastener.AddFastenersInformation(assemblyGraph, solidsNoFastener, SolidPrimitive);
            // create oppositeDirections for global direction pool.
            FindingOppositeDirectionsForGlobalPool(globalDirPool);
            //sw.Stop();
            // Simplify the solids, before doing anything
            //------------------------------------------------------------------------------------------
            //if (solidsNoFastener.Count == 20)
            {
                SimplifySolids(solidsNoFastener);
                // Implementing region octree for every solid
                //------------------------------------------------------------------------------------------
                PartitioningSolid.Partitions = new Dictionary<TessellatedSolid, Partition[]>();
                PartitioningSolid.PartitionsAABB = new Dictionary<TessellatedSolid, PartitionAABB[]>();
                PartitioningSolid.CreatePartitions(solidsNoFastener);
            }
            //s.Stop();
            //Console.WriteLine("Blocking Determination is done in:" + "     " + s.Elapsed);
            return globalDirPool;
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
                        k.Faces.Count() == solid.Faces.Count() &&
                        Math.Abs(k.Vertices.Count() - solid.Vertices.Count()) < 2 &&
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
                //Bridge.StatusReporter.ReportProgress((i + 1) / (float)(solids.Count));
            }
            return multipleRefs;
        }

        private static void AddInformationToArc(Connection a, List<int> finDirs, List<int> infDirs)
        {
            a.FiniteDirections.AddRange(finDirs);
            a.InfiniteDirections.AddRange(infDirs);
        }


        private static void SimplifySolids(Dictionary<string, List<TessellatedSolid>> subAssems)
        {
            foreach (var sa in subAssems)
            {
                //using (var fileStream = File.Create(sa.Key + ".amf"))
                //    IO.Save(fileStream, sa.Value, FileType.AMF);
                foreach (var ts in sa.Value)
                {
                    if (ts.Errors == null || ((ts.Errors.EdgesThatDoNotLinkBackToFace == null ||
                                               ts.Errors.EdgesThatDoNotLinkBackToFace.Count < 2) &&
                                              ts.Errors.SingledSidedEdges.Count < 5))
                    {
                        gCounter++;
                        //if (ts.Faces.Count() == 410) continue;
                        //var perc = (ts.Faces.Length - 1000)/(double)ts.Faces.Length;
                        ts.SimplifyByPercentage(0.7);
                        //ts.Repair();
                        //continue;
                    }
                    //if (ts.Faces.Length <= 800) continue;
                    //ts.SimplifyByPercentage(0.5);
                    //ts.Repair();
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
            DisassemblyDirections.DirectionsAndOpposits = new Dictionary<int, int>();
            for (int i = 0; i < Directions.Count; i++)
            {
                var dir = Directions[i];
                var oppos = Directions.First(d => d[0] == -dir[0] && d[1] == -dir[1] && d[2] == -dir[2]);
                DisassemblyDirections.DirectionsAndOpposits.Add(i, Directions.IndexOf(oppos));
            }
        }


        internal static void FindingOppositeDirectionsForGlobalPool(List<int> globalDirPool)
        {
            DisassemblyDirections.DirectionsAndOppositsForGlobalpool = new Dictionary<int, int>();
            var toBeAddedToGDir = new List<int>();
            for (int i = 0; i < globalDirPool.Count; i++)
            {
                var dir = Directions[globalDirPool[i]];
                var temp =
                    globalDirPool.Where(
                        d =>
                            Math.Abs(1 +
                            DisassemblyDirections.Directions[d].dotProduct(
                                DisassemblyDirections.Directions[globalDirPool[i]])) < 0.01).ToList();
                if (temp.Any())
                    DisassemblyDirections.DirectionsAndOppositsForGlobalpool.Add(globalDirPool[i], temp[0]);
                else
                {
                    DisassemblyDirections.Directions.Add(dir.multiply(-1));
                    DisassemblyDirections.DirectionsAndOppositsForGlobalpool.Add(globalDirPool[i], DisassemblyDirections.Directions.Count - 1);
                    toBeAddedToGDir.Add(DisassemblyDirections.Directions.Count - 1);
                }
            }
            foreach (var newD in toBeAddedToGDir)
            {
                globalDirPool.Add(newD);
                var key = DisassemblyDirections.DirectionsAndOppositsForGlobalpool.Where(k => k.Value == newD).ToList();
                DisassemblyDirections.DirectionsAndOppositsForGlobalpool.Add(newD, key[0].Key);
            }
        }
    }
}
