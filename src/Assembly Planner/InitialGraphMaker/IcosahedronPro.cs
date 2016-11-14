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
        internal static List<double[]> DirectionGeneration(double stepSize = 30)
        {
            var directions = new List<double[]>();
            var totalCases = (181 * 181) / (stepSize * stepSize);
            // x = r sin(theta)*cos(phi)
            // y = r sin(theta)*sin(phi)
            // z = r cos(theta)
            // Obviously r is equal to 1.
            var c = 0;
            for (var thetaD = 0.0; thetaD <= 180; thetaD += stepSize)
            {
                var thetaR = (thetaD * Math.PI) / 180;
                //double stepSize2;
                //if (c <= 180 / (stepSize * 2.0))
                //    stepSize2 = 180 + c * ((stepSize * (stepSize - 180)) / 90.0);
                //else
                //    stepSize2 = stepSize - (c - (180 / (stepSize * 2.0))) * ((stepSize * (stepSize - 180)) / 90.0);
                //c++;
                for (var phiD = 0.0; phiD < 180; phiD += stepSize)
                {
                    c++;
                    var phiR = (phiD * Math.PI) / 180;
                    var x = Math.Round(Math.Sin(thetaR) * Math.Cos(phiR), 4);
                    var y = Math.Round(Math.Sin(thetaR) * Math.Sin(phiR), 4);
                    var z = Math.Round(Math.Cos(thetaR), 4);
                    if (
                        directions.Any(
                            d => Math.Abs(d[0] - x) < 0.1 && Math.Abs(d[1] - y) < 0.1 && Math.Abs(d[2] - z) < 0.001))
                        continue;
                    directions.Add(new[] { x, y, z });
                }
            }
            var directions2 = (from dir in directions
                               where
                                   !directions.Any(
                                       d =>
                                           d[0] == dir.multiply(-1.0)[0] &&
                                           d[1] == dir.multiply(-1.0)[1] &&
                                           d[2] == dir.multiply(-1.0)[2])
                               select dir.multiply(-1.0)).ToList();
            directions.AddRange(directions2);
            if (!directions.Any(d => d[0] == 1 && d[1] == 0 && d[2] == 0))
                directions.Add(new[] { 1, 0.0, 0 });
            if (!directions.Any(d => d[0] == 0 && d[1] == 1 && d[2] == 0))
                directions.Add(new[] { 0, 1.0, 0 });
            if (!directions.Any(d => d[0] == 0 && d[1] == 0 && d[2] == 1))
                directions.Add(new[] { 0, 0, 1.0 });
            if (!directions.Any(d => d[0] == -1 && d[1] == 0 && d[2] == 0))
                directions.Add(new[] { -1, 0.0, 0 });
            if (!directions.Any(d => d[0] == 0 && d[1] == -1 && d[2] == 0))
                directions.Add(new[] { 0, -1.0, 0 });
            if (!directions.Any(d => d[0] == 0 && d[1] == 0 && d[2] == -1))
                directions.Add(new[] { 0, 0, -1.0 });
            return directions;
        }
    }
}

