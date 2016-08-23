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
    public class Program1
    {
        

        public static void main(string[] args)
        {

            string inputDir;
#if InputDialog
            inputDir = consoleFrontEnd.getPartsDirectory();
#else
            inputDir = "workspace";
            //"src/Test/Cube";
            //"src/Test/PumpWExtention";
            //"src/Test/FastenerTest/new/test";
            //"src/Test/Double";
            //"src/Test/test7";
            //"src/Test/FPM2";
            //"src/Test/Mc Cormik/STL2";
            //"src/Test/Truck -TXT-1/STL";
            //"src/Test/FoodPackagingMachine/FPMSTL2";

#endif
            var s = Stopwatch.StartNew();
            s.Start();
            Program.Solids = GetSTLs(inputDir);
            var detectFasteners = true; //TBI
            var threaded = 0; // 0:none, 1: all, 2: subset

            Program.AssemblyGraph = new designGraph();

            DisassemblyDirectionsWithFastener.RunGeometricReasoning(Program.Solids);

            if (detectFasteners)
            {
                DisassemblyDirectionsWithFastener.RunFastenerDetection(Program.Solids, threaded);
            }


            SerializeSolidProperties();


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
            foreach (var solidName in Program.Solids.Keys)
            {
                Console.WriteLine(solidName);
                var userUpdated = partsProperties.parts.First(p => p.Name == solidName);
                Program.SolidsMass.Add(solidName, userUpdated.Mass);
            }
        }

        private static void UpdateFasteners(PartsProperties partsProperties)
        {
            foreach (var solidName in Program.Solids.Keys)
            {
                var userUpdated = partsProperties.parts.First(p => p.Name == solidName);
                if (userUpdated.FastenerCertainty == 0)
                    Program.SolidsNoFastener.Add(solidName, Program.Solids[solidName]);
            }
            FastenerDetector.Fasteners =
                new HashSet<Fastener>(
                    FastenerDetector.Fasteners.Where(f => !Program.SolidsNoFastener.Keys.Contains(f.Solid.Name)).ToList());
            FastenerDetector.Nuts =
                new HashSet<Nut>(
                    FastenerDetector.Nuts.Where(n => !Program.SolidsNoFastener.Keys.Contains(n.Solid.Name)).ToList());
            FastenerDetector.Washers =
                new HashSet<Washer>(
                    FastenerDetector.Washers.Where(w => !Program.SolidsNoFastener.Keys.Contains(w.Solid.Name)).ToList());
            FastenerDetector.PotentialFastener =
                new HashSet<TessellatedSolid>(
                    FastenerDetector.PotentialFastener.Where(u => !Program.SolidsNoFastener.Keys.Contains(u.Name)).ToList());
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


