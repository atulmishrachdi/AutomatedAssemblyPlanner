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
            public double LC = 23 / 1000.0;// Load Constant always equal to 51 pound
            public double HM; // Horizontal Multiplier = 10/H at destination
            public double VM; // Vertical Multiplier = 1-(0.0075*|V-30|) at destination
            public double RAM; // Rotating Angle Multiplier
            public double CM; // Coupling Multiplier, from table 7 at destination
            public double Vmax = 177.8; // cm
            public double Hmin = 25.4; // cm
            public double Hmax = 63.5; // cm
            public double Dmin = 25.4; // cm
            public double Dmax = 177.8; // cm
            public double Amin = 0; // cm
            public double Amax = 343; // cm
            public double RWL; // Recommended Weight Limit
            public double RI; // Lifting Index = ObjectWeight/RWL

        }

        internal class LiftingIndices
        {
            // The lifting index can be assumed to be the same for each faces in a station
            // public double LC = 23 * Math.Pow(Bridge.MeshMagnifier, 3) / 1000.0; // Load Constant which is always equal to 51 pound
            public double LC = 23 / 1000.0;//TBD
            public double HM; // Horizontal Multiplier = 10/H
            public double VM; // Vertical Multiplier = 1-(0.0075*|V-30|)
            public double DM; // Distance Multiplier = 0.82 + (1.8/D)
            public double AM; // Asymmetric Multiplier = 1 - (0.0032*A)    
            public double FM; // Frequency Multiplier, from table 5
            public double CM; // Coupling Multiplier, from table 7
            public double Vmax = 177.8; // cm
            public double Hmin = 25.4; // cm
            public double Dmin = 25.4; // cm
            public double Dmax = 177.8; // cm
            public double Amin = 0; // cm
            public double Amax = 343; // cm
            public double RWL; // Recommended Weight Limit
            public double LI; // Lifting Index = ObjectWeight/RWL
        }
    }
}
