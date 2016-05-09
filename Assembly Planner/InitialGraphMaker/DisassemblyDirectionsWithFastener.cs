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
        internal static Dictionary<string, List<TessellatedSolid>> Solids;
        internal static Dictionary<string, List<TessellatedSolid>> SolidsNoFastener;
        internal static Dictionary<int, List<Component[]>> NonAdjacentBlocking = new Dictionary<int, List<Component[]>>(); //Component[0] is blocked by Component[1]
        internal static Dictionary<TessellatedSolid, List<PrimitiveSurface>> SolidPrimitive =
            new Dictionary<TessellatedSolid, List<PrimitiveSurface>>();
        internal static List<TessellatedSolid> PartsWithOneGeom;

        internal static List<int> Run(designGraph assemblyGraph, Dictionary<string, List<TessellatedSolid>> solids,
            bool classifyFastener = false, bool threaded = false)
        {
            Solids = new Dictionary<string, List<TessellatedSolid>>(solids);

            // Generate a good number of directions on the surface of a sphere
            //------------------------------------------------------------------------------------------
            Directions = IcosahedronPro.DirectionGeneration(1);
            DisassemblyDirections.Directions = new List<double[]>(Directions);
            FindingOppositeDirections();
            SpringDetector.DetectSprings(solids);
            var globalDirPool = new List<int>();
            //SpringDetector.DetectSprings(solids);
            //playWithOBB(solids);
            // Creating Bounding Geometries for every solid
            //------------------------------------------------------------------------------------------
            BoundingGeometry.OrientedBoundingBoxDic = new Dictionary<TessellatedSolid, BoundingBox>();
            BoundingGeometry.BoundingCylinderDic = new Dictionary<TessellatedSolid, BoundingCylinder>();
            BoundingGeometry.CreateOBB2(solids);
            BoundingGeometry.CreateBoundingCylinder(solids);

            // From repeated parts take only one of them, and do the primitive classification on that:
            //------------------------------------------------------------------------------------------
            var partsForPC = BlockingDetermination.PartsTobeClassifiedIntoPrimitives(solids);
            SolidPrimitive = BlockingDetermination.PrimitiveMaker(partsForPC);

            PartsWithOneGeom = new List<TessellatedSolid>();
            foreach (var subAssem in solids.Values)
                if (subAssem.Count == 1)
                    PartsWithOneGeom.Add(subAssem[0]);
            var multipleRefs = DuplicatePartsDetector(PartsWithOneGeom);

            // Detect fasteners
            //------------------------------------------------------------------------------------------
            var screwsAndBolts = new HashSet<TessellatedSolid>();
            if (classifyFastener)
            {
                screwsAndBolts = FastenerDetector.Run(SolidPrimitive, multipleRefs,true, threaded,false);
                screwsAndBolts = FastenerDetector.CheckFastenersWithUser(screwsAndBolts);
            }
            SolidsNoFastener = RemoveFastenersFromTheSolidsList(solids, screwsAndBolts);

            //// Detect gear mates
            ////------------------------------------------------------------------------------------------
            var gears = GearDetector.Run(PartsWithOneGeom, SolidPrimitive);


            // Add the solids as nodes to the graph. Exclude the fasteners 
            //------------------------------------------------------------------------------------------
            AddingNodesToGraph(assemblyGraph, SolidsNoFastener); //, gears, screwsAndBolts);

            // Implementing region octree for every solid
            //------------------------------------------------------------------------------------------
            PartitioningSolid.Partitions = new Dictionary<TessellatedSolid, Partition[]>();
            PartitioningSolid.PartitionsAABB = new Dictionary<TessellatedSolid, PartitionAABB[]>();
            PartitioningSolid.CreatePartitions(SolidsNoFastener);

            // Part to part interaction to obtain removal directions between every connected pair
            //------------------------------------------------------------------------------------------
            var s = Stopwatch.StartNew();
            s.Start();
            Console.WriteLine();
            Console.WriteLine("Blocking Determination is running ....");
            BlockingDetermination.OverlappingSurfaces = new List<OverlappedSurfaces>();
            var solidNofastenerList = SolidsNoFastener.ToList();
            //var totalCases = ((solidsNoFastener.Count - 1) * (solidsNoFastener.Count)) / 2.0;
            long totalTriTobeChecked = 0;
            var overlapCheck = new HashSet<KeyValuePair<string, List<TessellatedSolid>>[]>();
            for (var i = 0; i < SolidsNoFastener.Count - 1; i++)
            {
                var subAssem1 = solidNofastenerList[i];
                for (var j = i + 1; j < SolidsNoFastener.Count; j++)
                {
                    var subAssem2 = solidNofastenerList[j];
                    overlapCheck.Add(new[] { subAssem1, subAssem2 });
                    var tri2Sub1 = subAssem1.Value.Sum(s2 => s2.Faces.Length);
                    var tri2Sub2 = subAssem2.Value.Sum(s2 => s2.Faces.Length);
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
                if (counter < totalTriTobeChecked)
                    Bridge.StatusReporter.ReportProgress(counter / (float)totalTriTobeChecked);
            }//);
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


        private static Dictionary<string, List<TessellatedSolid>> RemoveFastenersFromTheSolidsList(
            Dictionary<string, List<TessellatedSolid>> solids, HashSet<TessellatedSolid> screwsAndBolts)
        {
            var solidsNoFastener = new Dictionary<string, List<TessellatedSolid>>(solids);
            foreach (var solid in solids)
            {
                if (solid.Value.Count > 1)
                {
                    solidsNoFastener.Add(solid.Key, solid.Value);
                    continue;
                }
                if (screwsAndBolts.Any(f=> f.Name == solid.Key)) continue;
                solidsNoFastener.Add(solid.Key, solid.Value);
            }
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

    }
}
