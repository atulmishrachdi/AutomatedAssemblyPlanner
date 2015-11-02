using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Planner
{
    public class BasicGeometryFunctions
    {
        public static double DistanceBetweenTwoVertices(double[] vertex1, double[] vertex2)
        {
            return
                Math.Sqrt((Math.Pow(vertex1[0] - vertex2[0], 2)) +
                          (Math.Pow(vertex1[1] - vertex2[1], 2)) +
                          (Math.Pow(vertex1[2] - vertex2[2], 2)));
        }
    }
}
