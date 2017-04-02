using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseClasses;
using Geometric_Reasoning;
using StarMathLib;
using TVGL;
using BoundingCylinder = Geometric_Reasoning.BoundingCylinder;


namespace Fastener_Detection
{
    class GearPolynomialTrend
    {
        internal static Gear PolynomialTrendDetector(TessellatedSolid solid)
        {
            // Since gears have different shapes, we need to generate bounding circles in multiple locations
            // around the gear (bounding cylinde). If any of them are showing a gear, return true. 
            // This makes the code really slow.
            // To also find negative (internal) gears:
            //   1. if the closest with negative dot product triangles to bounding cylinder points, it is a positive gear
            //   2. if the closest with positive dot product triangles to bounding cylinder points, it is a negative gear
            var section = 5.0;
            var bC = BoundingCylinder.Run(solid);
            var kPointsOnSurface = KpointObMidSurfaceOfCylinderGenerator(bC, 1000);
            for (var i = 0.0; i <= 1; i += 1/section)
            {
                var distancePointToSolidPositive = PointToSolidDistanceCalculator(solid, kPointsOnSurface, bC, i);
                // in the distance point to solid array, first one  is for outer triangle (general case)
                // the second one is for inner triangle (written for negative gears)
                List<int> originalInds;
                distancePointToSolidPositive[0] =
                    FastenerPolynomialTrend.MergingEqualDistances(distancePointToSolidPositive[0], out originalInds, 0.001);
                //FastenerPolynomialTrend.PlotInMatlab(distancePointToSolidPositive[0]);
                if (IsGear(distancePointToSolidPositive[0]))
                    return new Gear
                    {
                        Solid = solid,
                        PointOnAxis = bC.PointOnTheCenterLine,
                        Axis = bC.CenterLineVector,
                        LargeCylinderRadius = bC.Radius,
                        SmallCylinderRadius = bC.Radius - TeethDepthFinder(distancePointToSolidPositive[0])
                    };
                // check and see if it is an inner gear
                List<int> originalInds2;
                distancePointToSolidPositive[1] =
                    FastenerPolynomialTrend.MergingEqualDistances(distancePointToSolidPositive[1],out originalInds2, 0.001);
                if (IsGear(distancePointToSolidPositive[1]))
                    return new Gear
                    {
                        Solid = solid,
                        Type = GearType.Internal,
                        PointOnAxis = bC.PointOnTheCenterLine,
                        Axis = bC.CenterLineVector,
                        LargeCylinderRadius = bC.Radius,
                        SmallCylinderRadius = bC.Radius - TeethDepthFinder(distancePointToSolidPositive[0])
                    };

            }
            return null;
        }

        internal static double TeethDepthFinder(List<double> distancePointToSolid, int k = 10)
        {
            // To consider the noise, I cannot simply say that the depth is equal to the 
            // highest double in distancePointToSolid
            distancePointToSolid.Sort();
            // take the highest one if it is repeated more than k for example!
            for (var i = distancePointToSolid.Count-1; i >= 0; i--)
            {
                var candidate = distancePointToSolid[i];
                var r = 0;
                for (var j = i-1; j >= 0; j--)
                {
                    if (Math.Abs(distancePointToSolid[i] - candidate) < 0.001)
                        r++;
                    if (r >= k)
                        return candidate;
                }
            }
            return 0.0;
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

        private static List<double>[] PointToSolidDistanceCalculator(TessellatedSolid solid, List<double[]> kPointsOnSurface, BoundingCylinder bC, double section)
        {
            var distListOuter = new List<double>();
            var distListInner = new List<double>();
            var met = bC.CenterLineVector.multiply(bC.Length*section);
            kPointsOnSurface =
                kPointsOnSurface.Select(p => p.add(met)).ToList();
            foreach (var point in kPointsOnSurface)
            {
                var rayVector = (bC.PointOnTheCenterLine.add(met)).subtract(point);
                var ray = new Ray(new Vertex(point), rayVector);
                var minDisOuter = double.PositiveInfinity;
                var minDisInner = double.PositiveInfinity;
                foreach (var face in solid.Faces)
                {
                    double[] hittingPoint;
                    bool outerTriangle;
                    if (!GeometryFunctions.RayIntersectsWithFace(ray, face, out hittingPoint, out outerTriangle))
                        continue;
                    var dis = GeometryFunctions.DistanceBetweenTwoVertices(hittingPoint, point);
                    if (outerTriangle)
                    {
                        if (dis < minDisOuter) 
                            minDisOuter = dis;
                    }
                    else
                    {
                        if (dis < minDisInner)
                            minDisInner = dis;
                    }
                }
                if (!double.IsPositiveInfinity(minDisOuter))
                    distListOuter.Add(minDisOuter);
                if (!double.IsPositiveInfinity(minDisInner))
                    distListInner.Add(minDisOuter);
            }
            // Normalizing:
            var longestDis1 = distListOuter.Max();
            var distanceToOuterTriangles = distListOuter.Select(d => d / longestDis1).ToList();
            var longestDis2 = distListInner.Max();
            var distanceToInnerTriangles = distListInner.Select(d => d / longestDis2).ToList();
            return new[] {distanceToOuterTriangles, distanceToInnerTriangles};
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
