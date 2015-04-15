using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth;
using GraphSynth.Representation;
using StarMathLib;
using TVGL.Primitive_Surfaces.ClassifyTesselationAsPrimitives;

namespace Assembly_Planner
{
    class Program
    {
        static void Main(string[] args)
        {
            var filer = new BasicFiler("","","");
            var assemblyGraph = (designGraph)filer.Open("C:\\Users\\Nima\\Documents\\OSU\\Project\\AssemblyPlanner\\Test\\inputNG.gxml")[0];
            var globalDirPool = new List<int>{0,1,2,3,4,5};
            //List<int> globalDirPool = DisassemblyDirections.Run(assemblyGraph); //Input: assembly model
            var solutions = new List<AssemblyCandidate>();
            var inputData = new ConvexHullAndBoundingBox(assemblyGraph);
            DisassemblyProcessOrderedDFS.Run(inputData, globalDirPool); // the output is the assembly sequence
            //DisassemblyProcessBeam.Run(inputData, globalDirPool);
            OptimalOrientation.Run(solutions);
        }
    }

}
