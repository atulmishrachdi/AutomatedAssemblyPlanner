using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseClasses.Representation;
using Geometric_Reasoning;
using Graph_Generation;

namespace Plan_Generation
{
    class Program
    {
        public static int BeamWidth;
        public static designGraph AssemblyGraph;
        public static List<double> DegreeOfFreedoms = new List<double>();
        public static List<double> StablbiblityScores = new List<double>();
        public static double StabilityWeightChosenByUser = 0;
        public static double UncertaintyWeightChosenByUser = 0;
        protected internal static int AvailableWorkers = 0;
        public static double StabilityScore = 0;
        public static bool RobustSolution = false;
        public static List<int> globalDirPool = new List<int>();
        public static List<double> allmtime = new List<double>();
        public static List<double> allitime = new List<double>();
        public static List<double> gpmovingtime = new List<double>();
        public static List<double> gpinstalltime = new List<double>();
        public static List<double> gpsecuretime = new List<double>();
        public static List<double> gprotate = new List<double>();

        //Added this from the original program class. Added TVGL prefix to TessellatedSolid type to resolve syntax errors
        public static Dictionary<string, List<TVGL.TessellatedSolid>> SolidsNoFastenerSimplified = new Dictionary<string, List<TVGL.TessellatedSolid>>();

        static void Main(string[] args)
        {
            NonadjacentBlockingWithPartitioning.Run(AssemblyGraph, SolidsNoFastenerSimplified, globalDirPool);
        }
    }
}
