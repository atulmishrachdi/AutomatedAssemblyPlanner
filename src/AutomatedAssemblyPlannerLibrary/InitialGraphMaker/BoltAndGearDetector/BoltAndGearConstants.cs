using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Planner
{
    internal class BoltAndGearConstants
    {
        public const double ConePortion = 0.35;
        public static double ConeAreaPortion = 0.25;
        public static int TriabglesInTheGearSideFaces = 100;
        public static int AcceptableNumberOfDenseEdges = 150;
        public static double SmoothAngle = 0.9; // 25.8 degree
        public static int GearTeeth = 15;
    }
}