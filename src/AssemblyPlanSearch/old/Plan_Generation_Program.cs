﻿using System.Collections.Generic;
using GraphSynth.Representation;
using TVGL;

namespace Plan_Generation
{
    class Program
    {
        public static int BeamWidth;
        public static designGraph AssemblyGraph;
        public static Dictionary<string, List<TessellatedSolid>> SolidsNoFastenerSimplified 
            = new Dictionary<string, List<TessellatedSolid>>();

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
        static void Main(string[] args)
        {
            NonadjacentBlockingWithPartitioning.Run(AssemblyGraph, SolidsNoFastenerSimplified, globalDirPool);
        }
    }
}
