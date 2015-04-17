using System.Data;
using GeometryReasoning;
using GraphSynth;
using GraphSynth.Representation;
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace AssemblyEvaluation
{
    public class AssemblyEvaluator
    {
        private static List<List<DefaultConvexFace<Vertex>>> newRefCVHFacesInCom = new List<List<DefaultConvexFace<Vertex>>>();
        public static List<NewFootprintFaces> nFFList = new List<NewFootprintFaces>();
        private Dictionary<string, ConvexHull<Vertex, DefaultConvexFace<Vertex>>> convexHullForParts;
        public static Dictionary<string, Vector> insertionDirectionDic = new Dictionary<string, Vector>();
        private int Iterations;

        #region Constructor
        public AssemblyEvaluator(Dictionary<string, ConvexHull<Vertex, DefaultConvexFace<Vertex>>> convexHullForParts)
        {
            //feasibility = new FeasibilityEvaluator();
            //timeEvaluator = new TimeEvaluator();
            this.convexHullForParts = convexHullForParts;
            //    reOrientations = new ReOrientations();
        }

        #endregion


        public double Evaluate(AssemblyCandidate c, option opt)
        {
            // STEP #1: Set up moving and reference subassemblies
            var newSubAsm = c.Sequence.Update(opt, convexHullForParts);
            var refNodes = newSubAsm.Install.Reference.PartNodes.Select(n => (node)c.graph[n]).ToList();
            var movingNodes = newSubAsm.Install.Moving.PartNodes.Select(n => (node)c.graph[n]).ToList();
            var connectingArcs = c.graph.arcs.Where(a => ((movingNodes.Contains(a.To) && refNodes.Contains(a.From))
                                                         || (movingNodes.Contains(a.From) && refNodes.Contains(a.To)))).ToList();

            var install = new[] { refNodes, movingNodes };
            // For the moving or references which have unconnected parts: for example the moving is made of two parts which are
            // not connected to each other. I have to find a solution to avoid the error of "connectingArcs == 0" on the next iterations
            // Again I need to creat a stack and push every node.


            //foreach (var refORmov in install)
            //{
            //    var stack = new Stack<node>();
            //    var globalVisited = new List<node>();

            //    while (globalVisited.Count != refORmov.Count)
            //    {
            //        var notAssignedNode = refORmov.Where(n => !globalVisited.Contains(n)).ToList()[0];
            //        while (stack.Count != 0)
            //        {

            //        }
            //    }
            //}
            //foreach (var refORmov in install)
            //{
            //    var islands = new List<List<node>>();
            //    var firstNode = refORmov[0];
            //    for (var j = 1; j < refORmov.Count; j++)
            //    {
            //        var secondtNode = refORmov[0];
            //        // if there is an arc which connects t
            //    }
            //}

            // STEP #2: Getting insertion point coordinates
            double insertionDistance;
            var insertionDirection = FindPartDisconnectMovement(connectingArcs, refNodes, out insertionDistance);

            var firstArc = connectingArcs[0];
            var i = firstArc.localVariables.IndexOf(Constants.CLASH_LOCATION);
            var insertionPoint = (i == -1) ? new Vertex(0, 0, 0)
                : new Vertex(firstArc.localVariables[i + 1], firstArc.localVariables[i + 2], firstArc.localVariables[i + 3]);

            newSubAsm.Install.InstallDirection = StarMath.multiply(insertionDistance, insertionDirection.Position, 3);
            newSubAsm.Install.InstallPoint = insertionPoint.Position;
            
            //var travelDistance = PathDeterminationEvaluator.FindTravelDistance(newSubAsm, insertionDirection, insertionPoint);
            //newSubAsm.Install.Time =
            //    timeEvaluator.EvaluateTimeForInstall(connectingArcs.Count(), travelDistance, insertionDistance, newSubAsm);

            //c.f3 += newSubAsm.Install.Time;
            //c.f4 = timeEvaluator.EvaluateTimeOfLongestBranch(c.Sequence);
            //c.OrderScore = feasibility.EvaluateOrder(movingNodes, refNodes);
            if (double.IsNaN(insertionDirection.Position[0])) Console.WriteLine();
            //c.AccessibilityScore = ComputeAccessibleMetric(insertionPoint, insertionDirection, newSubAsm.Install.Reference.CVXHull.Points.ToList()); // Accessible Penalty Factor

            double evaluationScore = InitialEvaluation(newSubAsm, newSubAsm.Install.InstallDirection, insertionDirection, refNodes, movingNodes, c);
            //c.StabilityScore = ComputeStabilityMetric(insertionDirection, newSubAsm, combinedCVXHull, AssemblySequence.refFacesInCombined); // Stability Penalty Factor
            //var aaa = new NewFootprintFaces();
            //c.StabilityScore = StabilityConvinenceAndRotationCosts(insertionDirection, newSubAsm/*,AssemblySequence.refFacesInCombined*/, aaa);
            //return newSubAsm.Install.Time * (c.AccessibilityScore + c.StabilityScore + 1); // Overall Objective
            

            foreach (var a in connectingArcs)
                c.graph.removeArc(a);

            for (var j = 0; j < c.graph.hyperarcs.Count; j++)
            {
                var hy = c.graph.hyperarcs[j];
                if (hy.localLabels.Contains("Seperated") || hy.localLabels.Contains("Done")) continue;
                c.graph.removeHyperArc(c.graph.hyperarcs[j]);
                j--;
            }

            foreach (var list in install)
            {
                if (list.Count == 1)
                {
                    c.graph.addHyperArc(list);
                    c.graph.hyperarcs[c.graph.hyperarcs.Count - 1].localLabels.Add("Done");
                }
                else
                {
                    c.graph.addHyperArc(list);
                    c.graph.hyperarcs[c.graph.hyperarcs.Count - 1].localLabels.Add("Seperated");
                }
            }
            
            return evaluationScore;
        }

        private Vector FindPartDisconnectMovement(IEnumerable<arc> connectingArcs, List<node> refNodes, out double insertionDistance)
        {
            var installDirection = new Vector(0, 0, 0);
            // find install direction by averaging all visible_DOF
            foreach (var arc in connectingArcs)
            {
                var index = arc.localVariables.FindIndex(x => x == Constants.VISIBLE_DOF || x == Constants.CONCENTRIC_DOF);
                while (index != -1)
                {
                    var dir = new Vector(arc.localVariables[++index], arc.localVariables[++index], arc.localVariables[++index]);
                    dir.NormalizeInPlace();
                    installDirection.AddInPlace(dir);
                    if (double.IsNaN(dir.Position[0])) continue;
                    index = arc.localVariables.FindIndex(index, x => x == Constants.VISIBLE_DOF || x == Constants.CONCENTRIC_DOF);
                }
            }
            installDirection.NormalizeInPlace();
            if (double.IsNaN(installDirection.Position[0]))
            {
                /* if we are unable to find insertion direction from arcs, we will use the CG of the nodes     */
                var numRefCGs = 0;
                var numMovingCGs = 0;
                var movingCG = new double[3];
                var refCG = new double[3];
                foreach (var a in connectingArcs)
                {
                    var n = a.To;
                    var index = n.localVariables.FindIndex(x => x == Constants.TRANSLATION);
                    if (index != -1)
                    {
                        if (refNodes.Contains(n))
                        {
                            numRefCGs++;
                            refCG = StarMath.add(refCG,
                                new[] { n.localVariables[++index], n.localVariables[++index], n.localVariables[++index] },
                                3);
                        }
                        else
                        {
                            numMovingCGs++;
                            movingCG = StarMath.add(movingCG,
                                new[] { n.localVariables[++index], n.localVariables[++index],n.localVariables[++index] },
                                3);
                        }
                    }
                    n = a.From;
                    index = n.localVariables.FindIndex(x => x == Constants.TRANSLATION);
                    if (index != -1)
                    {
                        if (refNodes.Contains(n))
                        {
                            numRefCGs++;
                            refCG = StarMath.add(refCG,
                                new[] { n.localVariables[++index], n.localVariables[++index], n.localVariables[++index] },
                                3);
                        }
                        else
                        {
                            numMovingCGs++;
                            movingCG = StarMath.add(movingCG,
                                new[] { n.localVariables[++index], n.localVariables[++index], n.localVariables[++index] },
                                3);
                        }
                    }
                }
                refCG = StarMath.divide(refCG, numRefCGs, 3);
                movingCG = StarMath.divide(movingCG, numMovingCGs, 3);
                installDirection.Position = StarMath.subtract(movingCG, refCG, 3);
                installDirection.NormalizeInPlace();
            }
            if (double.IsNaN(installDirection.Position[0]))
            {
                installDirection.Position = new[] {1.0, 0.0, 0.0};
                SearchIO.output("unable to find install direction between parts",3);
            }

            // now, we have to figure out how much to move.
            // foreach arc, we find the point of the cvx hull of the reference node that is farthest along the install direction,
            // call it rmv (Referenc Max Value)
            // then we find the lowest value of the moving cvx hull point along this install direction,
            // call it mmv (Moving Min Value). 
            // The difference, delta, is the amount of movument to clear one part from the other.
            // we take the max delta from all interstitial arcs and multiply it by the install direction
            insertionDistance = double.NegativeInfinity;
            foreach (var arc in connectingArcs)
            {
                var fromNode = arc.From;
                var toNode = arc.To;
                ConvexHull<Vertex, DefaultConvexFace<Vertex>> refHull, movingHull;
                if (refNodes.Contains(fromNode))
                {
                    refHull = convexHullForParts[fromNode.name];
                    movingHull = convexHullForParts[toNode.name];
                }
                else
                {
                    refHull = convexHullForParts[toNode.name];
                    movingHull = convexHullForParts[fromNode.name];
                }
                var refMaxValue = STLGeometryFunctions.findMaxPlaneHeightInDirection(refHull.Points, installDirection);

                var movingMinValue = STLGeometryFunctions.findMinPlaneHeightInDirection(movingHull.Points, installDirection);

                var distance = refMaxValue - movingMinValue;
                if (insertionDistance < distance) insertionDistance = distance;
            }
            return installDirection;
        }

        public static List<DefaultConvexFace<Vertex>> UnchangedRefFacesDuringInstallation(SubAssembly newSubAsm, Vector insertionDirection)
        {
            var notAffectedFacesInCom = newSubAsm.Install.Reference.CVXHull.Faces.ToList();

            var halfOfRefCvh = new List<DefaultConvexFace<Vertex>>();
            foreach (var f in newSubAsm.Install.Reference.CVXHull.Faces)
            {
                var dotProduct = StarMath.dotProduct(f.Normal, insertionDirection.Position, 3);
                if (Math.Acos(dotProduct) > (Math.PI / 2))
                    halfOfRefCvh.Add(f);
            }
            foreach (var eachFace in halfOfRefCvh)
            {
                foreach (var eachMovingVerticies in newSubAsm.Install.Moving.CVXHull.Points)
                {
                    var ray = new Ray(eachMovingVerticies, insertionDirection);
                    var faceAffected = STLGeometryFunctions.RayIntersectsWithFace(ray, eachFace);
                    if (faceAffected)
                        notAffectedFacesInCom.Remove(eachFace);
                }
            }
            return notAffectedFacesInCom;
        }

        private static void FindAdgacentFaces()
        {
            foreach (var firstFace in nFFList)
            {
                foreach (var secondFace in nFFList.Where(secondFace => secondFace.ExCoVer.Any(
                    eachPointOfSecondFace => firstFace.ExCoVer
                        .Contains(eachPointOfSecondFace))))
                {
                    firstFace.Adjacents.Add(secondFace.Name);
                    break;
                }
            }
        }


        private static double InitialEvaluation(SubAssembly newSubAsm, double[] installDirection, Vector insertionDirection, List<node> refNodes, List<node> movingNodes, AssemblyCandidate c)
        {
            nFFList.Clear();
            newRefCVHFacesInCom.Clear();
            FindAdgacentFaces();
            // We need the faces in reference which are not hit by moving during insertion:
            var unChangedFaces = UnchangedRefFacesDuringInstallation(newSubAsm, insertionDirection);
            // Now merge the faces with the angle less than ??? to make a larger face
            mergingFacesWithSemiParallelNormals(unChangedFaces);
            // Unique name for each new merged face
            for (var i = 0; i < nFFList.Count; i++)
            {
                nFFList[i].Name = i + 1;
            }
            //Finding unique external verticies
            foreach (var t in nFFList)
            {
                t.ExVer = new List<Vertex>();
                var coVer = t.ExCoVer;
                t.ExVer.Add(coVer[0]);
                for (var i = 1; i < coVer.Count; i++)
                {
                    if (!t.ExVer.Contains(coVer[i]))
                        t.ExVer.Add(coVer[i]);
                }
            }
            //double timeScore = timeEvaluation(newSubAsm, refNodes, movingNodes);
            c.TimeScore = TimeEvaluation(newSubAsm, refNodes, movingNodes);
            //double accessScore = AccessabilityEvaluatio(insertionDirection);
            c.AccessibilityScore = AccessabilityEvaluation(installDirection);
            //double stabilityScore = StabilityEvaluation(newSubAsm, insertionDirection);
            c.StabilityScore = StabilityEvaluation(newSubAsm);
            insertionDirectionDic.Add(newSubAsm.Name, insertionDirection);
            //return timeScore + accessScore + stabilityScore;
            return c.TimeScore + c.AccessibilityScore + c.StabilityScore;
        }

        private static double StabilityEvaluation(SubAssembly newSubAsm)
        {
            //return 8.0;
            double r = 0.0;
            foreach (var newFa in nFFList)
            {
                double SI = CheckStabilityForReference(newSubAsm, newFa);
                r += SI;
            }
            //var r = nFFList.Sum(a => a.RefS) / nFFList.Count;
            r = r / nFFList.Count;
            return r;
        }

        public static double AccessabilityEvaluation(double[] installDirection)
        {
            //return 8.0;
            double r = 0.0;

            foreach (var f in nFFList)
            {
                double AI = CheckAccessability(installDirection, f);
                r += AI;
            }
            //r = nFFList.Sum(a => a.IC) / nFFList.Count;
            r = r/nFFList.Count;
            return r;
        }

        private static double TimeEvaluation(SubAssembly newSubAsm, List<node> refNodes, List<node> movingNodes)
        {
            // This should be s.th. like equal moving and reference assembly time. I must try to find a way to 
            // estimate these two times. Better score for a subassembly with Ref time = moving Time;
            // To do s.th. for OneByOne, I will only consider number of arcs and nodes to evaluate time.
            float time1 = refNodes.Count;
            float time2 = movingNodes.Count;
            double r = 1 - Math.Abs((time1 - time2) / (time2 + time1));
            return 10 * r;
        }

        public static double CheckStabilityForReference(SubAssembly newSubAsm, NewFootprintFaces f)
        {
            var refCOM = newSubAsm.Install.Reference.CenterOfMass;
            // For calculating the projection of COM on current New face, we have normal,
            // we have the position of COM and we have points on the edge. 
            // First We are looking for the parameters in this plane equation:
            // a(x-x0) + b(y-y0) + c(z-z0) = 0 plane equation or "ax + bY + cz = d"
            var x0 = f.ExCoVer[0].Position[0];
            var a = f.Normal[0];
            var y0 = f.ExCoVer[0].Position[1];
            var b = f.Normal[1];
            var z0 = f.ExCoVer[0].Position[2];
            var c = f.Normal[2];
            var d = x0 * a + y0 * b + z0 * c;
            var u = refCOM.Position[0];
            var v = refCOM.Position[1];
            var w = refCOM.Position[2];
            var t0 = -(a * u + b * v + c * w + d) / (a * a + b * b + c * c);

            f.COMP = new Vertex(0,0,0);
            f.COMP.Position[0] = u + a * t0; // x element
            f.COMP.Position[1] = v + b * t0; // y element
            f.COMP.Position[2] = w + c * t0; // z element

            for (var i = 0; i < f.ExCoVer.Count; i += 2)
            {
                // Checking if COM is inside or outside
                var fromPoint = f.ExCoVer[i];
                var toPoint = f.ExCoVer[i + 1];
                var currentEdge = StarMath.subtract(toPoint.Position, fromPoint.Position, 3);
                var arcToComProj = StarMath.subtract(f.COMP.Position, fromPoint.Position, 3);
                var arbitraryPoint = new Vertex(0, 0, 0);
                foreach (var eachVertex in f.ExVer.Where(q => q != fromPoint && q != toPoint))
                {
                    arbitraryPoint = eachVertex;
                    break;
                }
                var arcToCArbitrary = StarMath.subtract(arbitraryPoint.Position, fromPoint.Position, 3);
                var crProduct1 = StarMath.crossProduct(currentEdge, arcToCArbitrary);
                var crProduct2 = StarMath.crossProduct(currentEdge, arcToComProj);

                if (!(Math.Acos(StarMath.dotProduct(crProduct1, crProduct2, 3)) >
                      (Math.PI / 10))) continue;
                f.IsComInsideFace = false;
                break;
            }

            f.MinDisNeaEdg = double.PositiveInfinity;
            var x02 = f.COMP.Position[0];
            var y02 = f.COMP.Position[1];
            var z02 = f.COMP.Position[2];
            f.Hight = Math.Sqrt((Math.Pow((x02 - refCOM.Position[0]), 2)) +
                                (Math.Pow((y02 - refCOM.Position[1]), 2)) + (Math.Pow((z02 - refCOM.Position[2]), 2)));
            for (var i = 0; i < f.ExCoVer.Count; i += 2)
            {
                //i as the node 1 and i+1 as node 2
                var x1 = f.ExCoVer[i].Position[0];
                var y1 = f.ExCoVer[i].Position[1];
                var z1 = f.ExCoVer[i].Position[2];
                var x2 = f.ExCoVer[i + 1].Position[0];
                var y2 = f.ExCoVer[i + 1].Position[1];
                var z2 = f.ExCoVer[i + 1].Position[2];
                var a2 = (y2 - y02) * (z2 - z1) - (z2 - z02) * (y2 - y1);
                var b2 = (z2 - z02) * (x2 - x1) - (x2 - x02) * (z2 - z1);
                var c2 = (x2 - x02) * (y2 - y2) - (y2 - y02) * (x2 - x1);
                var distanceOfEdgeToComProj = Math.Sqrt((Math.Pow(a2, 2) + Math.Pow(b2, 2) + Math.Pow(c2, 2)) /
                                                        (Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2) +
                                                         Math.Pow(z2 - z1, 2)));
                if (distanceOfEdgeToComProj < f.MinDisNeaEdg)
                {
                    f.MinDisNeaEdg = distanceOfEdgeToComProj;
                }
            }
            var spf = Math.Pow(((2 / Math.PI) * (Math.Atan(f.Hight / f.MinDisNeaEdg))), 2);
            f.RefS = 10 - (10 * spf);
            return f.RefS;
        }

        public static double CheckAccessability(double[] installDirection, NewFootprintFaces f)
        {
            var angleBetweenInsDirAndNormal = Math.Acos(StarMath.dotProduct(installDirection, f.Normal, 3));
            var radToDeg = angleBetweenInsDirAndNormal * (180 / Math.PI);
            for (var i = 0; i < 11; i++)
            {
                if (!(radToDeg > (180 - (180 / (11 - i))) - 1)) continue;
                f.IC = i;
                break;
            }
            return f.IC;
        }


        public static void mergingFacesWithSemiParallelNormals(List<DefaultConvexFace<Vertex>> unChangedFaces)
        {
            for (var c = 0; c < unChangedFaces.Count; c++)
            {
                var ini = new NewFootprintFaces();
                nFFList.Add(ini);
                var normal2 = unChangedFaces[c].Normal;
                nFFList[c].Normal = normal2;
                nFFList[c].ExCoVer = new List<Vertex>();
                nFFList[c].Faces = new List<DefaultConvexFace<Vertex>>();
                for (var i = 0; i < 3; i++)
                {
                    for (var j = i + 1; j < 3; j++)
                    {
                        nFFList[c].ExCoVer.Add(unChangedFaces[c].Vertices[i]);
                        nFFList[c].ExCoVer.Add(unChangedFaces[c].Vertices[j]);
                    }
                }
                for (var n = 0; n < unChangedFaces.Count; n++)
                {
                    var normal1 = unChangedFaces[n].Normal;
                    var normalsDP = StarMath.dotProduct(normal1, normal2, 3);
                    if (!(Math.Acos(normalsDP) < Math.PI / 18) || !(Math.Acos(normalsDP) > -Math.PI / 18)) continue;
                    nFFList[c].Faces.Add(unChangedFaces[n]);
                    nFFList[c].Normal = StarMath.add(normal1, nFFList[c].Normal, 3);
                    for (var i = 0; i < 3; i++)
                    {
                        for (var j = i + 1; j < 3; j++)
                        {
                            nFFList[c].ExCoVer.Add(unChangedFaces[n].Vertices[i]);
                            nFFList[c].ExCoVer.Add(unChangedFaces[n].Vertices[j]);

                            for (var k = 0; k < nFFList[c].ExCoVer.Count - 2; k += 2)
                            {
                                if (nFFList[c].ExCoVer[k] == unChangedFaces[n].Vertices[i] &&
                                    nFFList[c].ExCoVer[k + 1] == unChangedFaces[n].Vertices[j])
                                {
                                    nFFList[c].ExCoVer.Remove(unChangedFaces[n].Vertices[i]);
                                    nFFList[c].ExCoVer.Remove(unChangedFaces[n].Vertices[j]);
                                    break;
                                }
                                if (nFFList[c].ExCoVer[k] != unChangedFaces[n].Vertices[j] ||
                                    nFFList[c].ExCoVer[k + 1] != unChangedFaces[n].Vertices[i]) continue;
                                nFFList[c].ExCoVer.Remove(unChangedFaces[n].Vertices[i]);
                                nFFList[c].ExCoVer.Remove(unChangedFaces[n].Vertices[j]);
                                break;
                            }
                        }
                    }
                    unChangedFaces.Remove(unChangedFaces[n]);
                }

            }
        }
    }
}





