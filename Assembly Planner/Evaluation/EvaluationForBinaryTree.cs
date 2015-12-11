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

namespace Assembly_Planner
{
    /// <summary>
    /// Class EvaluationForBinaryTree - this is a stub for evaluating a particular install step
    /// </summary>
    class EvaluationForBinaryTree
    {
        private static List<DefaultConvexFace<Vertex>> movingFacesInCombined;
        public static List<DefaultConvexFace<Vertex>> refFacesInCombined;
        public Dictionary<string, ConvexHull<Vertex, DefaultConvexFace<Vertex>>> convexHullForParts;

        public EvaluationForBinaryTree(Dictionary<string, ConvexHull<Vertex, DefaultConvexFace<Vertex>>> convexHullForParts)
        {
            //feasibility = new FeasibilityEvaluator();
            this.convexHullForParts = convexHullForParts;
            //    reOrientations = new ReOrientations();
        }
        /// <summary>
        /// Evaluates the subassemly.
        /// </summary>
        /// <param name="subassemblyNodes">The subassembly nodes - all the nodes in the combined install action.</param>
        /// <param name="optNodes">The subset of nodes that represent one of the two parts in the install step.</param>
        /// <param name="sub">The sub is the class that is then tied into the treequence.</param>
        /// <returns>System.Double.</returns>
        public double EvaluateSub(designGraph graph, List<Component> subassemblyNodes, List<Component> optNodes, out SubAssembly sub)
        {

            var rest = subassemblyNodes.Where(n => !optNodes.Contains(n)).ToList();
            sub = Update(optNodes, rest);
            var install = new[] { rest, optNodes };
            if (EitherRefOrMovHasSeperatedSubassemblies(install, subassemblyNodes))
                return -1;
            sub.Install.Time = 10;
            return 1;
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
                movingAssembly = new Part(nodeName, movingNodes[0].Volume, movingNodes[0].Volume,
                    convexHullForParts[nodeName], new Vertex(movingNodes[0].CenterOfMass));
            }
            else
            {
                var combinedCVXHullM = CreateCombinedConvexHull2(movingNodes);
                var VolumeM = GetSubassemblyVolume(movingNodes);
                var MassM = GetSubassemblyMass(movingNodes);
                var centerOfMass = GetSubassemblyCenterOfMass(movingNodes);
                movingAssembly = new SubAssembly(movingNodes, combinedCVXHullM, MassM, VolumeM, centerOfMass);
            }

            var referenceHyperArcnodes = new List<Component>();
                referenceHyperArcnodes = (List<Component>) newSubAsmNodes.Where(a => !movingNodes.Contains(a)).ToList();
            if (referenceHyperArcnodes.Count == 1)
            {
                var nodeName = referenceHyperArcnodes[0].name;
                refAssembly = new Part(nodeName, referenceHyperArcnodes[0].Mass, referenceHyperArcnodes[0].Volume,
                    convexHullForParts[nodeName],
                    new Vertex(referenceHyperArcnodes[0].CenterOfMass));
            }
            else
            {
                var combinedCVXHullR = CreateCombinedConvexHull2(referenceHyperArcnodes);
                var VolumeR = GetSubassemblyVolume(referenceHyperArcnodes);
                var MassR = GetSubassemblyMass(referenceHyperArcnodes);
                var centerOfMass = GetSubassemblyCenterOfMass(referenceHyperArcnodes);
                refAssembly = new SubAssembly(referenceHyperArcnodes, combinedCVXHullR, MassR, VolumeR, centerOfMass);
            }
            var combinedCvxHull = CreateCombinedConvexHull(refAssembly.CVXHull, movingAssembly.CVXHull);
            var InstallCharacter = shouldReferenceAndMovingBeSwitched(refAssembly, movingAssembly, combinedCvxHull,
                out refFacesInCombined, out movingFacesInCombined);
            if ((int) InstallCharacter < 0)
            {
                var tempASM = refAssembly;
                refAssembly = movingAssembly;
                movingAssembly = tempASM;
                refFacesInCombined = movingFacesInCombined; // no need to use temp here, as the movingFaces in the 
                // combined convex hull are not needed.
                InstallCharacter = (InstallCharacterType) (-((int) InstallCharacter));
            }
            var newSubassembly = new SubAssembly(refAssembly, movingAssembly, combinedCvxHull, InstallCharacter,
                refFacesInCombined);
            newSubassembly.CenterOfMass = CombinedCenterOfMass(newSubassembly);
            return newSubassembly;
        }

        private Vertex CombinedCenterOfMass(SubAssembly newSubassembly)
        {
            return
                new Vertex(
                    (newSubassembly.Install.Moving.CenterOfMass.Position[0] +
                     newSubassembly.Install.Reference.CenterOfMass.Position[0]) / 2,
                    (newSubassembly.Install.Moving.CenterOfMass.Position[1] +
                     newSubassembly.Install.Reference.CenterOfMass.Position[1]) / 2,
                    (newSubassembly.Install.Moving.CenterOfMass.Position[2] +
                     newSubassembly.Install.Reference.CenterOfMass.Position[2]) / 2);
        }
        private ConvexHull<Vertex, DefaultConvexFace<Vertex>> CreateCombinedConvexHull(ConvexHull<Vertex, DefaultConvexFace<Vertex>> refCVXHull, ConvexHull<Vertex, DefaultConvexFace<Vertex>> movingCVXHull)
        {
            var pointCloud = new List<Vertex>(refCVXHull.Points);
            pointCloud.AddRange(movingCVXHull.Points);
            return ConvexHull.Create(pointCloud);
        }

        private ConvexHull<Vertex, DefaultConvexFace<Vertex>> CreateCombinedConvexHull2(List<Component> nodes)
        {
            var pointCloud = new List<Vertex>();
            foreach (var n in nodes)
            {
                var nodeName = n.name;
                pointCloud.AddRange(convexHullForParts[nodeName].Points);
            }
            return ConvexHull.Create(pointCloud);
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

            return new Vertex(sumMx / M, sumMy / M, sumMz / M);
        }

        private InstallCharacterType shouldReferenceAndMovingBeSwitched(Part refAssembly, Part movingAssembly,
            ConvexHull<Vertex, DefaultConvexFace<Vertex>> combinedCVXHull,
            out List<DefaultConvexFace<Vertex>> refFacesInCombined,
            out List<DefaultConvexFace<Vertex>> movingFacesInCombined)
        {
            /* first, create a list of vertices from the reference hull that are present in the combined hull.
             * likewise, with the moving. */
            var refVertsInCombined = new List<Vertex>();
            var movingVertsInCombined = new List<Vertex>();
            foreach (var pt in combinedCVXHull.Points)
            {
                if (refAssembly.CVXHull.Points.Contains(pt)) refVertsInCombined.Add(pt);
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
                refFacesInCombined = new List<DefaultConvexFace<Vertex>>(combinedCVXHull.Faces);
                return InstallCharacterType.MovingIsInsideReference;
            }
            /* ...likewise for the original reference */
            if (refVertsInCombined.Count == 0)
            {
                refFacesInCombined = null;
                movingFacesInCombined = new List<DefaultConvexFace<Vertex>>(combinedCVXHull.Faces);
                return InstallCharacterType.ReferenceIsInsideMoving;
            }
            /* we could just count the number of vertices, but that would not be as accurate a prediction
             * as the area of the faces */
            refFacesInCombined = new List<DefaultConvexFace<Vertex>>();
            movingFacesInCombined = new List<DefaultConvexFace<Vertex>>();
            double refFaceArea = 0.0;
            var movingFaceArea = 0.0;
            var totalFaceArea = 0.0;
            foreach (var face in combinedCVXHull.Faces)
            {
                var faceArea = findFaceArea(face);
                totalFaceArea += faceArea;
                if (face.Vertices.All(v => refAssembly.CVXHull.Points.Contains(v)))
                {
                    refFacesInCombined.Add(face);
                    refFaceArea += faceArea;
                }
                else if (face.Vertices.All(v => movingAssembly.CVXHull.Points.Contains(v)))
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

        private double findFaceArea(DefaultConvexFace<Vertex> face)
        {
            var v1 = face.Vertices[0].MakeVectorTo(face.Vertices[1]);
            var v2 = face.Vertices[0].MakeVectorTo(face.Vertices[2]);

            return 0.5 * v1.Position.crossProduct(v2.Position).norm2();
        }

        /// <summary>
        /// if either part is really non-contiguous then return true. We do NOT want to adress
        /// these cases - they should be viewed as two separate install steps.
        /// </summary>
        /// <param name="install">The install.</param>
        /// <param name="A">a.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool EitherRefOrMovHasSeperatedSubassemblies(List<Component>[] install, 
            List<Component> A)
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
                        var a2 = pNode.arcs.Where(a => a.GetType() == typeof (Connection)).ToList();
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
    }
}
