using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Planner
{
    class OverlappingFuzzification
    {
        internal static double FuzzyProbabilityCalculator(double lowerBound, double upperBound, double clcultdDble)
        {
            // So, it is basically a case like this:    Ow my God, see how creative I am :D
            //              ^
            //              |__________   __________
            //              |          \ /
            //              |  Overlap  \   Dont Overlap
            //              |          / \
            //              |_________/___\________________>
            //
            if (clcultdDble < lowerBound) return 1;
            if (clcultdDble > upperBound) return 0;
            return ((0 - 1) / (upperBound - lowerBound)) * (clcultdDble - lowerBound) + 1;
        }
    }
}
