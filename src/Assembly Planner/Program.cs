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
using BaseClasses;
using BaseClasses.Representation;
using Fastener_Detection;
using Geometric_Reasoning;
using GPprocess;
using StarMathLib;
using TVGL;
using TVGL.IOFunctions;

namespace Assembly_Planner
{
    internal class Program
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
        public static double[] PointInMagicBox = {0,0,0.0};
        public static int BeamWidth;
        protected static bool DetectFasteners = true;
        protected internal static int AvailableWorkers = 0;
        protected static int FastenersAreThreaded = 0; // 0: none, 1: all, 2: subset
        public static double StabilityScore = 0;
        public static bool RobustSolution = false;
        public static List<int> globalDirPool = new List<int>();
        public static List<double> allmtime = new List<double>();
        public static List<double> allitime = new List<double>();
        public static List<double> gpmovingtime = new List<double>();
        public static List<double> gpinstalltime = new List<double>();
        public static List<double> gpsecuretime = new List<double>();
        public static List<double> gprotate = new List<double>();

        private static void Main(string[] args)
        {
            InititalConfigurations();
            string inputDir;
#if InputDialog
             inputDir = consoleFrontEnd.getPartsDirectory();
#else
            inputDir = "workspace";
            var ss = //Directory.GetCurrentDirectory();
            "src/Test/PumpWExtention";

#endif
            Solids = GetSTLs(inputDir);
            EnlargeTheSolid();

            AssemblyGraph = new designGraph();
            Process.Start("Geometric_Reasoning.exe");
            if (DetectFasteners)
                Process.Start("Fastener_Detection.exe");
            Process.Start("Graph_Generation.exe");
            Process.Start("Plan_Generation.exe");
            DisassemblyDirectionsWithFastener.RunGeometricReasoning(Solids);
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
            cand.SaveToDisk(Directory.GetCurrentDirectory() + "\\workspace\\solution.xml");
            WorkerAllocation.Run(solutions);
            Console.WriteLine("\n\nDone");
            Console.ReadLine();
        }

        private static void InititalConfigurations()
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
            var writer = new StreamWriter("workspace/parts_properties.xml");
            ser.Serialize(writer, partsProperties);
        }

        private static void DeserializeSolidProperties()
        {
            XmlSerializer ser = new XmlSerializer(typeof(PartsProperties));
            var reader = new StreamReader("workspace/parts_properties2.xml");
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
                        ? userUpdated.Mass*Math.Pow(MeshMagnifier, 3)
                        : userUpdated.Volume*Math.Pow(MeshMagnifier, 3));
            }
        }

        internal static void SaveDirections()
        {

            XmlSerializer ser = new XmlSerializer(typeof(DirectionSaveStructure));
            var writer = new StreamWriter("workspace/directionList.xml");
            var theData = new DirectionSaveStructure();
            theData.arcs = AssemblyGraph.arcs;
            theData.Directions = DisassemblyDirectionsWithFastener.Directions;
            ser.Serialize(writer, theData);

        }

        internal static void LoadDirections()
        {
            XmlSerializer ser = new XmlSerializer(typeof(DirectionSaveStructure));
            var reader = new StreamReader("workspace/directionList2.xml");
            var theData = (DirectionSaveStructure)ser.Deserialize(reader);
            var reviewedArc = theData.arcs;
            UpdateGraphArcs(reviewedArc);
        }

        private static void UpdateGraphArcs(List<arc> reviewedArc)
        {
            foreach (Connection arc in reviewedArc)
            {
                var counterpart =
                    AssemblyGraph.arcs.Cast<Connection>().First(c => c.XmlFrom == arc.XmlFrom && c.XmlTo == arc.XmlTo);
                if (arc.Certainty == 0)
                    AssemblyGraph.arcs.Remove(counterpart);
                counterpart.FiniteDirections = AddDirections(arc.FiniteDirections);
                counterpart.InfiniteDirections = AddDirections(arc.InfiniteDirections);
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
            Console.WriteLine("\nLoading STLs ....");
            var parts = new List<TessellatedSolid>();
            var di = new DirectoryInfo(InputDir);
            var fis = di.EnumerateFiles("*.STL");
            // Parallel.ForEach(fis, fileInfo =>
            var i = 0;
            foreach (var fileInfo in fis)
            {
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
                        ts[0].SimplifyByPercentage(0.5);
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
            return parts.ToDictionary(tessellatedSolid => tessellatedSolid.FileName, tessellatedSolid => new List<TessellatedSolid> { tessellatedSolid });
        }

        private static List<int> AddDirections(List<int> reviewedDirections)
        {
            var dirInds = new List<int>();
            if (reviewedDirections == null) return dirInds;
            var toBeAddedToGDir = new List<int>();
            foreach (var dir in reviewedDirections)
            {
                dirInds.Add(dir);
                if (!globalDirPool.Contains(dir))
                {
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
            Console.WriteLine("\nScaling the parts ....");
            MeshMagnifier = DetermineTheMagnifier();
            var solidsMagnified = new Dictionary<string, List<TessellatedSolid>>();
            Parallel.ForEach(Solids, dic =>
                //foreach (var dic in Solids)
            {
                var solids = new List<TessellatedSolid>();
                var solidsConstant = new List<TessellatedSolid>();
                foreach (var ts in dic.Value)
                {
                    var newVer = ts.Vertices.Select(vertex => new TempVer {Ver = vertex, IndexInList = 0}).ToList();
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

