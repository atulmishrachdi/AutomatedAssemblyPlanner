using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using StarMathLib;
using TVGL;

namespace Assembly_Planner
{
    class GearPolynomialTrend
    {
        internal static bool PolynomialTrendDetector(TessellatedSolid solid)
        {
            // Since gears have different shapes, we need to generate bounding circles in multiple locations
            // around the gear (bounding cylinde). If any of them are showing a gear, return true. 
            // This makes the code really slow.
            var section = 5.0;
            var bC = BoundingCylinder.Run(solid);
            var kPointsOnSurface = KpointObMidSurfaceOfCylinderGenerator(bC, 2000);
            for (var i = 0.0; i <= 1; i+=1/section)
            {
                var distancePointToSolid = PointToSolidDistanceCalculator(solid, kPointsOnSurface, bC, i);
                distancePointToSolid = FastenerPolynomialTrend.MergingEqualDistances(distancePointToSolid, 0.001);
                FastenerPolynomialTrend.PlotInMatlab(distancePointToSolid);
                if (IsGear(distancePointToSolid))
                    return true;
            }
            return false;
        }

        private static List<double[]> KpointObMidSurfaceOfCylinderGenerator(BoundingCylinder bC, int k)
        {
            var points = new List<double[]>();
            var tt = new[] { 0.0, Math.PI / 2, Math.PI, Math.PI * 3 / 4.0, 2 * Math.PI };
            var stepSize = (2*Math.PI)/(k + 1);
            var n = bC.CenterLineVector;
            var u = bC.PerpVector;
            var r = bC.Radius;
            for (var i = 0.0; i < 2*Math.PI; i+=stepSize)
            {
                points.Add((((u.multiply(r*Math.Cos(i))).add((n.crossProduct(u)).multiply(r*Math.Sin(i)))).add(
                    bC.PointOnTheCenterLine)));
            }
            return points;
        }

        private static List<double> PointToSolidDistanceCalculator(TessellatedSolid solid, List<double[]> kPointsOnSurface, BoundingCylinder bC, double section)
        {
            var distList = new List<double>();
            kPointsOnSurface =
                kPointsOnSurface.Select(p => p.add(bC.CenterLineVector.multiply(-bC.Length*section))).ToList();
            foreach (var point in kPointsOnSurface)
            {
                var rayVector = (bC.PointOnTheCenterLine.add(bC.CenterLineVector.multiply(-bC.Length*section))).subtract(point);
                var ray = new Ray(new AssemblyEvaluation.Vertex(point), new Vector(rayVector));
                var minDis = double.PositiveInfinity;
                foreach (var face in solid.Faces)
                {
                    double[] hittingPoint;
                    if (!BasicGeometryFunctions.RayIntersectsWithFace(ray, face, out hittingPoint))
                        continue;
                    var dis = BasicGeometryFunctions.DistanceBetweenTwoVertices(hittingPoint, point);
                    if (dis < minDis) minDis = dis;
                }
                if (minDis != double.PositiveInfinity)
                    distList.Add(minDis);
            }
            // Normalizing:
            var longestDis = distList.Max();
            return distList.Select(d => d / longestDis).ToList();
        }


        private static bool IsGear(List<double> distancePointToSolid)
        {
            var directionChange = new List<double>();
            for (var i = 0; i < distancePointToSolid.Count - 1; i++)
            {
                var c = 0;
                for (var j = i + 1;
                    Math.Sign(distancePointToSolid[j] - distancePointToSolid[j - 1]) ==
                    Math.Sign(distancePointToSolid[i + 1] - distancePointToSolid[i]);
                    j++)
                {
                    c++;
                    if (j == distancePointToSolid.Count - 1) break;
                }
                directionChange.Add(c);
                i += (c - 1);
            }
            //PlotInMatlab(directionChange);
            return ContainsTeethSeries(directionChange);
        }

        private static bool ContainsTeethSeries(List<double> directionChange)
        {

            var thread = new List<double>();
            var cumulativePoi = new List<double>();
            for (var i = 0; i < directionChange.Count - 1; i++)
            {
                if (directionChange[i] < 2) continue;
                var c = 1;
                var cumulativePoints = directionChange[i];
                for (var j = i + 1; Math.Abs(directionChange[j] - directionChange[j - 1]) < 10; j++)
                {
                    if (j == directionChange.Count - 1) break;
                    if (directionChange[j] < 2) continue;
                    cumulativePoints += directionChange[j];
                    c++;
                }
                thread.Add(c);
                cumulativePoi.Add(cumulativePoints);
                i += (c - 1);
            }
            // I can use minimum common number of threads for this * 2 
            // if it is 5 and less, cumulativePoints must be less than 90 of the total number of points
            return thread.Any(t => t > 20);
        }
    }
}
