using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GeometryReasoning;
using GraphSynth;
using GraphSynth.Representation;
using MIConvexHull;
using StarMathLib;
using TVGL.Primitive_Surfaces.ClassifyTesselationAsPrimitives;
using TVGL.Tessellation;

namespace Assembly_Planner
{
    class Program
    {
        static void Main(string[] args)
        {
            //var filer = new BasicFiler("", "", "");
            var solids = GetSTLs("..\\..\\..\\Test\\CubeSTL");
            //var assemblyGraph = (designGraph)filer.Open("..\\..\\..\\Test\\inputNG.gxml")[0];
            //var globalDirPool = new List<int> { 0, 1, 2, 3, 4, 5 };
            var assemblyGraph = new designGraph();
            List<int> globalDirPool = DisassemblyDirections.Run(assemblyGraph, solids); //Input: assembly model
            Updates.AddPartsProperties(assemblyGraph);
            var solutions = new List<AssemblyCandidate>();
            var inputData = new ConvexHullAndBoundingBox(assemblyGraph);
            
            //OrderedDFS.Run(inputData, globalDirPool); // the output is the assembly sequence
            BeamSearch.Run(inputData, globalDirPool);
            
            OptimalOrientation.Run(solutions);
        }

        private static List<TessellatedSolid> GetSTLs(string InputDir)
        {
            List<TessellatedSolid> parts=new List<TessellatedSolid>();
            var di = new DirectoryInfo(InputDir);
            IEnumerable<FileInfo> fis = di.EnumerateFiles("*.stl");
            Parallel.ForEach(fis, fileInfo =>
            //foreach (var fileInfo in fis)
            {
                var ts = TVGL.IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name);
                ts.Name = ts.Name.Remove(0, 1);
                lock(parts)parts.Add(ts);
            }
            );
            return parts;
        }
    }

}
