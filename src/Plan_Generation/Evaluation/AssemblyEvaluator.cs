using System.Data;
using Assembly_Planner;
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using StarMathLib;
using TVGL.IOFunctions;
using TVGL;
using BaseClasses;
using BaseClasses.AssemblyEvaluation;
using BaseClasses.Representation;
using Geometric_Reasoning;
using Plan_Generation;
using Constants = BaseClasses.AssemblyEvaluation.Constants;

namespace Plan_Generation.AssemblyEvaluation
{
    public class AssemblyEvaluator
    {
        private static List<List<PolygonalFace>> newRefCVHFacesInCom = new List<List<PolygonalFace>>();
        private static Dictionary<string, TVGLConvexHull> ConvexHullsForParts = new Dictionary<string, TVGLConvexHull>();
        private int Iterations;
        private readonly TimeEvaluator timeEvaluator;

        #region Constructor
        public AssemblyEvaluator(List<TessellatedSolid> solids)
        {
            foreach (var solid in solids)
                ConvexHullsForParts.Add(solid.Name, solid.ConvexHull);
            timeEvaluator = new TimeEvaluator();
        }

        #endregion


        public double Evaluate(AssemblyCandidate c, option opt, List<Component> rest, List<TessellatedSolid> solides)
        {
            // Set up moving and reference subassemblies
            var newSubAsm = c.Sequence.Update(opt, rest, ConvexHullsForParts);
            var refNodes = newSubAsm.Install.Reference.PartNames.Select(n => (Component)c.graph[n]).ToList();
            var movingNodes = newSubAsm.Install.Moving.PartNames.Select(n => (Component)c.graph[n]).ToList();
            var install = new[] { refNodes, movingNodes };
            var connectingArcs = c.graph.arcs.Cast<Connection>().Where(a => ((movingNodes.Contains(a.To) && refNodes.Contains(a.From))
                                                         || (movingNodes.Contains(a.From) && refNodes.Contains(a.To))))
                                                        .ToList();
            //if (connectingArcs.Count == 0) return -1;
            foreach (Connection a in connectingArcs)
            {
                Updates.RemoveRepeatedFasteners2(a, c.graph);
                c.graph.removeArc(a);
            }
            if (Updates.EitherRefOrMovHasSeperatedSubassemblies(install))
                return -1;

            // Getting insertion point coordinates
            double insertionDistance;
            var insertionDirection = FindPartDisconnectMovement(connectingArcs, refNodes, out insertionDistance);

            var firstArc = connectingArcs[0];
            var i = firstArc.localVariables.IndexOf(Constants.Values.CLASH_LOCATION);
            var insertionPoint = (i == -1) ? new Vertex(new[] { 0.0, 0.0, 0.0 })
                : new Vertex(new[] { firstArc.localVariables[i + 1], firstArc.localVariables[i + 2], firstArc.localVariables[i + 3] });

            newSubAsm.Install.InstallDirection = StarMath.multiply(insertionDistance, insertionDirection);
            newSubAsm.Install.InstallPoint = insertionPoint.Position;

            var travelDistance = 1;//m

            //PathDeterminationEvaluator.FindTravelDistance(newSubAsm, insertionDirection, insertionPoint);

            newSubAsm.Install.Time =
                timeEvaluator.EvaluateTimeAndSDForInstall(connectingArcs, travelDistance, insertionDistance, newSubAsm)[0];
            newSubAsm.Install.TimeSD =
                timeEvaluator.EvaluateTimeAndSDForInstall(connectingArcs, travelDistance, insertionDistance, newSubAsm)[1];
            c.f3 += newSubAsm.Install.Time;

            var movableparts = new List<Component>();
            //movableparts = 

            //c.f4 += newSubAsm.Install.TimeSD;
            //c.f4 = timeEvaluator.EvaluateTimeOfLongestBranch(c.Sequence);
            if (double.IsNaN(insertionDirection[0])) Console.WriteLine();

            double evaluationScore = InitialEvaluation(newSubAsm, newSubAsm.Install.InstallDirection, refNodes, movingNodes, c, newSubAsm.Install.Time);

            Updates.UpdateChildGraph(c, install);
            return evaluationScore;
        }


        private static double InitialEvaluation(SubAssembly newSubAsm, double[] installDirection, List<Component> refNodes, List<Component> movingNodes, AssemblyCandidate c, double InstallTime)
        {
            newRefCVHFacesInCom.Clear();

            var unAffectedFaces = UnaffectedRefFacesDuringInstallation(newSubAsm);
            var mergedFaces = MergingFaces(unAffectedFaces);

            c.TimeScore = InstallTime;
            c.AccessibilityScore = AccessabilityEvaluation(installDirection, mergedFaces);
            c.StabilityScore = StabilityEvaluation(newSubAsm, mergedFaces);
            return c.TimeScore + c.AccessibilityScore + c.StabilityScore;
        }

        public static double[] FindPartDisconnectMovement(IEnumerable<Connection> connectingArcs, List<Component> refNodes, out double insertionDistance)
        {
            // This function needs to be rewritten
            var installDirection = new[] { 0.0, 0, 0 };
            // find install direction by averaging all visible_DOF
            //foreach (var arc in connectingArcs)
            //{
            //    var index = arc.localVariables.FindIndex(x => x == Constants.Values.VISIBLE_DOF || x == Constants.Values.CONCENTRIC_DOF);
            //    while (index != -1)
            //    {
            //        var dir = new Vector(arc.localVariables[++index], arc.localVariables[++index], arc.localVariables[++index]);
            //        dir.NormalizeInPlace();
            //        installDirection.AddInPlace(dir);
            //        if (double.IsNaN(dir.Position[0])) continue;
            //        index = arc.localVariables.FindIndex(index, x => x == Constants.Values.VISIBLE_DOF || x == Constants.Values.CONCENTRIC_DOF);
            //    }
            //}
            //installDirection.NormalizeInPlace();
            //if (double.IsNaN(installDirection.Position[0]))
            //{
            //    /* if we are unable to find insertion direction from arcs, we will use the CG of the nodes     */
            //    var numRefCGs = 0;
            //    var numMovingCGs = 0;
            //    var movingCG = new double[3];
            //    var refCG = new double[3];
            //    foreach (var a in connectingArcs)
            //    {
            //        var n = a.To;
            //        var index = n.localVariables.FindIndex(x => x == Constants.Values.TRANSLATION);
            //        if (index != -1)
            //        {
            //            if (refNodes.Contains(n))
            //            {
            //                numRefCGs++;
            //                refCG = StarMath.add(refCG,
            //                    new[] { n.localVariables[++index], n.localVariables[++index], n.localVariables[++index] },
            //                    3);
            //            }
            //            else
            //            {
            //                numMovingCGs++;
            //                movingCG = StarMath.add(movingCG,
            //                    new[] { n.localVariables[++index], n.localVariables[++index],n.localVariables[++index] },
            //                    3);
            //            }
            //        }
            //        n = a.From;
            //        index = n.localVariables.FindIndex(x => x == Constants.Values.TRANSLATION);
            //        if (index != -1)
            //        {
            //            if (refNodes.Contains(n))
            //            {
            //                numRefCGs++;
            //                refCG = StarMath.add(refCG,
            //                    new[] { n.localVariables[++index], n.localVariables[++index], n.localVariables[++index] },
            //                    3);
            //            }
            //            else
            //            {
            //                numMovingCGs++;
            //                movingCG = StarMath.add(movingCG,
            //                    new[] { n.localVariables[++index], n.localVariables[++index], n.localVariables[++index] },
            //                    3);
            //            }
            //        }
            //    }
            //    refCG = StarMath.divide(refCG, numRefCGs, 3);
            //    movingCG = StarMath.divide(movingCG, numMovingCGs, 3);
            //    installDirection.Position = movingCG.subtract(refCG, 3);
            //    installDirection.NormalizeInPlace();
            //}
            //if (double.IsNaN(installDirection.Position[0]))
            //{
            //    installDirection.Position = new[] {1.0, 0.0, 0.0};
            //    SearchIO.output("unable to find install direction between parts",3);
            //}

            //// now, we have to figure out how much to move.
            //// foreach arc, we find the point of the cvx hull of the reference node that is farthest along the install direction,
            //// call it rmv (Referenc Max Value)
            //// then we find the lowest value of the moving cvx hull point along this install direction,
            //// call it mmv (Moving Min Value). 
            //// The difference, delta, is the amount of movument to clear one part from the other.
            //// we take the max delta from all interstitial arcs and multiply it by the install direction
            //insertionDistance = double.NegativeInfinity;
            //foreach (var arc in connectingArcs)
            //{
            //    var fromNode = arc.From;
            //    var toNode = arc.To;
            //    TVGLConvexHull refHull, movingHull;
            //    if (refNodes.Contains(fromNode))
            //    {
            //        refHull = convexHullForPartsA[fromNode.name];
            //        movingHull = convexHullForPartsA[toNode.name];
            //    }
            //    else
            //    {
            //        refHull = convexHullForPartsA[toNode.name];
            //        movingHull = convexHullForPartsA[fromNode.name];
            //    }
            //    var refMaxValue = STLGeometryFunctions.findMaxPlaneHeightInDirection(refHull.Vertices, installDirection);

            //    var movingMinValue = STLGeometryFunctions.findMinPlaneHeightInDirection(movingHull.Vertices, installDirection);

            //    var distance = refMaxValue - movingMinValue;
            //    if (insertionDistance < distance) insertionDistance = distance;
            //}
            insertionDistance = 0;
            return installDirection;
        }

        public static List<PolygonalFace> UnaffectedRefFacesDuringInstallation(SubAssembly newSubAsm)
        {
            var insertionDirection = newSubAsm.Install.InstallDirection;
            //var notAffectedFacesInCom = newSubAsm.Install.Reference.CVXHull.Faces.ToList();
            //var halfOfRefCvh = newSubAsm.Install.Reference.CVXHull.Faces.Where(f => f.Normal.dotProduct(insertionDirection) < 0).ToList();
            /*foreach (var eachFace in halfOfRefCvh)
            {
                foreach (var eachMovingVerticies in newSubAsm.Install.Moving.CVXHull.Vertices)
                {
                    var ray = new Ray(eachMovingVerticies, insertionDirection);
                    var faceAffected = GeometryFunctions.RayIntersectsWithFace(ray, eachFace);
                    if (faceAffected)
                        notAffectedFacesInCom.Remove(eachFace);
                }
            }*/
            //return notAffectedFacesInCom;
            var unaffected1 = newSubAsm.Install.Reference.CVXHull.Faces.Where(f => f.Normal.dotProduct(insertionDirection) > -0.1)
                    .ToList();
            /*foreach (var fastener in newSubAsm.Secure.Fasteners)
            {
                if (fastener.InstallDirection == null) continue;
                unaffected1 = unaffected1.Where(f => f.Normal.dotProduct(fastener.InstallDirection) > 0).ToList();
            }*/

            return unaffected1;
        }

        private static double StabilityEvaluation(SubAssembly newSubAsm, List<FootprintFace> FootPrintFaces)
        {
            double r = 0.0;
            foreach (var newFa in FootPrintFaces)
                r += CheckStabilityForReference(newSubAsm, newFa);
            r = r / FootPrintFaces.Count;
            return r;
        }

        public static double AccessabilityEvaluation(double[] installDirection, List<FootprintFace> FootPrintFaces)
        {
            var r = 0.0;
            foreach (var f in FootPrintFaces)
                r += CheckAccessability(installDirection, f);
            r = r / FootPrintFaces.Count;
            return r;
        }

        private static double TimeEvaluation(List<Component> refNodes, List<Component> movingNodes)
        {
            // This should be s.th. like equal moving and reference assembly time. I must try to find a way to 
            // estimate these two times. Better score for a subassembly with Ref time = moving Time;
            // To do s.th. for OneByOne, I will only consider number of arcs and nodes to evaluate time.
            float time1 = refNodes.Count;
            float time2 = movingNodes.Count;
            double r = 1 - Math.Abs((time1 - time2) / (time2 + time1));
            return 10 * r;
        }

        public static double CheckStabilityForReference(SubAssembly newSubAsm, FootprintFace f)
        {
            var refCOM = newSubAsm.Install.Reference.CenterOfMass;

            CenterOfMassProjectionOnFootprintFace(f, refCOM);

            f.MinDisNeaEdg = double.PositiveInfinity;
            var x02 = f.COMP.Position[0];
            var y02 = f.COMP.Position[1];
            var z02 = f.COMP.Position[2];
            f.Height = Math.Sqrt(Math.Pow(x02 - refCOM.Position[0], 2) +
                                Math.Pow(y02 - refCOM.Position[1], 2) +
                                Math.Pow(z02 - refCOM.Position[2], 2));
            foreach (var edge in f.OuterEdges)
            {
                var edgeVector = edge.From.Position.subtract(edge.To.Position).normalize();
                var distanceOfEdgeToComProj = GeometryFunctions.DistanceBetweenLineAndVertex(edgeVector,
                    edge.From.Position, f.COMP.Position);
                if (distanceOfEdgeToComProj < f.MinDisNeaEdg)
                {
                    f.MinDisNeaEdg = distanceOfEdgeToComProj;
                    f.IsComInsideFace = IsTheProjectionInsideOfTheFace(f, edge);
                }
            }
            var spf = Math.Pow(((2 / Math.PI) * (Math.Atan(f.Height / f.MinDisNeaEdg))), 2);
            f.RefS = spf;
            return f.RefS;
        }

        private static bool IsTheProjectionInsideOfTheFace(FootprintFace f, Edge edge)
        {
            var edgeVec = edge.From.Position.subtract(edge.To.Position).normalize();
            var comVer = f.COMP.Position.subtract(edge.To.Position).normalize();
            Vertex vertexOnFace = null;
            foreach (var e in f.OuterEdges)
            {
                if (e == edge) continue;
                if (1 - Math.Abs(e.From.Position.subtract(edge.To.Position).normalize().dotProduct(edgeVec)) > 1e-8)
                {
                    vertexOnFace = e.From;
                    break;
                }
                if (1 - Math.Abs(e.To.Position.subtract(edge.To.Position).normalize().dotProduct(edgeVec)) > 1e-8)
                {
                    vertexOnFace = e.To;
                    break;
                }
            }
            if (vertexOnFace == null) return false;
            var faceVerVec = vertexOnFace.Position.subtract(edge.To.Position).normalize();
            return edgeVec.crossProduct(comVer).dotProduct(edgeVec.crossProduct(faceVerVec)) > 0;
        }

        private static void CenterOfMassProjectionOnFootprintFace(FootprintFace f, Vertex refCOM)
        {
            // For calculating the projection of COM on current New face, we have normal,
            // we have the position of COM and we have points on the edge. 
            // First We are looking for the parameters in this plane equation:
            // a(x-x0) + b(y-y0) + c(z-z0) = 0 plane equation or "ax + bY + cz = d"
            var pointOnPlane = f.OuterEdges[0].From.Position;
            var norm = f.Normal;
            var com = refCOM.Position;
            var t = (norm[0] * (pointOnPlane[0] - com[0]) + norm[1] * (pointOnPlane[1] - com[1]) +
                     norm[2] * (pointOnPlane[2] - com[2])) / (norm[0] * norm[0] + norm[1] * norm[1] + norm[2] * norm[2]);
            f.COMP = new Vertex(new[] { com[0] + t * norm[0], com[1] + t * norm[1], com[2] + t * norm[2] });
        }

        public static double CheckAccessability(double[] installDirection, FootprintFace f)
        {
            var angleBetweenInsDirAndNormal = Math.Abs(installDirection.normalize().dotProduct(f.Normal));
            return 2 - (angleBetweenInsDirAndNormal / Math.PI) * 2.0;
        }


        /*public static List<FootprintFace> MergingFaces(List<PolygonalFace> initialFaces)
        {
            var unChangedFaces = new List<PolygonalFace>(initialFaces);
            var footprintFaces = new List<FootprintFace>();
            for (var c = 0; c < unChangedFaces.Count; c++)
            {
                var ftpF = new FootprintFace();
                var normal2 = unChangedFaces[c].Normal;
                ftpF.Normal = normal2;
                ftpF.ExCoVer = new List<Vertex>();
                ftpF.Faces = new List<PolygonalFace>();
                for (var i = 0; i < 3; i++)
                {
                    for (var j = i + 1; j < 3; j++)
                    {
                        ftpF.ExCoVer.Add(unChangedFaces[c].Vertices[i]);
                        ftpF.ExCoVer.Add(unChangedFaces[c].Vertices[j]);
                    }
                }
                var deleted = 0;
                for (var n = 0; n < unChangedFaces.Count; n++)
                {
                    var normal1 = unChangedFaces[n].Normal;
                    if ((1 - normal1.dotProduct(normal2))>0.005) continue;
                    ftpF.Faces.Add(unChangedFaces[n]);
                    ftpF.Normal = normal1.add(ftpF.Normal).normalize();
                    for (var i = 0; i < 3; i++)
                    {
                        for (var j = i + 1; j < 3; j++)
                        {
                            ftpF.ExCoVer.Add(unChangedFaces[n].Vertices[i]);
                            ftpF.ExCoVer.Add(unChangedFaces[n].Vertices[j]);

                            for (var k = 0; k < ftpF.ExCoVer.Count - 2; k += 2)
                            {
                                if (ftpF.ExCoVer[k] == unChangedFaces[n].Vertices[i] &&
                                    ftpF.ExCoVer[k + 1] == unChangedFaces[n].Vertices[j])
                                {
                                    ftpF.ExCoVer.Remove(unChangedFaces[n].Vertices[i]);
                                    ftpF.ExCoVer.Remove(unChangedFaces[n].Vertices[j]);
                                    break;
                                }
                                if (ftpF.ExCoVer[k] != unChangedFaces[n].Vertices[j] ||
                                    ftpF.ExCoVer[k + 1] != unChangedFaces[n].Vertices[i]) continue;
                                ftpF.ExCoVer.Remove(unChangedFaces[n].Vertices[i]);
                                ftpF.ExCoVer.Remove(unChangedFaces[n].Vertices[j]);
                                break;
                            }
                        }
                    }
                    unChangedFaces.Remove(unChangedFaces[n]);
                    n--;
                    deleted ++;
                }
                foreach (var f in ftpF.Faces)
                {
                    if (1 - f.Normal.dotProduct(new[] {1.0, 0, 0}) < 0.005)
                    {
                        ftpF.Normal = new[] {1.0, 0, 0};
                        break;
                    }
                    if (1 - f.Normal.dotProduct(new[] { 0.0, 1, 0 }) < 0.005)
                    {
                        ftpF.Normal = new[] { 0.0, 1, 0 };
                        break;
                    }
                    if (1 - f.Normal.dotProduct(new[] { 0.0, 0, 1 }) < 0.005)
                    {
                        ftpF.Normal = new[] { 0.0, 0, 1 };
                        break;
                    }
                    if (1 - f.Normal.dotProduct(new[] { -1.0, 0, 0 }) < 0.005)
                    {
                        ftpF.Normal = new[] { -1.0, 0, 0 };
                        break;
                    }
                    if (1 - f.Normal.dotProduct(new[] { 0.0, -1, 0 }) < 0.005)
                    {
                        ftpF.Normal = new[] { 0.0, -1, 0 };
                        break;
                    }
                    if (1 - f.Normal.dotProduct(new[] { 0.0, 0, -1 }) < 0.005)
                    {
                        ftpF.Normal = new[] { 0.0, 0, -1 };
                        break;
                    }
                }
                footprintFaces.Add(ftpF);
                c -= 1;
            }
            var rnd = new Random();
            //Finding unique external verticies
            foreach (var t in footprintFaces)
            {
                t.ExVer = new List<Vertex>();
                var coVer = t.ExCoVer;
                t.ExVer.Add(coVer[0]);
                for (var i = 1; i < coVer.Count; i++)
                {
                    if (!t.ExVer.Contains(coVer[i]))
                        t.ExVer.Add(coVer[i]);
                }
                t.Name = rnd.Next(0, 100000000);
            }
            foreach (var firstFace in footprintFaces)
            {
                firstFace.Adjacents = new List<FootprintFace>();
                foreach (var secondFace in footprintFaces.Where(secondFace => secondFace.ExCoVer.Any(
                    eachPointOfSecondFace => firstFace.ExCoVer
                        .Contains(eachPointOfSecondFace)) && secondFace != firstFace))
                {
                    firstFace.Adjacents.Add(secondFace);
                }
            }
            return footprintFaces;
        }*/
        public static List<FootprintFace> MergingFaces(List<PolygonalFace> initialFaces)
        {
            var unChangedFaces = new List<PolygonalFace>(initialFaces);
            var footprintFaces = new List<FootprintFace>();
            while (unChangedFaces.Any())
            {
                var lVisited = new HashSet<PolygonalFace>();
                var faces = new List<PolygonalFace>();
                var stack = new Stack<PolygonalFace>();
                var first = unChangedFaces[0];
                faces.Add(first);
                var outerEdges = new List<Edge>(first.Edges);
                foreach (var face in first.AdjacentFaces.Where(face => unChangedFaces.Contains(face)))
                    stack.Push(face);
                var refNormal = first.Normal;
                unChangedFaces.RemoveAt(0);
                while (stack.Any())
                {
                    var tbcFace = stack.Pop();
                    if (1 - tbcFace.Normal.dotProduct(refNormal) > 0.01)
                    {
                        lVisited.Add(tbcFace);
                        continue;
                    }
                    refNormal = ((refNormal.multiply(faces.Count)).add(tbcFace.Normal)).divide(faces.Count + 1).normalize();
                    faces.Add(tbcFace);
                    var sharedEdge = tbcFace.Edges.Where(outerEdges.Contains).ToList();
                    foreach (var sE in sharedEdge)
                        outerEdges.Remove(sE);

                    outerEdges.AddRange(tbcFace.Edges.Where(e => !sharedEdge.Contains(e)));
                    foreach (
                        var face in
                            tbcFace.AdjacentFaces.Where(
                                face => unChangedFaces.Contains(face) && !lVisited.Contains(face)))
                        stack.Push(face);
                    unChangedFaces.Remove(tbcFace);
                }

                /*if (1 - refNormal.dotProduct(new[] { 1.0, 0, 0 }) < 0.05)
                    refNormal = new[] { 1.0, 0, 0 };
                else if (1 - refNormal.dotProduct(new[] { 0.0, 1, 0 }) < 0.05)
                    refNormal = new[] { 0.0, 1, 0 };
                else if (1 - refNormal.dotProduct(new[] { 0.0, 0, 1 }) < 0.05)
                    refNormal = new[] { 0.0, 0, 1 };
                else if (1 - refNormal.dotProduct(new[] { -1.0, 0, 0 }) < 0.05)
                    refNormal = new[] { -1.0, 0, 0 };
                else if (1 - refNormal.dotProduct(new[] { 0.0, -1, 0 }) < 0.05)
                    refNormal = new[] { 0.0, -1, 0 };
                else if (1 - refNormal.dotProduct(new[] { 0.0, 0, -1 }) < 0.05)
                    refNormal = new[] { 0.0, 0, -1 };*/

                footprintFaces.Add(new FootprintFace
                {
                    Normal = refNormal,
                    OuterEdges = outerEdges.ToArray(),
                    Faces = faces
                });
            }
            foreach (var face in footprintFaces)
            {
                face.Adjacents =
                    footprintFaces.Where(f => f != face && f.OuterEdges.Any(face.OuterEdges.Contains)).ToList();
            }
            return footprintFaces;
        }
    }
}





