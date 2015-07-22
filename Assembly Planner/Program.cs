using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GraphSynth;
using GraphSynth.Representation;
using TVGL;
using TVGL.IOFunctions;
using TVGL;

namespace Assembly_Planner
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var inputDir =
                //"../../../Test/Cube";
                "../../../Test/Pump Assembly";
              // "../../../Test/3-parts-statbility";
            
                //"../../../Test/Double";
                //"../../../Test/PumpWExtention";
                //"../../../Test/FoodPackagingMachine";
                //"../../../Test/FPM2";
            var solids = GetSTLs(inputDir);
            var assemblyGraph = new designGraph();
            
            //var globalDirPool = DisassemblyDirections.Run(assemblyGraph, solids);
            var globalDirPool = DisassemblyDirectionsWithFastener.Run(assemblyGraph, solids);

            //SaveTheGraph(assemblyGraph);

            var inputData = new ConvexHullAndBoundingBox(inputDir, assemblyGraph);
            Updates.AddPartsProperties(inputDir, assemblyGraph);
            //NonadjacentBlockingDeterminationPro.Run(assemblyGraph, solids, globalDirPool);
            NonadjacentBlockingDetermination.Run(assemblyGraph, solids, globalDirPool);
            
            //var solutions = RecursiveOptimizedSearch.Run(inputData, globalDirPool);
            var solutions = OrderedDFS.Run(inputData, globalDirPool,solids); // the output is the assembly sequence
            //var solutions = BeamSearch.Run(inputData, globalDirPool);
           
            //var reorientation = OptimalOrientation.Run(solutions);
            //WorkerAllocation.Run(solutions, reorientation);
            
            Console.ReadLine();
        }

        private static void SaveTheGraph(designGraph assemblyGraph)
        {
            var outputDirectory = "../../../Test";
            var setting = new GlobalSettings();
            var sa = new BasicFiler(setting.InputDir, setting.OutputDir, setting.RulesDir);
            sa.outputDirectory = outputDirectory;
            sa.Save("graph.gxml", assemblyGraph, false);
        }

        private static List<TessellatedSolid> GetSTLs(string InputDir)
        {
            var parts = new List<TessellatedSolid>();
            var di = new DirectoryInfo(InputDir);
            var fis = di.EnumerateFiles("*.STL");
            Parallel.ForEach(fis, fileInfo =>
                //foreach (var fileInfo in fis)
            {
                var ts = IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name);
                //ts.Name = ts.Name.Remove(0, 1);
                lock (parts) parts.Add(ts[0]);
            }
                );
            return parts;
        }
    }
}