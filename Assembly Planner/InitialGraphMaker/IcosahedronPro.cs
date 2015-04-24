using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;

namespace Assembly_Planner
{
    class IcosahedronPro
    {
        internal static List<double[]> DirectionGeneration()
        {
            var directions = new List<double[]>();
            const int stepSize = 5;
            
            // x = r sin(theta)*cos(phi)
            // y = r sin(theta)*sin(phi)
            // z = r cos(theta)
            // Obviously r is equal to 1.
            for (var thetaD = 0; thetaD <= 180; thetaD+=stepSize)
            {
                var thetaR = (thetaD * Math.PI) / 180;
                for (var phiD = 0; phiD < 360; phiD+=stepSize)
                {
                    var phiR = (phiD * Math.PI) / 180;
                    var x = Math.Sin(thetaR)*Math.Cos(phiR);
                    var y = Math.Sin(thetaR)*Math.Sin(phiR);
                    var z = Math.Cos(thetaR);
                    if (directions.Any(d => d[0] == x && d[1] == y && d[2] == z))
                        continue;
                    directions.Add(new[]{x,y,z});
                }
            }
            return directions;
        }
    }
}
