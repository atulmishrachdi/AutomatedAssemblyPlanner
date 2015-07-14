using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Planner
{
    internal class PostProcessingConstnts
    {
        internal class RotatingIndices
        {
            // The Rotating index varies from face to face
            public double LC = 51;          // Load Constant always equal to 51 pound
            public double HM;               // Horizontal Multiplier = 10/H at destination
            public double VM;               // Vertical Multiplier = 1-(0.0075*|V-30|) at destination
            public double RAM;              // Rotating Angle Multiplier
            public double CM;               // Coupling Multiplier, from table 7 at destination
            public double Vmax = 70;        // Inches
            public double Hmin = 10;        // Inches
            public double Hmax = 25;        // Inches
            public double Dmin = 10;        // Inches
            public double Dmax = 70;        // Inches
            public double Amin = 0;         // Inches
            public double Amax = 135;       // Inches
            public double RWL;              // Recommended Weight Limit
            public double RI;               // Lifting Index = ObjectWeight/RWL

        }

        internal class LiftingIndices
        {
            // The lifting index can be assumed to be the same for each faces in a station
            public double LC = 51;          // Load Constant which is always equal to 51 pound
            public double HM;               // Horizontal Multiplier = 10/H
            public double VM;               // Vertical Multiplier = 1-(0.0075*|V-30|)
            public double DM;               // Distance Multiplier = 0.82 + (1.8/D)
            public double AM;               // Asymmetric Multiplier = 1 - (0.0032*A)    
            public double FM;               // Frequency Multiplier, from table 5
            public double CM;               // Coupling Multiplier, from table 7
            public double Vmax = 70;        // Inches
            public double Hmin = 10;        // Inches
            public double Dmin = 10;        // Inches
            public double Dmax = 70;        // Inches
            public double Amin = 0;         // Inches
            public double Amax = 135;       // Inches
            public double RWL;              // Recommended Weight Limit
            public double LI;               // Lifting Index = ObjectWeight/RWL
        }
    }
}
