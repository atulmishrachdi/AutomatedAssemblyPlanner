using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AssemblyEvaluation;
using Assembly_Planner;
using Assembly_Planner.GraphSynth.BaseClasses;
using GraphSynth.Representation;
using StarMathLib;
using TVGL;
using TVGL.IOFunctions;
using TVGL;

namespace Assembly_Planner
{
    internal class Program
    {
        public static List<double> DegreeOfFreedoms = new List<double>();
        public static List<double> StablbiblityScores = new List<double>();
        public static Dictionary<string, List<TessellatedSolid>> Solids = new Dictionary<string, List<TessellatedSolid>>();
        public static Dictionary<string, List<TessellatedSolid>> SolidsNoFastener = new Dictionary<string, List<TessellatedSolid>>();
        public static Dictionary<string, double> SolidsMass = new Dictionary<string, double>();
        public static designGraph AssemblyGraph;
        public static double StabilityWeightChosenByUser = 0;
        public const double MeshMagnifier = 1;
        public static double[] PointInMagicBox = {0,0,0.0};
        public static int BeamWidth;

        public static List<int> globalDirPool = new List<int>();
        public static List<double> allmtime = new List<double>();
        public static List<double> allitime = new List<double>();
        private static void Main(string[] args)
        {

            string inputDir;
#if InputDialog
             inputDir = consoleFrontEnd.getPartsDirectory();
#else
            inputDir =
            "src/Test/PumpWExtention";
            //"src/Test/Cube";
            //"src/Test/TXT-Binary";
            //"src/Test/FastenerTest/new/test";
            //"src/Test/Double";
            //"src/Test/test7";
            //"src/Test/FPM2";
            //"src/Test/Mc Cormik/STL2";
            //"src/Test/Truck -TXT-1/STL";
            //"src/Test/FoodPackagingMachine/FPMSTL2";
            //"src/Test/test8";
#endif
            Solids = GetSTLs(inputDir);
            var detectFasteners = true; //TBI
            var threaded = 0; // 0:none, 1: all, 2: subset

            AssemblyGraph = new designGraph();
            DisassemblyDirectionsWithFastener.RunGeometricReasoning(Solids);
            if (detectFasteners)
            {
                DisassemblyDirectionsWithFastener.RunFastenerDetection(Solids, threaded);
            }
            else
            {
                SolidsNoFastener = Solids;
            }
            SerializeSolidProperties();
            Console.WriteLine("Press enter once input parts table generated >>");
            Console.ReadLine();
            DeserializeSolidProperties();
            globalDirPool = DisassemblyDirectionsWithFastener.RunGraphGeneration(AssemblyGraph, SolidsNoFastener);
            // the second user interaction must happen here

            
            //saveDirections();
            /*
            Console.WriteLine("Press enter once input directions generated >>");
            Console.ReadLine();
            loadDirections();
            */

            NonadjacentBlockingWithPartitioning.Run(AssemblyGraph, SolidsNoFastener, globalDirPool);
            Stabilityfunctions.GenerateReactionForceInfo(AssemblyGraph);
            var leapSearch = new LeapSearch();
            var solutions = leapSearch.Run(AssemblyGraph, Solids, globalDirPool);
            OptimalOrientation.Run(solutions);
            var cand = new AssemblyCandidate() { Sequence = solutions };
            cand.SaveToDisk(Directory.GetCurrentDirectory() + "\\workspace\\solution.xml");
            //WorkerAllocation.Run(solutions, reorientation);
            Console.WriteLine("\n\nDone");
            Console.ReadLine();
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
                SolidsMass.Add(solidName,userUpdated.Mass);
            }
        }

        internal static void saveDirections()
        {

            XmlSerializer ser = new XmlSerializer(typeof(DirectionSaveStructure));
            var writer = new StreamWriter("workspace/directionList.xml");
            var theData = new DirectionSaveStructure();
            theData.arcs = AssemblyGraph.arcs;
            theData.Directions = DisassemblyDirectionsWithFastener.Directions;
            ser.Serialize(writer, theData);

        }

        internal static void loadDirections()
        {

            XmlSerializer ser = new XmlSerializer(typeof(DirectionSaveStructure));
            var reader = new StreamReader("workspace/directionList2.xml");
            var theData = (DirectionSaveStructure)ser.Deserialize(reader);
            AssemblyGraph.arcs = theData.arcs;

        }

        private static void UpdateFasteners(PartsProperties partsProperties)
        {
            foreach (var solidName in Solids.Keys)
            {
                var userUpdated = partsProperties.parts.First(p => p.Name == solidName);
                if (userUpdated.FastenerCertainty == 0)
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
            Console.WriteLine("Loading STLs ....");
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
                parts.Add(EnlargeTheSolid(ts[0]));
                i++;
            }
            //);
            Console.WriteLine("All the files are loaded successfully");
            Console.WriteLine("    * Number of tessellated solids:   " + parts.Count);
            Console.WriteLine("    * Total Number of Triangles:   " + parts.Sum(s => s.Faces.Count()));
            return parts.ToDictionary(tessellatedSolid => tessellatedSolid.Name, tessellatedSolid => new List<TessellatedSolid> { tessellatedSolid });
        }

        private static TessellatedSolid EnlargeTheSolid(TessellatedSolid ts)
        {
            var newVer = ts.Vertices.Select(vertex => new TempVer {Ver = vertex, IndexInList = 0}).ToList();
            var newFace = ts.Faces.Select(face => new TempFace { Face = face, Vers = face.Vertices.Select(v=>newVer.First(nv=> nv.Ver ==v)).ToArray()}).ToList();
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
            var tsM = new TessellatedSolid(tvglFaces, tvglVertices, null, UnitType.millimeter, ts.FileName);
            tsM.Repair();
            return tsM;
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