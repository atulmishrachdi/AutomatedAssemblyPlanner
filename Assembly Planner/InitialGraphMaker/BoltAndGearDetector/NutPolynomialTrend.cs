using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using Assembly_Planner;
using StarMathLib;
using TVGL;
using Vertex = TVGL.Vertex;

namespace Assembly_Planner
{
    class NutPolynomialTrend
    {
        internal static bool PolynomialTrendDetector(TessellatedSolid solid)
        {
            // first create the bounding cylinder. 
            // then from the centerline of the cylinder, shoot rays. 
            var bCy = BoundingGeometry.BoundingCylinderDic[solid];
            const int k = 500;

            var secondPoint = bCy.PointOnTheCenterLine.add(bCy.CenterLineVector.multiply(bCy.Length));
            var kPointsBetweenMidPoints = FastenerPolynomialTrend.KpointBtwPointsGenerator(bCy.PointOnTheCenterLine, secondPoint, k);

            double longestDist;
            var distancePointToSolid = FastenerPolynomialTrend.PointToSolidDistanceCalculator(solid, kPointsBetweenMidPoints,
                bCy.PerpVector, out longestDist);

            // one more step: Merge points with equal distances.
            List<int> originalInds;
            distancePointToSolid = FastenerPolynomialTrend.MergingEqualDistances(distancePointToSolid, out originalInds, 0.001);
            int numberOfThreads;
            int[] threadStartEndPoints;
            if (FastenerPolynomialTrend.ContainsThread(distancePointToSolid, out numberOfThreads, out threadStartEndPoints))
            {
                FastenerDetector.Nuts.Add(new Nut
                {
                    Solid = solid,
                    NumberOfThreads = numberOfThreads,
                    OverallLength = bCy.Length,
                    Diameter = bCy.Radius * 2,
                    Certainty = 1.0
                });
                return true;
            }
            // Plot:
            //if (hasThread)
            FastenerPolynomialTrend.PlotInMatlab(distancePointToSolid);
            return false;
        }


        private static double DiameterOfFastenerFinderUsingPolynomial(List<double> distancePointToSolid,
            int[] threadStartEndPoints, PolygonalFace longestSide, TessellatedSolid solid, double longestDist)
        {
            var newList = new List<double>();
            for (var i = threadStartEndPoints[0];
                i < threadStartEndPoints[1];
                i++)
                newList.Add(distancePointToSolid[i]);
            var obbEdgesLength =
                GeometryFunctions.SortedLengthOfObbEdges(BoundingGeometry.OrientedBoundingBoxDic[solid]);
            // one of the first two small lengths are the number that I am looking for.
            var shortestEdgeOfTriangle = GeometryFunctions.SortedEdgeLengthOfTriangle(longestSide)[0];
            var obbDepth = Math.Abs(obbEdgesLength[0] - shortestEdgeOfTriangle) < 0.01
                ? obbEdgesLength[1]
                : obbEdgesLength[0];

            return obbDepth - 2*(newList.Min()*longestDist);
        }
    }
}
