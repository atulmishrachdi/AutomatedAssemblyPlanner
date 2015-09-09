using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Threading.Tasks;
using Assembly_Planner.GraphSynth.BaseClasses;
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
            var graphExists = true;
            var inputDir =
                //"../../../Test/Cube";
                //"../../../Test/Pump Assembly";
                // "../../../Test/3-parts-statbility";
                //"../../../Test/Double";
                //"../../../Test/Simple-Test";
                //"../../../Test/simple";
                //"../../../Test/PumpWExtention";
                "../../../Test/FoodPackagingMachine";
                // "C:\\DMDII Project\\GearAndFastener Detection\\TrainingData\\not-screw";
                //"../../../Test/FPM2";
            var solids = GetSTLs(inputDir);
            designGraph assemblyGraph;
            List<int> globalDirPool = new List<int>();
            if (graphExists)
            {
                var fileName = "../../../Test/PremadeGraphs/FPM.gxml";
                assemblyGraph = (designGraph)GraphSaving.OpenSavedGraph(fileName)[0];
                globalDirPool = GraphSaving.RetrieveGlobalDirsFromExistingGraph(assemblyGraph);
                var Directions = IcosahedronPro.DirectionGeneration();
                DisassemblyDirections.Directions = new List<double[]>(Directions);
            }
            else
            {
                assemblyGraph = new designGraph();
                //var globalDirPool = DisassemblyDirections.Run(assemblyGraph, solids);
                globalDirPool = DisassemblyDirectionsWithFastener.Run(assemblyGraph, solids);
                //Updates.AddPartsProperties(inputDir, assemblyGraph);
                //NonadjacentBlockingDeterminationPro.Run(assemblyGraph, solids, globalDirPool);
                NonadjacentBlockingDetermination.Run(assemblyGraph, solids, globalDirPool);
                GraphSaving.SaveTheGraph(assemblyGraph);
            }
            var inputData = new ConvexHullAndBoundingBox(inputDir, assemblyGraph);
            var solutions = RecursiveOptimizedSearch.Run(inputData, globalDirPool);
            //var solutions = OrderedDFS.Run(inputData, globalDirPool,solids); // the output is the assembly sequence
            //var solutions = BeamSearch.Run(inputData, globalDirPool);
           
            //var reorientation = OptimalOrientation.Run(solutions);
            //WorkerAllocation.Run(solutions, reorientation);
            
            Console.ReadLine();
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