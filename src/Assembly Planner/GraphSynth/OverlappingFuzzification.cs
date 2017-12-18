using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseClasses
{
    public class OverlappingFuzzification
    {
        public const double ParralelLinesL = 0.005; //0.001
        public const double ParralelLinesU = 0.0055;
        public const double ParralelLines2L = 0.05; //0.001
        public const double ParralelLines2U = 0.055;
        public const double RadiusDifsL = 3; //0.02;
        public const double RadiusDifsU = 3.3;
        public const double PointOnLineL = 0.00001;
        public const double PointOnLineU = 0.000011;
        public const double PointPointL = 0.0001;
        public const double PointPointU = 0.00011;
        public const double PlaneDistL = 0.1; // 0.0001  0.035
        public const double PlaneDistU = 0.11;
        public const double CheckWithGlobDirsParall = 0.00015;  //0.05
        public const double CheckWithGlobDirsParall2 = 0.03;  //0.05
        public const double CheckWithGlobDirs = -0.00001;//0.0;
        public const double EqualToZeroL = 5e-4;
        public const double EqualToZeroU = 5.5e-4;
        public const double EqualToZero2L = 0.15;
        public const double EqualToZero2U = 0.165;
        public const double FractionIncreaseForAABBIntersect = 0.05;

        public static double FuzzyProbabilityCalculator(double lowerBound, double upperBound, double clcultdDble)
        {
            // So, it is basically a case like this:    Ow my God, see how creative I am :D
            //              ^
            //              |__________    __________
            //              |          \  /
            //              |  Overlap  \/   Dont Overlap
            //              |           /\
            //              |__________/__\________________>
            //
            if (clcultdDble < lowerBound) return 1;
            if (clcultdDble > upperBound) return 0;
            return ((0 - 1) / (upperBound - lowerBound)) * (clcultdDble - lowerBound) + 1;
        }
    }
}
