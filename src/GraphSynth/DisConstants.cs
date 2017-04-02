using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseClasses
{
    public class DisConstants
    {
        public static string SeperateHyperarcs = "Seperated";
        public static string SCC = "SCC";
        public static string gSCC = "FrozenNodes";
        public static string Removable = "Removable";
        public static string SingleNode = "Done";
        public static int BeamWidth = 50;
        public static double Parallel = 1e-5;
        public static string Gear = "Gear";
        public static string Bolt = "Bolt";
        public static double GearNormal = -11000.0;
        public const int DirIndUpperBound = -11;  // all the integers between two -10 and -11 are the feasible directions for the arc
        public const int DirIndLowerBound = -10;
        public static double BoltDirectionOfFreedom = -12000.0;
        public static double BoltDepth = -13000.0;
        public static double BoltRadius = -14000.0;
        public static double IndexOfNodesLockedByFastenerL = -15000.0;
        public static double IndexOfNodesLockedByFastenerU = -15001.0;
    }
}
