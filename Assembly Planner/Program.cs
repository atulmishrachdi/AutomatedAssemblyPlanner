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
            assemblyGraph.addNode("node_one");
            graphElement iuy = assemblyGraph["node_one"];
            if (n1 == iuy) Console.WriteLine("same");
            DisassemblyDirections.Run(assemblyGraph); //Input: assembly model 
        }
    }

}
