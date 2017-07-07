using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseClasses.Representation;
using StarMathLib;
using TVGL;
using TVGL;
using BaseClasses;
using TVGL.IOFunctions;
using Component = BaseClasses.Component;

namespace Assembly_Planner
{
    internal class DisassemblyDirectionsWithFastener
    {
        public static List<double[]> Directions = new List<double[]>();

        internal static Dictionary<int, List<Component[]>> NonAdjacentBlocking =
            new Dictionary<int, List<Component[]>>(); //Component[0] is blocked by Component[1]

        internal static Dictionary<TessellatedSolid, List<PrimitiveSurface>> SolidPrimitive =
            new Dictionary<TessellatedSolid, List<PrimitiveSurface>>();
        internal static Dictionary<string, List<TessellatedSolid>> Solids;
        internal static Dictionary<string, List<TessellatedSolid>> SolidsNoFastener;
        internal static List<TessellatedSolid> PartsWithOneGeom;

        protected static int gCounter = 0;

        internal static void RunGeometricReasoning(Dictionary<string, List<TessellatedSolid>> solids)
        {
            Process.Start("Geometric_Reasoning.exe");
        }

        internal static void RunFastenerDetection(Dictionary<string, List<TessellatedSolid>> solids, int threaded)
        {
            Process.Start("Fastener_Detection.exe");
        }

        internal static List<int> RunGraphGeneration(designGraph assemblyGraph, Dictionary<string, List<TessellatedSolid>> solidsNoFastener)
        {
            throw new NotImplementedException();
        }



    }
}

