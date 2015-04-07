using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using StarMathLib;
using TVGL.Primitive_Surfaces.ClassifyTesselationAsPrimitives;

namespace Assembly_Planner
{
    class Program
    {
        static void Main(string[] args)
        {
            var assemblyGraph = new designGraph();
            var globalDirPool = DisassemblyDirections.Run(assemblyGraph); //Input: assembly model
            DisassemblyProcess.Run(assemblyGraph, globalDirPool);
        }
    }

}
