using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth.Representation;
using MIConvexHull;
using Assembly_Planner.GraphSynth.BaseClasses;
using StarMathLib;
using TVGL;
using Constants = AssemblyEvaluation.Constants;

namespace Assembly_Planner
{
    /// <summary>
    /// Class EvaluationForBinaryTree - this is a stub for evaluating a particular install step
    /// </summary>
    internal class EvaluationForBinaryTree
    {
        internal static Dictionary<string, TVGLConvexHull> ConvexHullsForParts = new Dictionary<string, TVGLConvexHull>();

        internal EvaluationForBinaryTree(Dictionary<string, List<TessellatedSolid>> subAssems)
        {
            ConvexHullsForParts = NonadjacentBlockingWithPartitioning.CombinedCVHForMultipleGeometries;
        }
        /// <summary>
        /// Evaluates the subassemly.
        /// </summary>
        /// <param name="subassemblyNodes">The subassembly nodes - all the nodes in the combined install action.</param>
        /// <param name="optNodes">The subset of nodes that represent one of the two parts in the install step.</param>
        /// <param name="sub">The sub is the class that is then tied into the treequence.</param>
        /// <returns>System.Double.</returns>
        public double EvaluateSub(designGraph graph, HashSet<Component> subassemblyNodes, List<Component> optNodes, HashSet<int> validDirs, out SubAssembly sub)
        {

            var rest = subassemblyNodes.Where(n => !optNodes.Contains(n)).ToList();
            sub = Update(optNodes, rest);
            var connectingArcs =
                graph.arcs.Where(a => a is Connection).Cast<Connection>().Where(a => ((optNodes.Contains(a.To) && rest.Contains(a.From))
                                                            || (optNodes.Contains(a.From) && rest.Contains(a.To))))
                    .ToList();
            var insertionDirections = InsertionDirectionsFromRemovalDirections(sub, optNodes, validDirs);
            var minDis = double.PositiveInfinity;
            var bestInstallDirection = new[] { 0.0, 0.0, 0.0 };
            foreach (var dir in insertionDirections)
            {
                var dis = DetermineDistanceToSeparateHull(sub, dir); /// Bridge.MeshMagnifier;
                if (dis >= minDis) continue;
                minDis = dis;
                bestInstallDirection = dir;
            }
            sub.Install.InstallDirection = bestInstallDirection;
            sub.Install.InstallDistance = minDis;
            // I need to add InstallationPoint also

            var install = new[] { rest, optNodes };
            if (EitherRefOrMovHasSeperatedSubassemblies(install, subassemblyNodes))
                return -1;

            var moivingnames = sub.Install.Moving.PartNames;
            var refnames = sub.Install.Reference.PartNames;
            var movingnodes = subassemblyNodes.Where(n => moivingnames.Contains(n.name)).ToList();
            var refnodes = subassemblyNodes.Where(n => refnames.Contains(n.name)).ToList();

            /////time
            sub.Install.Time = 10;
            EvaluateSubFirstThreeTime(sub, refnodes, movingnodes, minDis, out sub.Install.Time, out sub.Install.TimeSD);


            ///////stability
            var stabilityscore = EvaluateSubStable(graph, refnodes, movingnodes, sub);
            //sub.Secure.Fasteners = ConnectingArcsFastener(connectingArcs);
            return 1;
        }
        private double EvaluateSubStable(designGraph graph, List<Component> optNodes, List<Component> rest, SubAssembly sub)
        {

            ////debug
            //var refnodenames = new List<string>
            //{
            //   //  "Assem1 - Part4",
            //   //"Assem1 - Part2",
            //   // "Assem1 - Part1",
            //   //  "Assem1 - b-1",
            //  //  "Assem1 - a-1",

            //      "sim1 - L",
            //   "sim1 - B",
            //};
            //var movnodenames = new List<string>
            //{
            //    // "Assem1 - Part3",

            //       // "Assem1 - c-1",
            //    //"Assem1 - Part4",
            //    //  "Assem1 - Part3",
            //  "sim1 - P",
            //    "sim1 - R",
            //     "sim1 - S",
            //};
            //optNodes.Clear();
            //rest.Clear(); ;
            //var refnodes = new List<node>();
            //var movnodes = new List<node>();
            ////  var refarcs = new List<arc>();

            //foreach (var refname in refnodenames)
            //{
            //    optNodes.Add(graph.nodes.
            //        Where(n => n is Component).Cast<Component>().ToList().
            //        Find(x => x.name.StartsWith(refname)));
            //}

            //foreach (var movename in movnodenames)
            //{
            //    rest.Add(graph.nodes.
            //        Where(n => n is Component).Cast<Component>().ToList().
            //        Find(x => x.name.StartsWith(movename)));
            //}

            //foreach (var arc in graph.arcs.Where(a => a is arc))
            //{
            //    if (rest.Exists(a => a.name.Equals(arc.From.name) || a.name.Equals(arc.To.name)))
            //    {
            //        continue;
            //    }
            //    refarcs.Add(arc);
            //}

            //debug

            var subassembly = new List<Component>();
            var othersubassembly = new List<Component>();


            double bothsubstatiblit = 0;
            double finalscore = 0;


            for (int i = 0; i < 2; i++)//check moving and reference
            {


                if (i == 0)
                {

                    subassembly = optNodes;
                    othersubassembly = rest;

                }
                else
                {
                    subassembly = rest;
                    othersubassembly = optNodes;
                }


                var overallstatiblity = 0.0;

                var ss = EvaluationForBinaryTree.CreateCombinedConvexHull2(subassembly);
                var reductedfaces = AssemblyEvaluator.MergingFaces(ss.Faces.ToList());
                var maxsigleDOF = 0.0;
                var minsigleSB = double.PositiveInfinity;
                double totaldof = 0;
                double totalSBscore = 0;

                foreach (var refnode in subassembly)
                {
                    bool isanyfastener = false;
                    double stability = 0;
                    var alldof = new double[12];
                    if (refnode.name.StartsWith("sim1 - S"))
                    {
                        var er = 1;
                    }
                    var othernodes = subassembly.Where(n => !n.name.Equals(refnode.name)).ToList();
                    var refarcs =
                        graph.arcs
                            .Where(a => (a.XmlFrom.Equals(refnode.name) && othernodes.Exists(on => on.name.Equals(a.XmlTo)))
                            || ((a.XmlTo.Equals(refnode.name) && othernodes.Exists(on => on.name.Equals(a.XmlFrom)))
                            )).ToList();

                    if (subassembly.Count == 1)
                    {
                        totaldof = 11.5;
                        totalSBscore = 9.8;// TBD
                        maxsigleDOF = 11.5;
                        minsigleSB = 9.8;
                        continue;

                    }
                    foreach (Connection carc in refarcs.Where(a => a is Connection))
                    {
                        if (carc.Fasteners.Count != 0)
                        {
                            isanyfastener = true;
                            stability = 9.8;
                            break;
                        }
                    }
                    if (isanyfastener == false)
                        alldof = Stabilityfunctions.GetDOF(refnode, refarcs);

                    var linearDOF = new double[6];
                    var rotateDOF = new double[6];
                    var sumlinearDOF = new double();
                    var sumrotateDOF = new double();
                    for (int k = 0; k < 6; k++)
                    {
                        linearDOF[i] = alldof[k];
                        rotateDOF[i] = alldof[6 + k];
                        sumlinearDOF += alldof[k];
                        sumrotateDOF += alldof[6 + k];
                    }
                    foreach (var d in alldof)
                    {
                        totaldof += d;
                    }
                    if (totaldof > maxsigleDOF)
                    {
                        maxsigleDOF = totaldof;
                    }
                    //cheke stability
                    var mindir = new double[3];
                    if ((sumlinearDOF <= 1 && sumrotateDOF <= 1) || isanyfastener == true)//need new if only 1 DOF or 1 roitate DOF
                    {
                        stability = 9.8; //50??
                    }
                    else
                    {
                        var removalindex = Stabilityfunctions.GetSubPartRemovealDirectionIndexs(refnode, refarcs);
                        var Gdirection = new double[3];
                        ///perpendicular to the ground. TBD
                        foreach (var indxe in removalindex)
                        {
                            Gdirection = Gdirection.add(DisassemblyDirections.Directions[indxe]);
                        }
                        var tofaceNormal = Gdirection.normalize().multiply(-1);

                        var totallinear = alldof[0] + alldof[1] + alldof[2] + alldof[3] + alldof[4] + alldof[5];
                        var totalrotate = alldof[6] + alldof[7] + alldof[8] + alldof[9] + alldof[10] + alldof[11];
                        var selected = new double[3];
                        // stability = RotateBeforeTreeSearch.Getstabilityscore(refnode, refarcs, reduceface.Normal, out mindir);
                        stability = Stabilityfunctions.Getstabilityscore(refnode, refarcs, tofaceNormal, out mindir, out selected);
                        if (stability == -9.8)//at least one part can tip without any acceleration
                            sub.InternalStabilityInfo.needfixture = true;
                        //minimum acceleration to tip a part
                    }
                    if (stability > 9.9)
                    {
                        var tt = 1;
                    }
                    if (stability < minsigleSB)
                    {
                        minsigleSB = stability;
                    }
                    totalSBscore += stability;
                    Console.WriteLine(stability);
                    Console.WriteLine("{0} {1} {2}", mindir[0], mindir[1], mindir[2]);
                    //   overallstatiblity += partialminstatiblity;
                }
                // bothsubstatiblit += overallstatiblity;
                //var s1 = totaldof / subassembly.Count + maxsigleDOF;
                //var s2 = minsigleSB + totalSBscore / subassembly.Count;
                var s1 = totaldof / subassembly.Count + maxsigleDOF - (minsigleSB + totalSBscore / subassembly.Count);
                finalscore += totaldof / subassembly.Count + maxsigleDOF - (minsigleSB + totalSBscore / subassembly.Count); //the less the better

                if (i == 1)
                {
                    UpdateInternalStabilityInfo(sub, totaldof, maxsigleDOF, totalSBscore, minsigleSB);
                    sub.InternalStabilityInfo.Totalsecore = finalscore;
                }
            }
            return finalscore;
        }
        private void UpdateInternalStabilityInfo(SubAssembly sub, double totaldof, double maxsigleDOF, double totalSBscore, double minsigleSB)
        {

            sub.InternalStabilityInfo.OverAllDOF = totaldof;
            sub.InternalStabilityInfo.MaxSingleDOF = maxsigleDOF;
            sub.InternalStabilityInfo.OverAllTippingScore = totalSBscore;
            sub.InternalStabilityInfo.MinSingleTippingScore = minsigleSB;
        }
        public void EvaluateSubFirstThreeTime(SubAssembly sub, List<Component> refnodes, List<Component> movingnodes, double olpdis, out double totaltime, out double totalSD)
        {
            var movingsolids = new List<TessellatedSolid>();
            var referencesolids = new List<TessellatedSolid>();
            var movingvertex = new List<Vertex>();

            ///////
            ///moving
            ///////
            foreach (var n in movingnodes)
            {
                movingsolids.Add(Program.solids[n.name][0]); //cheange bbbbb class
            }
            foreach (var s in movingsolids)
            {
                foreach (var v in s.Vertices)
                {
                    movingvertex.Add(v);
                }

            }
            var movingOBB = OBB.BuildUsingPoints(movingvertex);
            var lengths = new double[]
            {
                StarMath.norm1(movingOBB.CornerVertices[0].Position.subtract(movingOBB.CornerVertices[1].Position)),
                StarMath.norm1(movingOBB.CornerVertices[2].Position.subtract(movingOBB.CornerVertices[1].Position)),
                StarMath.norm1(movingOBB.CornerVertices[0].Position.subtract(movingOBB.CornerVertices[4].Position)),
            };
            var lmax = lengths.Max();
            var lmin = lengths.Min();
            double lmid = lengths.Average() * 3 - lmax - lmin;
            var movingweight = Math.Log(sub.Install.Moving.Mass / 1000);//in g ???????????
            var OBBvolue = Math.Log(movingOBB.Volume / 1000);
            var OBBmaxsurface = Math.Log(lmax * lmid / 1000);
            var OBBminsurface = Math.Log(lmin * lmid / 1000);
            /////
            /// distance TBD
            var movingdistance = 500;

            var movingdis = Math.Log(movingdistance);
            var movinginput = new double[5] { movingweight, OBBvolue, OBBmaxsurface, OBBminsurface, movingdis };
            //    log ( weight, OOBvol OBB, maxf OBB, minf moving, distance  )all in mmgs unit system 
            double movingtime, movingSD;
            GPprocess.CalculateAssemblyTimeAndSD.GetTimeAndSD(movinginput, "moving", out movingtime, out movingSD);
            ///////
            ///install
            ///////
            var allolpfs = BlockingDetermination.OverlappingSurfaces.FindAll(
           s =>
               (movingnodes.Any(n => n.name.Equals(s.Solid1.Name))) && (refnodes.Any(n => n.name.Equals(s.Solid2.Name)))
               || (movingnodes.Any(n => n.name.Equals(s.Solid2.Name))) && (refnodes.Any(n => n.name.Equals(s.Solid1.Name)))
               );
            int olpfeatures = 0;
            for (int i = 0; i < allolpfs.Count; i++)
            {
                olpfeatures += allolpfs[i].Overlappings.Count;////not alignment features for now;
            }
            var installinput = new double[6] { movingweight, OBBvolue, OBBmaxsurface, OBBminsurface, olpdis, olpfeatures };
            double installtime, instalSD;
            GPprocess.CalculateAssemblyTimeAndSD.GetTimeAndSD(installinput, "install", out installtime, out instalSD);
            ///////
            ///rotate
            ///////
            totaltime = installtime + movingtime;
            totalSD = instalSD + instalSD;


            var sss = 1;
        }
        private HashSet<Double[]> InsertionDirectionsFromRemovalDirections(SubAssembly sub, List<Component> optNodes,
            HashSet<int> validDirs)
        {
            // Note: The hashset of validDirs are the removal directions to remove optNodes from the rest.
            //       If the optNodes is the reference, then the installDirections are the same as validDirs
            //       If the optNodes is the moving, then the InstallDirection is the opposite of the validDirs

            var dirs = new HashSet<Double[]>();
            if (sub.Install.Reference.PartNames.All(optNodes.Select(n => n.name).ToList().Contains) &&
                optNodes.Select(n => n.name).ToList().All(sub.Install.Reference.PartNames.Contains))
            {
                foreach (var dir in validDirs)
                    dirs.Add(DisassemblyDirections.Directions[dir]);
                return dirs;
            }
            foreach (var dir in validDirs)
                dirs.Add(DisassemblyDirections.Directions[dir].multiply(-1.0));
            return dirs;
        }

        private List<Fastener> ConnectingArcsFastener(List<Connection> connectingArcs)
        {
            var fasteners = new List<Fastener>();
            foreach (var arc in connectingArcs)
                foreach (var fas in arc.Fasteners.Where(a => !fasteners.Contains(a)))
                    fasteners.Add(fas);
            return fasteners;
        }

        /// <summary>
        /// returns the subassembly class given the two lists of components
        /// </summary>
        /// <param name="opt">The opt.</param>
        /// <param name="rest">The rest.</param>
        /// <returns>SubAssembly.</returns>
        public SubAssembly Update(List<Component> opt, List<Component> rest)
        {
            //todo: change List to HashSet
            Part refAssembly, movingAssembly;
            var movingNodes = opt;
            var newSubAsmNodes = rest;
            if (movingNodes.Count == 1)
            {
                var nodeName = movingNodes[0].name;
                movingAssembly = new SubAssembly(new HashSet<Component>(movingNodes), ConvexHullsForParts[nodeName], movingNodes[0].Mass,
                    movingNodes[0].Volume, new Vertex(movingNodes[0].CenterOfMass));
                //movingAssembly = new Part(nodeName, movingNodes[0].Volume, movingNodes[0].Volume,
                //    ConvexHullsForParts[nodeName], new Vertex(movingNodes[0].CenterOfMass));
            }
            else
            {
                var combinedCVXHullM = CreateCombinedConvexHull2(movingNodes);
                var VolumeM = GetSubassemblyVolume(movingNodes);
                var MassM = GetSubassemblyMass(movingNodes);
                var centerOfMass = GetSubassemblyCenterOfMass(movingNodes);
                movingAssembly = new SubAssembly(new HashSet<Component>(movingNodes), combinedCVXHullM, MassM, VolumeM, centerOfMass);
            }

            var referenceHyperArcnodes = new List<Component>();
            referenceHyperArcnodes = (List<Component>)newSubAsmNodes.Where(a => !movingNodes.Contains(a)).ToList();
            if (referenceHyperArcnodes.Count == 1)
            {
                var nodeName = referenceHyperArcnodes[0].name;
                refAssembly = new SubAssembly(new HashSet<Component>(referenceHyperArcnodes), ConvexHullsForParts[nodeName],
                    referenceHyperArcnodes[0].Mass, referenceHyperArcnodes[0].Volume,
                    new Vertex(referenceHyperArcnodes[0].CenterOfMass));
                //refAssembly = new Part(nodeName, referenceHyperArcnodes[0].Mass, referenceHyperArcnodes[0].Volume,
                //    ConvexHullsForParts[nodeName],
                //   new Vertex(referenceHyperArcnodes[0].CenterOfMass));
            }
            else
            {
                var combinedCVXHullR = CreateCombinedConvexHull2(referenceHyperArcnodes);
                var VolumeR = GetSubassemblyVolume(referenceHyperArcnodes);
                var MassR = GetSubassemblyMass(referenceHyperArcnodes);
                var centerOfMass = GetSubassemblyCenterOfMass(referenceHyperArcnodes);
                refAssembly = new SubAssembly(new HashSet<Component>(referenceHyperArcnodes), combinedCVXHullR, MassR, VolumeR, centerOfMass);
            }
            var combinedCvxHull = CreateCombinedConvexHull(refAssembly.CVXHull, movingAssembly.CVXHull);
            List<PolygonalFace> movingFacesInCombined;
            List<PolygonalFace> refFacesInCombined;
            var InstallCharacter = shouldReferenceAndMovingBeSwitched(refAssembly, movingAssembly, combinedCvxHull,
                out refFacesInCombined, out movingFacesInCombined);
            if ((int)InstallCharacter < 0)
            {
                var tempASM = refAssembly;
                refAssembly = movingAssembly;
                movingAssembly = tempASM;
                refFacesInCombined = movingFacesInCombined; // no need to use temp here, as the movingFaces in the 
                // combined convex hull are not needed.
                InstallCharacter = (InstallCharacterType)(-((int)InstallCharacter));
            }
            var newSubassembly = new SubAssembly(refAssembly, movingAssembly, combinedCvxHull, InstallCharacter,
                refFacesInCombined);
            newSubassembly.CenterOfMass = CombinedCenterOfMass(newSubassembly);
            return newSubassembly;
        }

        private Vertex CombinedCenterOfMass(SubAssembly newSubassembly)
        {
            return
                new Vertex(new[]{(newSubassembly.Install.Moving.CenterOfMass.Position[0] +
                     newSubassembly.Install.Reference.CenterOfMass.Position[0]) / 2,
                    (newSubassembly.Install.Moving.CenterOfMass.Position[1] +
                     newSubassembly.Install.Reference.CenterOfMass.Position[1]) / 2,
                    (newSubassembly.Install.Moving.CenterOfMass.Position[2] +
                     newSubassembly.Install.Reference.CenterOfMass.Position[2]) / 2});
        }
        private TVGLConvexHull CreateCombinedConvexHull(TVGLConvexHull refCVXHull, TVGLConvexHull movingCVXHull)
        {
            var pointCloud = new List<Vertex>(refCVXHull.Vertices);
            pointCloud.AddRange(movingCVXHull.Vertices);
            return new TVGLConvexHull(pointCloud);
        }

        public static TVGLConvexHull CreateCombinedConvexHull2(List<Component> nodes)
        {
            var pointCloud = new List<Vertex>();
            foreach (var n in nodes)
            {
                var nodeName = n.name;
                pointCloud.AddRange(ConvexHullsForParts[nodeName].Vertices);
            }
            return new TVGLConvexHull(pointCloud);
        }

        private double GetSubassemblyVolume(List<Component> nodes)
        {
            return nodes.Sum(n => n.Volume);
        }

        private double GetSubassemblyMass(List<Component> nodes)
        {
            return nodes.Sum(n => n.Mass);
        }
        private Vertex GetSubassemblyCenterOfMass(List<Component> nodes)
        {
            var sumMx = 0.0;
            var sumMy = 0.0;
            var sumMz = 0.0;
            var M = 0.0;
            foreach (var n in nodes)
            {
                var m = n.Mass;
                var nCOM = n.CenterOfMass;
                sumMx += nCOM[0] * m;
                sumMy += nCOM[1] * m;
                sumMz += nCOM[2] * m;
                M += m;
            }

            return new Vertex(new[] { sumMx / M, sumMy / M, sumMz / M });
        }

        private InstallCharacterType shouldReferenceAndMovingBeSwitched(Part refAssembly, Part movingAssembly,
            TVGLConvexHull combinedCVXHull,
            out List<PolygonalFace> refFacesInCombined,
            out List<PolygonalFace> movingFacesInCombined)
        {
            /* first, create a list of vertices from the reference hull that are present in the combined hull.
             * likewise, with the moving. */
            var refVertsInCombined = new List<Vertex>();
            var movingVertsInCombined = new List<Vertex>();
            foreach (var pt in combinedCVXHull.Vertices)
            {
                if (refAssembly.CVXHull.Vertices.Contains(pt)) refVertsInCombined.Add(pt);
                else
                {
                    /* this additional Contains function is unnecessary and potential time-consuming. 
                     * It was implemented for initial validiation, but it is commented out now. 
                    if (!movingAssembly.CVXHull.Points.Contains(pt)) 
                        throw new Exception("The point is in neither original part!");  */
                    movingVertsInCombined.Add(pt);
                }
            }
            /* If none of the combined vertices are from the moving, we can end this function early and save time. */
            if (movingVertsInCombined.Count == 0)
            {
                movingFacesInCombined = null;
                refFacesInCombined = new List<PolygonalFace>(combinedCVXHull.Faces);
                return InstallCharacterType.MovingIsInsideReference;
            }
            /* ...likewise for the original reference */
            if (refVertsInCombined.Count == 0)
            {
                refFacesInCombined = null;
                movingFacesInCombined = new List<PolygonalFace>(combinedCVXHull.Faces);
                return InstallCharacterType.ReferenceIsInsideMoving;
            }
            /* we could just count the number of vertices, but that would not be as accurate a prediction
             * as the area of the faces */
            refFacesInCombined = new List<PolygonalFace>();
            movingFacesInCombined = new List<PolygonalFace>();
            double refFaceArea = 0.0;
            var movingFaceArea = 0.0;
            var totalFaceArea = 0.0;
            foreach (var face in combinedCVXHull.Faces)
            {
                var faceArea = findFaceArea(face);
                totalFaceArea += faceArea;
                if (face.Vertices.All(v => refAssembly.CVXHull.Vertices.Contains(v)))
                {
                    refFacesInCombined.Add(face);
                    refFaceArea += faceArea;
                }
                else if (face.Vertices.All(v => movingAssembly.CVXHull.Vertices.Contains(v)))
                {
                    movingFacesInCombined.Add(face);
                    movingFaceArea += faceArea;
                }
            }
            /* former faces is the sum areas of faces from prior cvx hulls */
            var formerFacesArea = refFaceArea + movingFaceArea;
            /* if the former face area does not take up a significant portion of 
             * the new faces then we do not have the confidence to make the judgement
             * based on this fact. */
            if (formerFacesArea / totalFaceArea > Constants.Values.CVXFormerFaceConfidence)
            {
                /* there are two check here: if the common area is very small, we assume the 
                 * subassembly is inside the other. If not, maybe it is more on the outside
                 * but a smaller effect on resulting convex hull. */
                if (refFaceArea / formerFacesArea < Constants.Values.CVXOnInsideThreshold)
                    return InstallCharacterType.ReferenceIsInsideMoving;
                if (movingFaceArea / formerFacesArea < Constants.Values.CVXOnInsideThreshold)
                    return InstallCharacterType.MovingIsInsideReference;
                if (refFaceArea / formerFacesArea < Constants.Values.CVXOnOutsideThreshold)
                    return InstallCharacterType.ReferenceIsOnOutsideOfMoving;
                if (movingFaceArea / formerFacesArea < Constants.Values.CVXOnOutsideThreshold)
                    return InstallCharacterType.MovingIsOnOutsideOfReference;
            }
            /* if we cannot confidently use face area then we switch to comparing
             * the magnitudes of the moment of inertia. */
            else
            {
                if (refAssembly.AvgMomentofInertia >= movingAssembly.AvgMomentofInertia)
                    return InstallCharacterType.MovingReferenceSimiliar;
                else return InstallCharacterType.ReferenceMovingSimiliarSwitch;
            }
            /* this will not be invoked, but it is left as a final result in case these heuristic cases should change. */
            return InstallCharacterType.Unknown;
        }

        private double findFaceArea(PolygonalFace face)
        {
            var v1 = face.Vertices[1].Position.subtract(face.Vertices[0].Position);
            var v2 = face.Vertices[2].Position.subtract(face.Vertices[1].Position);

            return 0.5 * v1.crossProduct(v2).norm2();
        }

        /// <summary>
        /// if either part is really non-contiguous then return true. We do NOT want to adress
        /// these cases - they should be viewed as two separate install steps.
        /// </summary>
        /// <param name="install">The install.</param>
        /// <param name="A">a.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool EitherRefOrMovHasSeperatedSubassemblies(List<Component>[] install,
            HashSet<Component> A)
        {
            foreach (var subAsm in install)
            {
                var stack = new Stack<Component>();
                var visited = new HashSet<Component>();
                var globalVisited = new HashSet<Component>();
                foreach (var Component in subAsm.Where(n => !globalVisited.Contains(n)))
                {
                    stack.Clear();
                    visited.Clear();
                    stack.Push(Component);
                    while (stack.Count > 0)
                    {
                        var pNode = stack.Pop();
                        visited.Add(pNode);
                        globalVisited.Add(pNode);
                        var a2 = pNode.arcs.Where(a => a is Connection).ToList();
                        foreach (Connection arc in a2)
                        {
                            if (!A.Contains(arc.From) || !A.Contains(arc.To) ||
                                !subAsm.Contains(arc.From) || !subAsm.Contains(arc.To)) continue;
                            var otherNode = (Component)(arc.From == pNode ? arc.To : arc.From);
                            if (visited.Contains(otherNode))
                                continue;
                            stack.Push(otherNode);
                        }
                    }
                    if (visited.Count < subAsm.Count)
                        return true;
                }
            }
            return false;
        }

        private static double DetermineDistanceToSeparateHull(SubAssembly newSubAsm, Double[] insertionDirection)
        {
            var refMaxValue = GeometryFunctions.FindMaxPlaneHeightInDirection(newSubAsm.Install.Reference.CVXHull.Vertices, insertionDirection);
            var refMinValue = GeometryFunctions.FindMinPlaneHeightInDirection(newSubAsm.Install.Reference.CVXHull.Vertices, insertionDirection);

            var movingMaxValue = GeometryFunctions.FindMaxPlaneHeightInDirection(newSubAsm.Install.Moving.CVXHull.Vertices, insertionDirection);
            var movingMinValue = GeometryFunctions.FindMinPlaneHeightInDirection(newSubAsm.Install.Moving.CVXHull.Vertices, insertionDirection);

            var distanceToFree = Math.Abs(refMaxValue - movingMinValue);
            if (distanceToFree < 0) { distanceToFree = 0; throw new Exception("How is distance to free less than zero?"); }
            return distanceToFree + (movingMaxValue - movingMinValue) + (refMaxValue - refMinValue);
        }
    }
}
