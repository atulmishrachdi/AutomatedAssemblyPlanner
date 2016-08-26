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
        public const double MeshMagnifier = 10;
        public static double[] PointInMagicBox = {0,0,0.0};

        public static List<int> globalDirPool = new List<int>();

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
            var s = Stopwatch.StartNew();
            s.Start();
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
            DeserializeSolidProperties();
            globalDirPool = DisassemblyDirectionsWithFastener.RunGraphGeneration(AssemblyGraph, SolidsNoFastener);
            var TTC1 = AssemblyGraph.nodes.Count;
            var counter1 = 0;
            var batches = new List<HashSet<Component>>();
            var stack = new Stack<Component>();
            var visited = new HashSet<Component>();
            var globalVisited = new HashSet<Component>();
            foreach (Component Component in AssemblyGraph.nodes.Where(n => !globalVisited.Contains(n)))
            {
                stack.Clear();
                visited.Clear();
                stack.Push(Component);
                while (stack.Count > 0)
                {
                    var pNode = stack.Pop();
                    visited.Add(pNode);
                    counter1++;
                    globalVisited.Add(pNode);
                    List<Connection> a2;
                    lock (pNode.arcs)
                        a2 = pNode.arcs.Where(a => a is Connection).Cast<Connection>().ToList();

                    foreach (Connection arc in a2)
                    {
                        if (!AssemblyGraph.nodes.Contains(arc.From) || !AssemblyGraph.nodes.Contains(arc.To)) continue;
                        var otherNode = (Component)(arc.From == pNode ? arc.To : arc.From);
                        if (visited.Contains(otherNode))
                            continue;
                        stack.Push(otherNode);
                    }
                }
                if (visited.Count != AssemblyGraph.nodes.Count)
                {
                    var a = 2;
                }
            }
            // the second user interaction must happen here
            NonadjacentBlockingWithPartitioning.Run(AssemblyGraph, SolidsNoFastener, globalDirPool);
            Stabilityfunctions.GenerateReactionForceInfo(AssemblyGraph);
            var leapSearch = new LeapSearch();
            var solutions = leapSearch.Run(AssemblyGraph, Solids, globalDirPool, 1);
            OptimalOrientation.Run(solutions);
            var cand = new AssemblyCandidate() { Sequence = solutions };
            cand.SaveToDisk(Directory.GetCurrentDirectory() + "\\workspace\\solution.xml");
            //WorkerAllocation.Run(solutions, reorientation);
            s.Stop();
            Console.WriteLine();
            Console.WriteLine("TOTAL TIME:" + "     " + s.Elapsed);
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

        private static void UpdateFasteners(PartsProperties partsProperties)
        {
            foreach (var solidName in Solids.Keys)
            {
                var userUpdated = partsProperties.parts.First(p => p.Name == solidName);
                if (userUpdated.FastenerCertainty == 1)
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