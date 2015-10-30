// ***********************************************************************
// Assembly         : Assembly Planner
// Author           : 
// Created          : 08-27-2015
//
// Last Modified By : Matt
// Last Modified On : 08-27-2015
// ***********************************************************************
// <copyright file="Fastener.cs" company="">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************
/// <summary>
/// The BaseClasses namespace.
/// </summary>

using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Xml.Serialization;
using GraphSynth.Representation;
using TVGL;

namespace Assembly_Planner.GraphSynth.BaseClasses
{
    /// <summary>
    /// Enum FastenerTypeEnum
    /// </summary>
    public enum FastenerTypeEnum
    {
        /// <summary>
        /// The bolt
        /// </summary>
        Bolt,
        /// <summary>
        /// The screw
        /// </summary>
        Screw,
        /// <summary>
        /// The other threaded post
        /// </summary>
        OtherThreadedPost,
        /// <summary>
        /// The pin
        /// </summary>
        Pin,
        /// <summary>
        /// The rivet
        /// </summary>
        Rivet,
        /// <summary>
        /// The clip
        /// </summary>
        Clip,
        /// <summary>
        /// The adhesive
        /// </summary>
        Adhesive,
        /// <summary>
        /// The weld
        /// </summary>
        Weld
    }
    public enum NutType
    {
        /// <summary>
        /// The hex
        /// </summary>
        Hex,
        /// <summary>
        /// The wing
        /// </summary>
        Wing
    }
    public enum Tool
    {
        /// <summary>
        /// The Hex Wrench
        /// </summary>
        HexWrench,
        /// <summary>
        /// The Allen or Hex socket
        /// </summary>
        Allen,
        /// <summary>
        /// The simple flat screw driver
        /// </summary>
        FlatBlade ,
        /// <summary>
        /// The phillips blade screw driver, 4 sided
        /// </summary>
        PhillipsBlade
    }
    /// <summary>
    /// Class Fastener.
    /// </summary>
    public class Fastener
    {

        /// <summary>
        /// The tesselated solid
        /// </summary>
        [XmlIgnore]
        public TessellatedSolid Solid;
        /// <summary>
        /// The removal direction
        /// </summary>
        public int RemovalDirection;
        /// <summary>
        /// The overall length
        /// </summary>
        public double OverallLength;
        /// <summary>
        /// The engaged length
        /// </summary>
        public double EngagedLength;
        /// <summary>
        /// The diameter
        /// </summary>
        public double Diameter;
        /// <summary>
        /// The access position
        /// </summary>
        public double[] AccessPosition;
        /// <summary>
        /// The access direction
        /// </summary>
        public double[] AccessDirection;
        /// <summary>
        /// The access cylinder radius
        /// </summary>
        public double AccessCylinderRadius;
        /// <summary>
        /// The fastener type
        /// </summary>
        public FastenerTypeEnum FastenerType;
        /// <summary>
        /// The certainity of this classification
        /// </summary>
        public double Certainty;
        /// <summary>
        /// The tool if identified
        /// </summary>
        public Tool Tool;
        /// <summary>
        /// The size of the tool if identified
        /// </summary>
        public double ToolSize;
        /// <summary>
        /// The nut if available
        /// </summary>
        public Nut Nut;
        /// <summary>
        /// The fastener type
        /// </summary>
        public List<int> PartsLockedByFastener = new List<int>();

        internal static void AddFastenersInformation(designGraph assemblyGraph, HashSet<TessellatedSolid> screwsAndBolts, List<TessellatedSolid> solidsNoFastener,
    Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive)
        {
            foreach (var fastener in screwsAndBolts)
            {
                var fastenertPrimitives = solidPrimitive[fastener];
                var lockedByTheFastener = new List<TessellatedSolid>();
                foreach (var solid in solidsNoFastener)
                {
                    var solidPrimitives = solidPrimitive[solid];
                    // This has a way simpler blocking determination code. Check it out:
                    if (BlockingDetermination.BoundingBoxOverlap(fastener, solid))
                        if (BlockingDetermination.ConvexHullOverlap(fastener, solid))
                            if (FastenerPrimitiveOverlap(fastenertPrimitives, solidPrimitives))
                                lockedByTheFastener.Add(solid);
                }
                // now find the removal direction of the fastener.
                //var RD = BoltRemovalDirection(fastener);
                double depth, radius;
                int RD;
                if (fastener.Name.Contains("LargeScrew"))
                {
                    RD = 1;
                    depth = 25;
                    radius = 8;
                }
                else if (fastener.Name.Contains("ScrewLong"))
                {
                    RD = 316;
                    depth = 27;
                    radius = 6;
                }
                else if (fastener.Name.Contains("S1") || fastener.Name.Contains("S2"))
                {
                    RD = 38;
                    depth = 47;
                    radius = 4;
                }
                else
                {
                    RD = 38;
                    depth = 17;
                    radius = 2.5;
                }
                AddRemovalInformationToArcs(assemblyGraph, lockedByTheFastener, RD, depth, radius);
            }
            // So, by this point, if there is a fastener between two or more parts, a new local variable
            // is added to their arc which shows the direction of freedom of the fastener.
            // So if I want to seperate two parts or two subassemblies, now I know that there is a 
            // fastener holding them to each other. And I also know the removal direction of the fastener
        }

        private static void AddRemovalInformationToArcs(designGraph graph,
            List<TessellatedSolid> lockedByTheFastener, int RD, double depth, double radius)
        {
            var partsName = lockedByTheFastener.Select(part => part.Name).ToList();
            foreach (
                Connection connection in
                    graph.arcs.Where(a => partsName.Contains(a.From.name) && partsName.Contains(a.To.name))
                        .ToList())
            {
                var fasten = new GraphSynth.BaseClasses.Fastener();
                fasten.RemovalDirection = RD;
                fasten.Diameter = radius * 2;
                fasten.EngagedLength = depth;
                if (lockedByTheFastener.Count > 2)
                    foreach (var solid in lockedByTheFastener)
                        fasten.PartsLockedByFastener.Add(graph.nodes.IndexOf(graph.nodes.Where(n => n.name == solid.Name).ToList()[0]));
                connection.Fasteners.Add(fasten);
            }
        }

        private static bool FastenerPrimitiveOverlap(List<PrimitiveSurface> fastenertPrimitives, List<PrimitiveSurface> solidPrimitives)
        {
            foreach (var primitiveA in fastenertPrimitives)
            {
                foreach (var primitiveB in solidPrimitives)
                {
                    if (primitiveA is Cylinder && primitiveB is Cylinder)
                        if (PrimitivePrimitiveInteractions.CylinderCylinderOverlappingCheck((Cylinder)primitiveA, (Cylinder)primitiveB))
                            return true;
                    if (primitiveA is Cone && primitiveB is Cone)
                        if (PrimitivePrimitiveInteractions.ConeConeOverlappingCheck((Cone)primitiveA, (Cone)primitiveB))
                            return true;
                }
            }
            return false;
        }

        /*private static List<int> BoltRemovalDirection(TessellatedSolid fastener)
        {
            var dir = new double[3];
            var CvhSolid = new TessellatedSolid
            {
                Faces = fastener.ConvexHullFaces,
                Edges = fastener.ConvexHullEdges
            };

            var solidPrim = TesselationToPrimitives.Run(CvhSolid);
            var cones = solidPrim.Where(p => p is Cone).ToList();
            if (cones.Count == 0)
                throw Exception("If the part is Bolt or Screw, its CVH must contain Cone primitive");
            var largestCone = new PrimitiveSurface();
            var maxArea = 0.0;
            foreach (var cone in cones)
            {
                if (cone.Area < maxArea) continue;
                maxArea = cone.Area;
                largestCone = cone;
            }
            var selectedCone = (Cone)largestCone;
            dir = selectedCone.Axis.multiply(-1);
            return NormalIndexInGlobalDirns(dir);
        }*/
    }

    public class Nut
    {
        /// <summary>
        [XmlIgnore]
        /// The tesselated solid
        /// </summary>
        public TessellatedSolid Solid;
        /// <summary>
        /// The removal direction
        /// </summary>
        public int RemovalDirection;
        /// <summary>
        /// The overall length
        /// </summary>
        public double OverallLength;
        /// <summary>
        /// The diameter
        /// </summary>
        public double Diameter;
        /// <summary>
        /// The nut type
        /// </summary>
        public NutType NutType;
        /// <summary>
        /// The tool if identified
        /// </summary>
        public Tool Tool;
        /// <summary>
        /// The size of the tool if identified
        /// </summary>
        public double ToolSize;

    }

}