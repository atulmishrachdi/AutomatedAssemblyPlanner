using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Assembly_Planner;
using Assembly_Planner.GraphSynth.BaseClasses;
using GPprocess;
using GraphSynth.Representation;
using StarMathLib;
using TVGL;
using TVGL.IOFunctions;

namespace Assembly_Planner
{
    public class Program
    {

        public static List<double> DegreeOfFreedoms = new List<double>();
        public static List<double> StablbiblityScores = new List<double>();
        public static Dictionary<string, List<TessellatedSolid>> Solids = new Dictionary<string, List<TessellatedSolid>>();
        public static Dictionary<string, List<TessellatedSolid>> SolidsNoFastener = new Dictionary<string, List<TessellatedSolid>>();
        public static Dictionary<string, List<TessellatedSolid>> SolidsNoFastenerSimplified = new Dictionary<string, List<TessellatedSolid>>();
        public static Dictionary<string, List<TessellatedSolid>> SimplifiedSolids = new Dictionary<string, List<TessellatedSolid>>();
        public static Dictionary<string, double> SolidsMass = new Dictionary<string, double>();
        public static designGraph AssemblyGraph;
        public static double StabilityWeightChosenByUser = 0;
        public static double UncertaintyWeightChosenByUser = 0;
        public static double MeshMagnifier = 1;
        public static double[] PointInMagicBox = { 0, 0, 0.0 };
        public static int BeamWidth;
        public static bool DetectFasteners = true; 
        public static int AvailableWorkers = 0;
        public static int FastenersAreThreaded = 0;
        public static double StabilityScore = 0;
        public static bool RobustSolution = false;
        public static List<int> globalDirPool = new List<int>();
        public static List<double> allmtime = new List<double>();
        public static List<double> allitime = new List<double>();
        public static List<double> gpmovingtime = new List<double>();
        public static List<double> gpinstalltime = new List<double>();
        public static List<double> gpsecuretime = new List<double>();
        public static List<double> gprotate = new List<double>();
        public static ProgramState state;

        private static void Main(string[] args)
        {
            state = new ProgramState();
            SetInputArguments(state, args);
            LoadState();
            Solids = GetSTLs(state.inputDir);
            EnlargeTheSolid();

            AssemblyGraph = new designGraph();
            DisassemblyDirectionsWithFastener.RunGeometricReasoning(Solids);
            if (DetectFasteners)
                DisassemblyDirectionsWithFastener.RunFastenerDetection(Solids, FastenersAreThreaded);
            //SolidsNoFastener = Solids;
            SerializeSolidProperties();
            Console.WriteLine("\nPress enter once input parts table generated >>");


            Console.ReadLine();
            DeserializeSolidProperties();
            globalDirPool = DisassemblyDirectionsWithFastener.RunGraphGeneration(AssemblyGraph, SolidsNoFastener);
            //the second user interaction must happen here
            SaveDirections();
            var connectedGraph = false;
            while (!connectedGraph)
            {
                Console.WriteLine("\n\nPress enter once input directions generated >>");
                Console.ReadLine();
                LoadDirections();
                connectedGraph = DisassemblyDirectionsWithFastener.GraphIsConnected(AssemblyGraph);
            }


            NonadjacentBlockingWithPartitioning.Run(AssemblyGraph, SolidsNoFastenerSimplified, globalDirPool);
            GraphSaving.SaveTheGraph(AssemblyGraph);
            Stabilityfunctions.GenerateReactionForceInfo(AssemblyGraph);
            var leapSearch = new LeapSearch();
            var solutions = leapSearch.Run(AssemblyGraph, Solids, globalDirPool);
            OptimalOrientation.Run(solutions);
            var cand = new AssemblyCandidate() { Sequence = solutions };
            cand.SaveToDisk(state.inputDir + "solution.xml");
            WorkerAllocation.Run(solutions);
            Console.WriteLine("\n\nDone");
            Console.ReadLine();
        }


        public static void doFastenerDetection(string[] args)
        {
            state = new ProgramState();
            SetInputArguments(state, args);
            LoadState();

            Solids = GetSTLs(state.inputDir);
            EnlargeTheSolid();
            AssemblyGraph = new designGraph();
            DisassemblyDirectionsWithFastener.RunGeometricReasoning(Solids);
            if (DetectFasteners)
            {
                DisassemblyDirectionsWithFastener.RunFastenerDetection(Solids, FastenersAreThreaded);
            }
            SerializeSolidProperties();

            SaveState();
            state.Save(state.inputDir + "/intermediate/ProgramState.xml");
            Console.WriteLine("\nDone");

        }

        public static void doDisassemblyDirections()
        {
            state = new ProgramState();
            ProgramState.Load("./bin/intermediate/ProgramState.xml", ref state);
            LoadState();

            DeserializeSolidProperties();
            globalDirPool = DisassemblyDirectionsWithFastener.RunGraphGeneration(AssemblyGraph, SolidsNoFastener);
            //the second user interaction must happen here
            SaveDirections();
            var connectedGraph = false;
			string dummyString = "";
            while (!connectedGraph)
            {
                Console.WriteLine("\n\nPress enter once input directions generated >>");
				dummyString = Console.ReadLine();
				Console.WriteLine("\n\nChecking connectedness...");
                LoadDirections();
                connectedGraph = DisassemblyDirectionsWithFastener.GraphIsConnected(AssemblyGraph);
            }
			Console.WriteLine("\n\nConnectedness verified");

            SaveState();
            state.Save(state.inputDir + "/intermediate/ProgramState.xml");
            Console.WriteLine("\nDone");

        }


        public static void doAssemblyPlanning()
        {
            state = new ProgramState();
            ProgramState.Load("./bin/intermediate/ProgramState.xml", ref state);
            LoadState();

            NonadjacentBlockingWithPartitioning.Run(AssemblyGraph, SolidsNoFastenerSimplified, globalDirPool);
            GraphSaving.SaveTheGraph(AssemblyGraph);
            Stabilityfunctions.GenerateReactionForceInfo(AssemblyGraph);
            var leapSearch = new LeapSearch();
            var solutions = leapSearch.Run(AssemblyGraph, Solids, globalDirPool);
            OptimalOrientation.Run(solutions);
            var cand = new AssemblyCandidate() { Sequence = solutions };
            cand.SaveToDisk(state.inputDir + "/XML/solution.xml");
            WorkerAllocation.Run(solutions);

            SaveState();
            state.Save(state.inputDir + "/intermediate/ProgramState.xml");
            Console.WriteLine("\n\nDone");

        }




        public static void LoadState()
        {

            DegreeOfFreedoms = state.DegreeOfFreedoms;
            StablbiblityScores = state.StablbiblityScores;
            Solids = state.Solids;
            SolidsNoFastener = state.SolidsNoFastener;
            SolidsNoFastenerSimplified = state.SolidsNoFastenerSimplified;
            SimplifiedSolids = state.SimplifiedSolids;
            SolidsMass = state.SolidsMass;
            AssemblyGraph = state.AssemblyGraph;
            StabilityWeightChosenByUser = state.StabilityWeightChosenByUser;
            UncertaintyWeightChosenByUser = state.UncertaintyWeightChosenByUser;
            MeshMagnifier = state.MeshMagnifier;
            PointInMagicBox = state.PointInMagicBox;
            BeamWidth = state.BeamWidth;
            DetectFasteners = state.DetectFasteners;
            AvailableWorkers = state.AvailableWorkers;
            FastenersAreThreaded = state.FastenersAreThreaded;
            StabilityScore = state.StabilityScore;
            RobustSolution = state.RobustSolution;
            globalDirPool = state.globalDirPool;
            allmtime = state.allmtime;
            allitime = state.allitime;
            gpmovingtime = state.gpmovingtime;
            gpinstalltime = state.gpinstalltime;
            gpsecuretime = state.gpsecuretime;
            gprotate = state.gprotate;

        }

        public static void SaveState()
        {

            state.DegreeOfFreedoms = DegreeOfFreedoms;
            state.StablbiblityScores = StablbiblityScores;
            state.Solids = Solids;
            state.SolidsNoFastener = SolidsNoFastener;
            state.SolidsNoFastenerSimplified = SolidsNoFastenerSimplified;
            state.SimplifiedSolids = SimplifiedSolids;
            state.SolidsMass = SolidsMass;
            state.AssemblyGraph = AssemblyGraph;
            state.StabilityWeightChosenByUser = StabilityWeightChosenByUser;
            state.UncertaintyWeightChosenByUser = UncertaintyWeightChosenByUser;
            state.MeshMagnifier = MeshMagnifier;
            state.PointInMagicBox = PointInMagicBox;
            state.BeamWidth = BeamWidth;
            state.DetectFasteners = DetectFasteners;
            state.AvailableWorkers = AvailableWorkers;
            state.FastenersAreThreaded = FastenersAreThreaded;
            state.StabilityScore = StabilityScore;
            state.RobustSolution = RobustSolution;
            state.globalDirPool = globalDirPool;
            state.allmtime = allmtime;
            state.allitime = allitime;
            state.gpmovingtime = gpmovingtime;
            state.gpinstalltime = gpinstalltime;
            state.gpsecuretime = gpsecuretime;
            state.gprotate = gprotate;

        }



        private static void SetInputArguments(ProgramState state, string[] args)
        {
            var argsIndex = 0;
            if (!args.Any())
            {
                Console.WriteLine("No arguments provided. Using default values.");
                SetInputArguments(state, Constants.DefaultInputArguments);
                return;
            }
            if (args[argsIndex].Equals("dialog", StringComparison.CurrentCultureIgnoreCase))
                SetInputArgumentsViaDialog(state);
            else
            {
                state.inputDir = args[argsIndex];
                if (args.Length > ++argsIndex)
                    DetectFasteners = args[argsIndex].Equals("y", StringComparison.CurrentCultureIgnoreCase);
                if (DetectFasteners && args.Length > ++argsIndex)
                    FastenersAreThreaded = int.Parse(args[argsIndex]);
                if (args.Length > ++argsIndex)
                    StabilityScore = double.Parse(args[argsIndex]);
                if (args.Length > ++argsIndex)
                    RobustSolution = args[argsIndex].Equals("y", StringComparison.CurrentCultureIgnoreCase);
            }
        }

        private static void SetInputArgumentsViaDialog(ProgramState state)
        {
            var autoFastenersDetect = "m";
            while (autoFastenersDetect != "y" && autoFastenersDetect != "n" && autoFastenersDetect != "Y" &&
                   autoFastenersDetect != "N")
            {
                Console.WriteLine("Do you want the AAP tool to automatically detect the fasteners? (y/n)");
                autoFastenersDetect = Console.ReadLine();
            }
            if (autoFastenersDetect == "y" || autoFastenersDetect == "Y")
            {
                DetectFasteners = true;
                var threaded = 5;
                while (threaded != 0 && threaded != 1 && threaded != 2)
                {
                    Console.WriteLine("Are the fasteners of the input model threaded? (0: none, 1: all, 2: subset)");
                    threaded = Convert.ToInt32(Console.ReadLine());
                }
                FastenersAreThreaded = threaded;
            }
            else DetectFasteners = false;
            var stabilityScore = -1.0;
            while (stabilityScore > 1 || stabilityScore < 0)
            {
                Console.WriteLine("What is the desired level of fixturing of the final assembly plan?" +
                                  "\n   - Please enter a number between 0 (high level of fixturing) and " +
                                  "\n     1 (low level of fixturing)");
                stabilityScore = Convert.ToDouble(Console.ReadLine());
            }
            StabilityScore = stabilityScore;
            var robustSolution = "m";
            while (robustSolution != "y" && robustSolution != "n" && robustSolution != "Y" &&
                   robustSolution != "N")
            {
                Console.WriteLine("Do you want a robust final solution? (y/n)");
                robustSolution = Console.ReadLine();
            }
            RobustSolution = false;
            if (robustSolution == "Y" || robustSolution == "y")
                RobustSolution = true;
        }

        private static void SerializeSolidProperties()
        {
            XmlSerializer ser = new XmlSerializer(typeof(PartsProperties));
            var partsProperties = new PartsProperties();
            partsProperties.GenerateProperties();
            var writer = new StreamWriter(state.inputDir + "/XML/parts_properties.xml");
            ser.Serialize(writer, partsProperties);
        }

        private static void DeserializeSolidProperties()
        {
            XmlSerializer ser = new XmlSerializer(typeof(PartsProperties));
            var reader = new StreamReader(state.inputDir + "/XML/parts_properties2.xml");
            var partsProperties = (PartsProperties)ser.Deserialize(reader);
            //now update everything with the revised properties
            UpdateSolidsProperties(partsProperties);
            UpdateFasteners(partsProperties);
        }

        private static void UpdateSolidsProperties(PartsProperties partsProperties)
        {
            foreach (var solidName in Solids.Keys)
            {
                var userUpdated = partsProperties.parts.First(p => p.Name == solidName);
                SolidsMass.Add(solidName,
                    userUpdated.Mass > 0
                        ? userUpdated.Mass * Math.Pow(MeshMagnifier, 3)
                        : userUpdated.Volume * Math.Pow(MeshMagnifier, 3));
            }
        }

        internal static void SaveDirections()
        {

            XmlSerializer ser = new XmlSerializer(typeof(DirectionSaveStructure));
            var writer = new StreamWriter(state.inputDir + "/XML/directionList.xml");
            var theData = new DirectionSaveStructure();
            theData.arcs = AssemblyGraph.arcs;
            theData.Directions = DisassemblyDirectionsWithFastener.Directions;
            ser.Serialize(writer, theData);

        }

        internal static void LoadDirections()
        {
            XmlSerializer ser = new XmlSerializer(typeof(DirectionSaveStructure));
            var reader = new StreamReader(state.inputDir + "/XML/directionList2.xml");
            var theData = (DirectionSaveStructure)ser.Deserialize(reader);
            var reviewedArc = theData.arcs;
            UpdateGraphArcs(reviewedArc);
        }

        private static void UpdateGraphArcs(List<arc> reviewedArc)
        {
            foreach (Connection arc in reviewedArc)
            {

				//$ Incorperated the ability to add arcs, because otherwise resolving incomplete graphs
				//  would be impossible
				var counterpart =(Connection)
                    AssemblyGraph.arcs.FirstOrDefault(c => c.XmlFrom == arc.XmlFrom && c.XmlTo == arc.XmlTo);
				if (counterpart == null) {
					AssemblyGraph.addArc(AssemblyGraph.nodes.First(a => a.name == arc.XmlFrom),
						AssemblyGraph.nodes.First(a => a.name == arc.XmlTo),"",typeof(Connection));
					counterpart = (Connection)AssemblyGraph.arcs.Last ();
				} else {
					if (arc.Certainty == 0) {
						AssemblyGraph.removeArc(counterpart);
					}
				}
				counterpart.FiniteDirections = AddDirections (arc.FiniteDirections);
				counterpart.InfiniteDirections = AddDirections (arc.InfiniteDirections);
            }
        }

        private static void UpdateFasteners(PartsProperties partsProperties)
        {
            foreach (var solidName in Solids.Keys)
            {
                var userUpdated = partsProperties.parts.First(p => p.Name == solidName);
                if (userUpdated.FastenerCertainty < 1)
                    SolidsNoFastener.Add(solidName, Solids[solidName]);
            }
            FastenerDetector.Fasteners =
                new HashSet<Fastener>(
                    FastenerDetector.Fasteners.Where(f => !SolidsNoFastener.Keys.Contains(f.Solid.Name)).ToList());
            FastenerDetector.Nuts =
                new HashSet<Nut>(
                    FastenerDetector.Nuts.Where(n => !SolidsNoFastener.Keys.Contains(n.Solid.Name)).ToList());
            FastenerDetector.Washers =
                new HashSet<Washer>(
                    FastenerDetector.Washers.Where(w => !SolidsNoFastener.Keys.Contains(w.Solid.Name)).ToList());
            var pF =
                FastenerDetector.PotentialFastener.Where(u => !SolidsNoFastener.Keys.Contains(u.Key.Name))
                    .ToDictionary(t => t.Key, t => t.Value);
            FastenerDetector.PotentialFastener =
                new Dictionary<TessellatedSolid, double>(pF);
        }

        private static Dictionary<string, List<TessellatedSolid>> GetSTLs(string InputDir)
        {
            Console.WriteLine("\nLoading STLs ....\n");
            var parts = new List<TessellatedSolid>();
            var di = new DirectoryInfo(InputDir + "/models");
            var fis = di.EnumerateFiles("*");
            // Parallel.ForEach(fis, fileInfo =>
            var i = 0;
            foreach (var fileInfo in fis)
            {
                // debug: does this work? does extension include "." or capitals?
                if (!Constants.ValidShapeFileTypes.Any(s =>
                s.Equals(fileInfo.Extension, StringComparison.CurrentCultureIgnoreCase)))
                    continue;

                var ts = IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name);
                //ts.Name = ts.Name.Remove(0, 1);
                //lock (parts) 
                if (ts[0].Faces.Length > 50000 &&
                    (ts[0].Errors == null || ((ts[0].Errors.EdgesThatDoNotLinkBackToFace == null ||
                                               ts[0].Errors.EdgesThatDoNotLinkBackToFace.Count < 2) &&
                                              (ts[0].Errors.SingledSidedEdges == null ||
                                               ts[0].Errors.SingledSidedEdges.Count < 5))))
                {
                    try
                    {
                        ts[0].Simplify(ts[0].NumberOfFaces / 2);
                    }
                    catch (Exception)
                    {
                        //continue;
                    }
                }
                parts.Add(ts[0]);
                i++;
            }
            //);
            Console.WriteLine("All the files are loaded successfully");
            Console.WriteLine("    * Number of tessellated solids:   " + parts.Count);
            Console.WriteLine("    * Total Number of Triangles:   " + parts.Sum(s => s.Faces.Count()));
            return parts.ToDictionary(tessellatedSolid => tessellatedSolid.Name, tessellatedSolid => new List<TessellatedSolid> { tessellatedSolid });
        }

        private static List<int> AddDirections(List<int> reviewedDirections)
        {

			//$ Filling DisassemblyDirections 
			//DisassemblyDirections.Directions;
			//


            var dirInds = new List<int>();
            if (reviewedDirections == null) return dirInds;
            var toBeAddedToGDir = new List<int>();
            foreach (var dir in reviewedDirections)
            {
                dirInds.Add(dir);
                if (!globalDirPool.Contains(dir))
                {
					//$ Added to check for invalid values. Remove later 
					if (dir < 0 || dir > DisassemblyDirections.Directions.Count) {
						Console.Write ("\n");
						Console.Write (dir);
						Console.Write (" - ");
						Console.Write (DisassemblyDirections.Directions.Count);
					}
					//

                    globalDirPool.Add(dir);
                    var temp =
                        globalDirPool.Where(
                            d =>
                                Math.Abs(1 +
                                         DisassemblyDirections.Directions[d].dotProduct(
                                             DisassemblyDirections.Directions[dir])) < 0.01).ToList();
                    if (temp.Any())
                        DisassemblyDirections.DirectionsAndOppositsForGlobalpool.Add(dir, temp[0]);
                    else
                    {
                        var dir2 = DisassemblyDirections.Directions[dir];
                        DisassemblyDirections.Directions.Add(dir2.multiply(-1));
                        DisassemblyDirections.DirectionsAndOppositsForGlobalpool.Add(dir, DisassemblyDirections.Directions.Count - 1);
                        toBeAddedToGDir.Add(DisassemblyDirections.Directions.Count - 1);
                    }
                }
            }
            foreach (var newD in toBeAddedToGDir)
            {
                globalDirPool.Add(newD);
                var key = DisassemblyDirections.DirectionsAndOppositsForGlobalpool.Where(k => k.Value == newD).ToList();
                DisassemblyDirections.DirectionsAndOppositsForGlobalpool.Add(newD, key[0].Key);
            }
            return dirInds;
        }

        private static void EnlargeTheSolid()
        {
            Console.WriteLine("\nScaling the parts ....\n");
            MeshMagnifier = DetermineTheMagnifier();
            var solidsMagnified = new Dictionary<string, List<TessellatedSolid>>();
            Parallel.ForEach(Solids, dic =>
            //foreach (var dic in Solids)
            {
                var solids = new List<TessellatedSolid>();
                var solidsConstant = new List<TessellatedSolid>();
                foreach (var ts in dic.Value)
                {
                    var newVer = ts.Vertices.Select(vertex => new TempVer { Ver = vertex, IndexInList = 0 }).ToList();
                    var newFace =
                        ts.Faces.Select(
                            face =>
                                new TempFace
                                {
                                    Face = face,
                                    Vers = face.Vertices.Select(v => newVer.First(nv => nv.Ver == v)).ToArray()
                                })
                            .ToList();
                    var tvglVertices = new List<Vertex>();
                    var tvglFaces = new List<PolygonalFace>();
                    for (var i = 0; i < newVer.Count; i++)
                    {
                        var v = newVer[i];
                        //var vTransformed = MultiplyByTrans(vapPart.OriginalTransformation, v);
                        v.IndexInList = i;
                        tvglVertices.Add(new Vertex(v.Ver.Position.multiply(MeshMagnifier)));
                    }
                    foreach (var tri in newFace)
                    {
                        tvglFaces.Add(
                            new PolygonalFace(tri.Vers.Select(v => tvglVertices[v.IndexInList]).ToList(), null));
                    }
                    var tsM = new TessellatedSolid(tvglFaces, tvglVertices, null, UnitType.millimeter, ts.Name);
                    var tsC = new TessellatedSolid(tvglFaces, tvglVertices, null, UnitType.millimeter, ts.Name);
                    tsM.Repair();
                    tsC.Repair();
                    solids.Add(tsM);
                    solidsConstant.Add(tsC);
                }
                lock (solidsMagnified)
                    solidsMagnified.Add(solids[0].Name, solids);
                lock (SimplifiedSolids)
                    SimplifiedSolids.Add(solidsConstant[0].Name, solidsConstant);
            }
                );
            Solids = solidsMagnified;
        }

        private static double DetermineTheMagnifier()
        {
            // Regardless of the actual size of the assembly, I will fit it in a box with
            // a diagonal length of 500,000
            var allVertices = Solids.SelectMany(p => p.Value).SelectMany(g => g.Vertices);
            var maxX = (double)allVertices.Max(v => v.X);
            var maxY = (double)allVertices.Max(v => v.Y);
            var maxZ = (double)allVertices.Max(v => v.Z);

            var minX = (double)allVertices.Min(v => v.X);
            var minY = (double)allVertices.Min(v => v.Y);
            var minZ = (double)allVertices.Min(v => v.Z);

            var diagonalLength = GeometryFunctions.DistanceBetweenTwoVertices(new[] { maxX, maxY, maxZ },
                new[] { minX, minY, minZ });
            return 500000 / diagonalLength;
        }
    }

    class TempVer
    {
        internal Vertex Ver { get; set; }
        internal int IndexInList;
    }
    class TempFace
    {
        internal PolygonalFace Face { get; set; }
        internal TempVer[] Vers;
    }
}

