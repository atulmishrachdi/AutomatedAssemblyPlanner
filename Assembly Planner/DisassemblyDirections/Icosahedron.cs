using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;

namespace Assembly_Planner
{
    class Icosahedron
    {
        internal static List<double[]> DirectionGeneration()
        {
            // It is a temprorary approach

            var directions = new List<double[]>();
            var planes = new List<double[]>
            {
                new double[] {-1, 0, 0},
                new double[] {1, 0, 0},
                new double[] {0, -1, 0},
                new double[] {0, 1, 0},
                new double[] {0, 0, -1},
                new double[] {0, 0, 1}
            };

            //for the plane [1,0,0]
            foreach (var plane in planes)
            {
                if (plane[0] != 0)
                {
                    var i = plane[0];
                    for (double j = -1; j <= 1; j += 0.001)
                    {
                        for (double k = -1; k <= 1; k += 0.001)
                        {
                            var m = new[] { i, j, k };
                            directions.Add(m.normalize());
                        }
                    }
                }
                if (plane[1] != 0)
                {
                    var j = plane[1];
                    for (double i = -1; i <= 1; i += 0.001)
                    {
                        for (double k = -1; k <= 1; k += 0.001)
                        {
                            var m = new[] { j, j, k };
                            directions.Add(m.normalize());
                        }
                    }
                }
                if (plane[2] != 0)
                {
                    var k = plane[2];
                    for (double i = -1; i <= 1; i += 0.001)
                    {
                        for (double j = -1; j <= 1; j += 0.001)
                        {
                            var m = new[] { j, j, k };
                            directions.Add(m.normalize());
                        }
                    }
                }

                // there are some repeated numbers which are fine
            }
            return directions;
        }
    }
}
