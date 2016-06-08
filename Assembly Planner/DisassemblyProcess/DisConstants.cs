using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Planner
{
    internal class DisConstants
    {
        internal static string SeperateHyperarcs = "Seperated";
        internal static string SCC = "SCC";
        internal static string gSCC = "FrozenNodes";
        internal static string Removable = "Removable";
        internal static string SingleNode = "Done";
        internal static int BeamWidth = 50;
        internal static double Parallel = 1e-5;
        internal static string Gear = "Gear";
        internal static string Bolt = "Bolt";
        internal static double GearNormal = -11000.0;
        public const int DirIndUpperBound = -11;  // all the integers between two -10 and -11 are the feasible directions for the arc
        public const int DirIndLowerBound = -10;
        public static double BoltDirectionOfFreedom = -12000.0;
        public static double BoltDepth = -13000.0;
        public static double BoltRadius = -14000.0;
        public static double IndexOfNodesLockedByFastenerL = -15000.0;
        public static double IndexOfNodesLockedByFastenerU = -15001.0;
    }
}
