using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using MIConvexHull;
using StarMathLib;

namespace AssemblyEvaluation
{
    public class ConvexHullEvaluator
    {
        /// <summary>
        /// public variables
        /// </summary>
        public bool isStable = true;
        public double transferTime;
        public double APF;
        public double SPF;
        public double processingTime;
        /// <summary>
        /// private variables
        /// </summary>
        private double TransferLength;
        private double[] CenterOfMass;
        private List<Vertex> convexHullVertices;
        private List<DefaultConvexFace<Vertex>> convexHullFaces;
        private Dictionary<string, ConvexHull<Vertex, DefaultConvexFace<Vertex>>> convexHullDictionary;
        /// <summary>
        /// constructor
        /// </summary>
        public ConvexHullEvaluator(Dictionary<string, ConvexHull<Vertex, DefaultConvexFace<Vertex>>> convexHullDictionary)
        {
            this.convexHullDictionary = convexHullDictionary;
        }
        /// <summary>
        /// methods
        /// </summary>
        /// 


        internal void Perform(Vertex insertionPoint, double[] insertionDirection, SubAssembly newSubAsm)
        {
            // Step #1: Creat ConvexHull Vertices for the SubAssembly
            //CreateConvexHullVertices(componentsInReference);
            var insertDir = new Vector(insertionDirection[0], insertionDirection[1], insertionDirection[2]);
            // Step #2: Compute Accessiblity Metric
            ComputeAccessibleMetric(insertionPoint, insertDir);

            // Step #3: Compute Stability Metric
            ComputeStabilityMetric(insertionPoint, insertDir);

        }

        private void ComputeAccessibleMetric(Vertex insertionPoint, Vector insertionDirection)
        {
            var ray = new Ray(insertionPoint, insertionDirection);

            // Distance to all convexhull vertices
            var coneHeights = new List<double>();
            foreach (var chv in convexHullVertices)
                coneHeights.Add(StarMath.dotProduct(ray.Direction, ray.MakeVectorTo(chv).Position, 3));
            double ConeMaxHeight = Enumerable.Max(coneHeights);

            // Find candidate convexhull point
            int candidateVertexIndex = coneHeights.FindIndex(x => x == ConeMaxHeight);
            Vertex maxVertex = convexHullVertices[candidateVertexIndex];

            // Accessible cone upper-bound (R, D) dimensions
            double ConeMaxRadius = pointRaySquareDistance(ray, maxVertex);
            double halfAngleOfMinCone = findConeHalfAngle(ray, maxVertex);

            double ConeMinRadius = 0.0;
            double ConeMinHeight = double.PositiveInfinity;
            // Accessible cone lower-bound (r, d) dimensions
            for (var i = 0; i < convexHullVertices.Count(); i++)
            {
                var vertex = convexHullVertices[i];
                var coneHeight = coneHeights[i];
                if (coneHeight <= 0) continue;
                var halfAngle = findConeHalfAngle(ray, vertex);
                if (halfAngle > halfAngleOfMinCone) continue;
                if (halfAngle == halfAngleOfMinCone && coneHeight > ConeMinHeight) continue;
                halfAngleOfMinCone = halfAngle;
                ConeMinHeight = coneHeight;
                ConeMinRadius = pointRaySquareDistance(ray, vertex);
            }
            if (ConeMinHeight != 0 && ConeMaxHeight != 0)
                APF = Math.Abs((ConeMinRadius * ConeMaxRadius) / (ConeMinHeight * ConeMaxHeight + ConeMaxHeight * ConeMaxHeight));
            else
                APF = 0;
        }

        private void ComputeStabilityMetric(Vertex Point, Vector insertionAxis)
        {
            // Find fixed face for a given subassembly
            var maxDot = 0.001;
            var FixedFaces = new List<DefaultConvexFace<Vertex>>();
            var FixedFace_Vertices = new List<Vertex>();
            var FixedFace_Normal = new double[3] {insertionAxis.Position[0], 
                                                  insertionAxis.Position[1], 
                                                  insertionAxis.Position[2]};
            for (var i = 0; i < convexHullFaces.Count(); i++)
            {
                DefaultConvexFace<Vertex> F = convexHullFaces[i];
                var Dot = F.Normal[0] * insertionAxis.Position[0] +
                          F.Normal[1] * insertionAxis.Position[1] +
                          F.Normal[2] * insertionAxis.Position[2];
                if (Dot > 0 & Dot < maxDot)
                {
                    FixedFaces.Add(F);
                    FixedFace_Vertices.AddRange(F.Vertices.ToList());
                }
            }

            // Find stability metrics
            if (FixedFaces.Count() > 0)
            {
                double h = 0;
                var CoM = PolygonCoM(FixedFace_Vertices); // This is an approximation of center of mass
                var projected_point = LinePlaneIntersection(FixedFace_Normal, CoM, Point, ref h);
                var boundaryEdges = findBoundaryEdges(FixedFaces, FixedFace_Vertices);
                var d = pointPolygonMinDistance(projected_point, boundaryEdges);
                var Area = pointPolygonAtt(projected_point, FixedFaces, ref isStable);
                SPF = (2 / Math.PI) * Math.Abs(Math.Atan2(h, Math.Abs(d)));
            }
            else
                SPF = 1;
        }

        /// <summary>
        /// distance and polygon computations
        /// </summary>
        private Vertex PolygonCoM(List<Vertex> polygon)
        {
            var n = polygon.Count;
            var CoM = new double[3];
            for (int i = 0; i < n; i++)
            {
                Vertex P = polygon[i];
                CoM[0] += P.Position[0];
                CoM[1] += P.Position[1];
                CoM[2] += P.Position[2];
            }
            CoM[0] /= n;
            CoM[1] /= n;
            CoM[2] /= n;
            return (new Vertex(CoM[0], CoM[1], CoM[2]));
        }
        private Vertex LinePlaneIntersection(double[] normal, Vertex P0, Vertex P1, ref double distance)
        {
            // P0 is the point on the plane, P1 is the input point, P2 is another point on the line
            var T = 1000;
            var P2 = new Vertex(P1.Position[0] + T * normal[0], P1.Position[1] + T * normal[1], P1.Position[2] + T * normal[2]);
            var P0_P1 = new double[3] { P0.Position[0] - P1.Position[0], P0.Position[1] - P1.Position[1], P0.Position[2] - P1.Position[2] };
            distance = Math.Abs(StarMath.dotProduct(normal, P0_P1, 3));

            // Ref: http://paulbourke.net/geometry/pointlineplane/
            var D = -(normal[0] * P0.Position[0] + normal[1] * P0.Position[1] + normal[2] * P0.Position[2]);
            var u = (normal[0] * P1.Position[0] + normal[1] * P1.Position[1] + normal[2] * P1.Position[2] + D) /
                    (normal[0] * (P1.Position[0] - P2.Position[0]) +
                     normal[1] * (P1.Position[1] - P2.Position[1]) +
                     normal[2] * (P1.Position[2] - P2.Position[2]));
            var X = P1.Position[0] + u * (P2.Position[0] - P1.Position[0]);
            var Y = P1.Position[1] + u * (P2.Position[1] - P1.Position[1]);
            var Z = P1.Position[2] + u * (P2.Position[2] - P1.Position[2]);
            return (new Vertex(X, Y, Z));
        }
        private List<Vertex[]> findBoundaryEdges(List<DefaultConvexFace<Vertex>> FixedFaces, List<Vertex> FixedFace_Vertices)
        {
            var boundaryEdges_Length = new List<double>();
            var boundaryEdges = new List<Vertex[]>();
            var boundaryEdges_Check = new List<bool>();
            for (var i = 0; i < FixedFaces.Count(); i++)
            {
                for (var j = 0; j < FixedFaces[i].Vertices.Count(); j++)
                {
                    var first_index = j;
                    var next_index = j + 1; if (j + 1 == FixedFaces[i].Vertices.Count()) next_index = 0;
                    var P1 = FixedFaces[i].Vertices[first_index];
                    var P2 = FixedFaces[i].Vertices[next_index];
                    var P1_P2 = new Vertex[2];
                    P1_P2[0] = P1;
                    P1_P2[1] = P2;
                    var L = StarMath.norm2(new double[3] { P2.Position[0] - P1.Position[0], 
                                                           P2.Position[1] - P1.Position[1], 
                                                           P2.Position[2] - P1.Position[2] });
                    var validEdge = true;
                    for (var k = 0; k < boundaryEdges.Count(); k++)
                    {
                        if (boundaryEdges_Length[k] == L)
                        {
                            if ((P1.Same(boundaryEdges[k][0]) || P1.Same(boundaryEdges[k][1])) &&
                                (P2.Same(boundaryEdges[k][0]) || P2.Same(boundaryEdges[k][1])))
                            {
                                boundaryEdges_Check[k] = false;
                                validEdge = false;
                                break;
                            }
                        }
                    }
                    if (validEdge)
                    {
                        boundaryEdges_Check.Add(true);
                        boundaryEdges.Add(P1_P2);
                        boundaryEdges_Length.Add(L);
                    }
                }
            }

            var validBoundaryEdges = new List<Vertex[]>();
            for (var i = 0; i < boundaryEdges_Check.Count(); i++)
                if (boundaryEdges_Check[i]) validBoundaryEdges.Add(boundaryEdges[i]);

            return validBoundaryEdges;
        }
        private double pointPolygonAtt(Vertex point, List<DefaultConvexFace<Vertex>> FixedFaces, ref bool PMC)
        {
            /*
             Mehtod #1
              Store the crossproduct between the vector from the point to the last 
              vertex of the polygon and the vector from the point to the first vertex 
              of the polygon. Then, for each vertex of the polygon (i)
              and the next vertex (i+1), do the crossproduct like the first and with the 
              result do a dotproduct with input point P. If this results in a negative value, 
              your point (P) is outside of the polygon.
             
             Method #2
              The Jordan Curve Theorem states that a point is inside a polygon if 
              the number of crossings from an arbitrary direction is odd. 
            */
            double Area = 0;
            var ListPMC = new List<bool> { };
            for (var np = 0; np < FixedFaces.Count(); np++)
            {
                var tmpPMC = true;
                var polygon = FixedFaces[np].Vertices.ToList();
                var n = polygon.Count;
                var CoM = PolygonCoM(polygon);
                var X = point.Position[0];
                var Y = point.Position[1];
                var Z = point.Position[2];

                // Crossproduct between the vector from the point to the last vertex of the polygon and 
                // the vector from the point to the first vertex of the polygon
                var V = new double[3] { polygon[n - 1].Position[0] - X, polygon[n - 1].Position[1] - Y, polygon[n - 1].Position[2] - Z };
                var W = new double[3] { polygon[0].Position[0] - X, polygon[0].Position[1] - Y, polygon[0].Position[2] - Z };
                var VW = StarMath.crossProduct(V, W);
                for (int i = 0; i < n; i++)
                {
                    Vertex P1, P2;
                    P1 = polygon[i];
                    if ((i + 1) == polygon.Count()) P2 = polygon[0]; else P2 = polygon[i + 1];

                    // compute PMC test result
                    var E = new double[3] { P1.Position[0] - X, P1.Position[1] - Y, P1.Position[2] - Z };
                    var F = new double[3] { P2.Position[0] - X, P2.Position[1] - Y, P2.Position[2] - Z };
                    var EF = StarMath.crossProduct(E, F);
                    if (tmpPMC)
                    {
                        var VW_dot_EF = StarMath.dotProduct(VW, EF, 3);
                        if (VW_dot_EF < 0)
                            tmpPMC = false; // point is outside of the polygon
                    }
                    // compute polygon area
                    var G = new double[3] { P1.Position[0] - CoM.Position[0], P1.Position[1] - CoM.Position[1], P1.Position[2] - CoM.Position[2] };
                    var H = new double[3] { P2.Position[0] - CoM.Position[0], P2.Position[1] - CoM.Position[1], P2.Position[2] - CoM.Position[2] };
                    var GH = StarMath.crossProduct(G, H);
                    Area += 0.5 * StarMath.norm2(GH);
                    ListPMC.Add(tmpPMC);
                }
                if (ListPMC.Contains(true)) PMC = true; else PMC = false;
            }
            return Area;
        }
        private double pointPolygonMinDistance(Vertex point, List<Vertex[]> boundaryEdges)
        {
            // compute min distance from a point to the polygon edges
            var minDistance = 1e10;
            for (var i = 0; i < boundaryEdges.Count(); i++)
            {
                var P1 = boundaryEdges[i][0];
                var P2 = boundaryEdges[i][1];

                var M = new double[3] { (P2.Position[0] - P1.Position[0]), (P2.Position[1] - P1.Position[1]), (P2.Position[2] - P1.Position[2]) };
                var N = new double[3] { (point.Position[0] - P1.Position[0]), (point.Position[1] - P1.Position[1]), (point.Position[2] - P1.Position[2]) };
                var MN = StarMath.crossProduct(M, N);
                var tmpDistance = StarMath.norm2(MN) / StarMath.norm2(M);
                if (Math.Abs(tmpDistance) < Math.Abs(minDistance))
                {
                    minDistance = Math.Abs(tmpDistance);
                }
            }
            return minDistance;
        }
        private double pointPolygonDistance(Vertex point, List<Vertex> polygon)
        {
            double minDistance = 1e10;
            for (var i = 0; i < polygon.Count(); i++)
            {
                Vertex P1, P2;
                P1 = polygon[i];
                double x_1 = P1.Position[0];
                double y_1 = P1.Position[1];
                double z_1 = P1.Position[2];

                if ((i + 1) == polygon.Count()) P2 = polygon[0];
                else P2 = polygon[i + 1];

                double x_2 = P2.Position[0];
                double y_2 = P2.Position[1];
                double z_2 = P2.Position[2];

                double x_3 = point.Position[0];
                double y_3 = point.Position[1];
                double z_3 = point.Position[2];

                var V = new double[3] { (x_2 - x_1), (y_2 - y_1), (z_2 - z_1) };
                var W = new double[3] { (x_3 - x_1), (y_3 - y_1), (z_3 - z_1) };
                var VW = StarMath.crossProduct(V, W);
                var D = StarMath.norm2(VW) / StarMath.norm2(V);
                if (Math.Abs(D) < Math.Abs(minDistance))
                {
                    minDistance = D;
                }
            }
            return minDistance;
        }
        private double pointRaySquareDistance(Ray R, IVertex P) // Equivalent to Cross Product
        {
            Vector v = R.MakeVectorTo(P);
            var cross = StarMath.crossProduct(R.Direction, v.Position);
            return StarMath.norm2(cross);
        }


        private double findConeHalfAngle(Ray R, Vertex P)
        {
            Vector v = R.MakeVectorTo(P);
            v.NormalizeInPlace();
            return StarMath.dotProduct(R.Direction, v.Position, 3);
        }

        private double pointRayRealDistance(Ray R, IVertex P) // Equivalent to Dot Product
        {
            Vector w = R.MakeVectorTo(P);
            return StarMath.dotProduct(R.Direction, R.MakeVectorTo(P).Position, 3);
        }

    }
}