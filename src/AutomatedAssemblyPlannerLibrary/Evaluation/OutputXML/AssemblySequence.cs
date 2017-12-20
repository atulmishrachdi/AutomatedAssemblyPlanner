using GraphSynth.Representation;
using MIConvexHull;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
//using System.Runtime.InteropServices.WindowsRuntime;
using StarMathLib;
using Assembly_Planner.GraphSynth.BaseClasses;
using TVGL;

namespace Assembly_Planner
{
    public class AssemblySequence
    {
        public List<SubAssembly> Subassemblies = new List<SubAssembly>();
        private static List<PolygonalFace> movingFacesInCombined;
        [XmlIgnore]
        public static List<PolygonalFace> refFacesInCombined;

        public SubAssembly CreateAssemblyTree(candidate c, int recipeIndex = -1)
        {
            throw new NotImplementedException();
            if (recipeIndex == -1) recipeIndex = c.recipe.Count - 1;
            var ruleAction = c.recipe[recipeIndex];
        }

        public SubAssembly Update(option opt, List<Component> rest, Dictionary<string, TVGLConvexHull> convexHullForParts)
        {
            Part refAssembly, movingAssembly;
            //if (ActionIsAssemblyByAssembly())
            //{
            //    //var node0 = (Component)opt.nodes[0];
            //    //var node1 = (Component)opt.nodes[1];
            //    //var node0name = node0.name;
            //    //var node1name = node1.name;
            //    //refAssembly = Subassemblies.FirstOrDefault(subasm => subasm.PartNames.Contains(node0name));
            //    //if (refAssembly == null)
            //    //    refAssembly = new Part(node0name, GetPartMass(node0), GetPartVolume(node0),
            //    //        convexHullForParts[node0name], GetPartCenterOfMass(node0));
            //    //else Subassemblies.Remove((SubAssembly) refAssembly);
            //    //movingAssembly = Subassemblies.FirstOrDefault(subasm => subasm.PartNames.Contains(node1name));
            //    //if (movingAssembly == null)
            //    //    movingAssembly = new Part(node1name, GetPartMass(node1), GetPartVolume(node1),
            //    //        convexHullForParts[node1name], GetPartCenterOfMass(node0));
            //    //else Subassemblies.Remove((SubAssembly) movingAssembly);
            //}
            //else if (ActionIsRemoveSCC())
            //{
                var movingNodes = opt.Nodes.Cast<Component>().ToList();
                var newSubAsmNodes = rest;
                if (movingNodes.Count == 1)
                {
                    var nodeName = movingNodes[0].name;
                    movingAssembly = new Part(nodeName,
                        GetPartMass(movingNodes[0]), GetPartVolume(movingNodes[0]),
                        convexHullForParts[nodeName], GetPartCenterOfMass(movingNodes[0]));
                }
                else
                {
                    var combinedCVXHullM = CreateCombinedConvexHull2(movingNodes, convexHullForParts);
                    var VolumeM = GetSubassemblyVolume(movingNodes);
                    var MassM = GetSubassemblyMass(movingNodes);
                    var centerOfMass = GetSubassemblyCenterOfMass(movingNodes);
                    movingAssembly = new SubAssembly(new HashSet<Component>(movingNodes), combinedCVXHullM, MassM, VolumeM, centerOfMass);
                }

                var referenceHyperArcnodes = new List<Component>();
                referenceHyperArcnodes = newSubAsmNodes.Where(a => !movingNodes.Contains(a)).ToList();
                if (referenceHyperArcnodes.Count == 1)
                {
                    var nodeName = referenceHyperArcnodes[0].name;
                    refAssembly = new Part(nodeName,
                        GetPartMass(referenceHyperArcnodes[0]), GetPartVolume(referenceHyperArcnodes[0]),
                        convexHullForParts[nodeName], GetPartCenterOfMass(referenceHyperArcnodes[0]));
                }
                else
                {
                    var combinedCVXHullR = CreateCombinedConvexHull2(referenceHyperArcnodes, convexHullForParts);
                    var VolumeR = GetSubassemblyVolume(referenceHyperArcnodes);
                    var MassR = GetSubassemblyMass(referenceHyperArcnodes);
                    var centerOfMass = GetSubassemblyCenterOfMass(referenceHyperArcnodes);
                    refAssembly = new SubAssembly(new HashSet<Component>(referenceHyperArcnodes), combinedCVXHullR, MassR, VolumeR, centerOfMass);
                }
            //}
            //else throw new Exception("Only install rules in assembly at this point.");
            TVGLConvexHull combinedCVXHull = CreateCombinedConvexHull(
                refAssembly.CVXHull, movingAssembly.CVXHull);
            //List<PolygonalFace> refFacesInCombined, movingFacesInCombined;
            var InstallCharacter = shouldReferenceAndMovingBeSwitched(refAssembly, movingAssembly, combinedCVXHull,
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
            string refName = NameMaker(refAssembly);
            string movName = NameMaker(movingAssembly);
            var newSubassembly = new SubAssembly(refAssembly, movingAssembly, combinedCVXHull, InstallCharacter,
                refFacesInCombined);
            //newSubassembly.Name = refName +"   on   "+movName;
            newSubassembly.CenterOfMass = CombinedCenterOfMass(newSubassembly);
            // instead of adding to Subassemblies, newSubassembly must be added to its preceeding subassembly (to its parent)
            Subassemblies.Add(newSubassembly);
            return newSubassembly;
        }

        private string NameMaker(Part refAssembly)
        {
            var name = refAssembly.PartNames[0];
            for (var i = 1; i < refAssembly.PartNames.Count; i++)
            {
                name = name + "," + refAssembly.PartNames[i];
            }
            return name;
        }

        private Vertex CombinedCenterOfMass(SubAssembly newSubassembly)
        {
            return
                new Vertex(new[]{(newSubassembly.Install.Moving.CenterOfMass.Position[0] +
                     newSubassembly.Install.Reference.CenterOfMass.Position[0])/2,
                    (newSubassembly.Install.Moving.CenterOfMass.Position[1] +
                     newSubassembly.Install.Reference.CenterOfMass.Position[1])/2,
                    (newSubassembly.Install.Moving.CenterOfMass.Position[2] +
                     newSubassembly.Install.Reference.CenterOfMass.Position[2])/2});
        }


        private bool ActionIsRemoveSCC()
        {
            return true;
            //return rule.name.Equals("choose_SCC");
        }
        /// <summary>
        /// Should the reference and moving be switched? Essentially the initial choice about what is the reference
        /// and what is the moving sub-assemblies is arbitrary. With this function, we make a detailed check to determine
        /// which should truly be the reference.
        /// </summary>
        /// <param name="refAssembly">The reference assembly.</param>
        /// <param name="movingAssembly">The moving assembly.</param>
        /// <param name="combinedCVXHull">The combined convex hull.</param>
        /// <param name="refFacesInCombined">The reference faces in combined.</param>
        /// <param name="movingFacesInCombined">The moving faces in combined.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">The point is in neither original part!</exception>
        private InstallCharacterType shouldReferenceAndMovingBeSwitched(Part refAssembly, Part movingAssembly, TVGLConvexHull combinedCVXHull,
            out List<PolygonalFace> refFacesInCombined, out List<PolygonalFace> movingFacesInCombined)
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
            if (formerFacesArea / totalFaceArea > Constants.CVXFormerFaceConfidence)
            {
                /* there are two check here: if the common area is very small, we assume the 
                 * subassembly is inside the other. If not, maybe it is more on the outside
                 * but a smaller effect on resulting convex hull. */
                if (refFaceArea / formerFacesArea < Constants.CVXOnInsideThreshold)
                    return InstallCharacterType.ReferenceIsInsideMoving;
                if (movingFaceArea / formerFacesArea < Constants.CVXOnInsideThreshold)
                    return InstallCharacterType.MovingIsInsideReference;
                if (refFaceArea / formerFacesArea < Constants.CVXOnOutsideThreshold)
                    return InstallCharacterType.ReferenceIsOnOutsideOfMoving;
                if (movingFaceArea / formerFacesArea < Constants.CVXOnOutsideThreshold)
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

        private double GetSubassemblyMass(List<Component> Nodes)
        { return Nodes.Sum(n => GetPartMass(n)); }

        private Vertex GetSubassemblyCenterOfMass(List<Component> Nodes)
        {
            var sumMx = 0.0;
            var sumMy = 0.0;
            var sumMz = 0.0;
            var M = 0.0;
            foreach (var n in Nodes)
            {
                var m = GetPartMass(n);
                var nCOM = GetPartCenterOfMass(n);
                sumMx += nCOM.Position[0] * m;
                sumMy += nCOM.Position[1] * m;
                sumMz += nCOM.Position[2] * m;
                M += m;
            }

            return new Vertex(new[] { sumMx / M, sumMy / M, sumMz / M });
        }

        private double GetPartMass(Component n)
        {
            //var j = n.localVariables.IndexOf(-6000);
            //if (j == -1) return 0.0; //in case we don't have a weight tag, treat the part as zero instead
            //else return n.localVariables[j + 1];
            return n.Mass;
        }

        private Vertex GetPartCenterOfMass(Component n)
        {
            //var j = n.localVariables.IndexOf(-6005);
            //if (j == -1) return null; //in case we don't have a center of mass tag, treat the part as zero instead
            //else return new Vertex(n.localVariables[j + 1], n.localVariables[j + 2], n.localVariables[j + 3]);
            return new Vertex(new[] { n.CenterOfMass[0], n.CenterOfMass[1], n.CenterOfMass[2] });
        }

        private double GetSubassemblyVolume(List<Component> Nodes)
        { return Nodes.Sum(n => GetPartVolume(n)); }
        
        private double GetPartVolume(Component n)
        {
            //var j = n.localVariables.IndexOf(-6001);
            //if (j == -1) return 0.0; //in case we don't have a volume tag, treat the part as zero instead
            //else return n.localVariables[j + 1];
            return n.Volume;
        }


        private TVGLConvexHull CreateCombinedConvexHull(TVGLConvexHull refCVXHull, TVGLConvexHull movingCVXHull)
        {
            var pointCloud = new List<Vertex>(refCVXHull.Vertices);
            pointCloud.AddRange(movingCVXHull.Vertices);
            return new TVGLConvexHull(pointCloud, 1e-8);
        }

        private TVGLConvexHull CreateCombinedConvexHull2(List<Component> nodes, Dictionary<string, TVGLConvexHull> convexHullForParts)
        {
            var pointCloud = new List<Vertex>();
            foreach (var n in nodes)
            {
                var nodeName = n.name;
                pointCloud.AddRange(convexHullForParts[nodeName].Vertices);
            }
            return new TVGLConvexHull(pointCloud, 1e-8);
        }


        private static bool ActionIsAssemblyByAssembly()
        {
            return false;
            //return rule.name.Equals("merger-additional");
        }



        internal AssemblySequence copy()
        {
            var copySequence = new AssemblySequence();
            copySequence.Subassemblies = new List<SubAssembly>(Subassemblies);
            return copySequence;
        }


    }
}