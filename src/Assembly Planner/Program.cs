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
        public const double MeshMagnifier = 1;
        public static double[] PointInMagicBox = {0,0,0.0};

        public static List<int> globalDirPool = new List<int>();

        private static void Main(string[] args)
        {

            string inputDir;
#if InputDialog
             inputDir = consoleFrontEnd.getPartsDirectory();
#else
            inputDir =
                //    "../../../Test/Cube";
                "../../../Test/PumpWExtention";
            //"../../../Test/FastenerTest/new/test";
            //"../../../Test/Double";
            //"../../../Test/test7";
            //"../../../Test/FPM2";
            //"../../../Test/Mc Cormik/STL2";
            //"../../../Test/Truck -TXT-1/STL";
            //"../../../Test/FoodPackagingMachine/FPMSTL2";
            //"../../../../GearAndFastener Detection/TrainingData/not-screw/Gear";
            //   "../../../Test/test8";
#endif
            var s = Stopwatch.StartNew();
            s.Start();
            Solids = GetSTLs(inputDir);
            var detectFasteners = false; //TBI
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
            //DeserializeSolidProperties();
            globalDirPool = DisassemblyDirectionsWithFastener.RunGraphGeneration(AssemblyGraph, SolidsNoFastener);
            // the second user interaction must happen here
            NonadjacentBlockingWithPartitioning.Run(AssemblyGraph, SolidsNoFastener, globalDirPool);
            Stabilityfunctions.GenerateReactionForceInfo(AssemblyGraph);
            var leapSearch = new LeapSearch();
            var solutions = leapSearch.Run(AssemblyGraph, Solids, globalDirPool, 1);
            OptimalOrientation.Run(solutions);
            var cand = new AssemblyCandidate() { Sequence = solutions };
            cand.SaveToDisk(Directory.GetCurrentDirectory() + "\\solution.xml");
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
            var writer = new StreamWriter("parts_properties.xml");
            ser.Serialize(writer, partsProperties);
        }

        private static void DeserializeSolidProperties()
        {
            XmlSerializer ser = new XmlSerializer(typeof(PartsProperties));
            var reader = new StreamReader("parts_properties2.xml");
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
            foreach (var fileInfo in fis)
            {
                var ts = IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name);
                //ts.Name = ts.Name.Remove(0, 1);
                //lock (parts) 
                parts.Add(ts[0]);
            }
            //);
            Console.WriteLine("All the files are loaded successfully");
            Console.WriteLine("    * Number of tessellated solids:   " + parts.Count);
            Console.WriteLine("    * Total Number of Triangles:   " + parts.Sum(s => s.Faces.Count()));
            return parts.ToDictionary(tessellatedSolid => tessellatedSolid.Name, tessellatedSolid => new List<TessellatedSolid> { tessellatedSolid });
        }
    }
}