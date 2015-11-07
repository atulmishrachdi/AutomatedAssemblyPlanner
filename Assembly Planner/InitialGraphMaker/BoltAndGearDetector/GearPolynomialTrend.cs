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
            var bC = BoundingCylinder.Run(solid);
            var kPointsOnSurface = KpointObMidSurfaceOfCylinderGenerator(bC, 2000);
            var distancePointToSolid = PointToSolidDistanceCalculator(solid, kPointsOnSurface,
                bC.PointOnTheCenterLine.add(bC.CenterLineVector.multiply(-bC.Length*1.0/4.0)));
            // one more step: Merge points with equal distances.
            distancePointToSolid = FastenerPolynomialTrend.MergingEqualDistances(distancePointToSolid, 0.001);
            FastenerPolynomialTrend.PlotInMatlab(distancePointToSolid);
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
                    bC.PointOnTheCenterLine)).add(n.multiply(-bC.Length*1.0/4.0)));
            }
            return points;
        }

        private static List<double> PointToSolidDistanceCalculator(TessellatedSolid solid, List<double[]> kPointsOnSurface, double[] centerPoint)
        {
            var distList = new List<double>();
            foreach (var point in kPointsOnSurface)
            {
                var rayVector = centerPoint.subtract(point);
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
    }
}
