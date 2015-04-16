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
            var filer = new BasicFiler("", "", "");
            List<TessellatedSolid> parts = GetSTLs("..\\..\\..\\Test\\");
            var assemblyGraph = (designGraph)filer.Open("..\\..\\..\\Test\\inputNG.gxml")[0];
            var globalDirPool = new List<int> { 0, 1, 2, 3, 4, 5 };
            //List<int> globalDirPool = DisassemblyDirections.Run(assemblyGraph); //Input: assembly model
            var solutions = new List<AssemblyCandidate>();
            var inputData = new ConvexHullAndBoundingBox(assemblyGraph);
            DisassemblyProcessOrderedDFS.Run(inputData, globalDirPool); // the output is the assembly sequence
            //DisassemblyProcessBeam.Run(inputData, globalDirPool);
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
                var ts = TVGL.IOFunctions.IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name);
                lock(parts)parts.Add(ts);
            }
            );
            return parts;
        }
    }

}
