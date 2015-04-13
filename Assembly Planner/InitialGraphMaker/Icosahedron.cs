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
            const int h1 = 2;
            const int h2 = 1;
            var stepSize = h2*1.0/(Math.Pow(10, h1));
            //for the plane [1,0,0]
            foreach (var plane in planes)
            {
                if (plane[0] != 0)
                {
                    var i = plane[0];
                    for (var j = -1.0; j <= 1.0; j += stepSize)
                    {
                        for (var k = -1.0; k <= 1.0; k += stepSize)
                        {
                            var m = new[] { Math.Round(i, h1), Math.Round(j, h1), Math.Round(k, h1) };
                            directions.Add(m.normalize());
                        }
                    }
                }
                if (plane[1] != 0)
                {
                    var j = plane[1];
                    for (var i = -1.0; i <= 1.0; i += stepSize)
                    {
                        for (var k = -1.0; k <= 1.0; k += stepSize)
                        {
                            var m = new[] { Math.Round(i, h1), Math.Round(j, h1), Math.Round(k, h1) };
                            directions.Add(m.normalize());
                        }
                    }
                }
                if (plane[2] != 0)
                {
                    var k = plane[2];
                    for (var i = -1.0; i <= 1.0; i += stepSize)
                    {
                        for (var j = -1.0; j <= 1.0; j += stepSize)
                        {
                            var m = new[] { Math.Round(i, h1), Math.Round(j, h1), Math.Round(k, h1) };
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
