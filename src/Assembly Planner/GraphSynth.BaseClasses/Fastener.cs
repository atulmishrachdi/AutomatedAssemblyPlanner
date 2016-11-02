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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Xml.Serialization;
using Assembly_Planner.GraphSynth.BaseClasses;
using GraphSynth.Representation;
using StarMathLib;
using TVGL;

namespace Assembly_Planner
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
        FlatBlade,
        /// <summary>
        /// The phillips blade screw driver, 4 sided
        /// </summary>
        PhillipsBlade,
        /// <summary>
        /// The power screw driver
        /// </summary>
        powerscrewdriver
    }
    /// <summary>
    /// Class Fastener.
    /// </summary>
    public class Fastener : FastenerBase
    {
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
        /// The number of threads if available
        /// </summary>
        public int NumberOfThreads;
        /// <summary>
        /// The nuts if available
        /// </summary>
        public List<Nut> Nuts;
        /// <summary>
        /// The washers if available
        /// </summary>
        public List<Washer> Washer;
        /// <summary>
        /// The Parts Locked By Fastener
        /// </summary>
        public List<int> PartsLockedByFastener;
        /// <summary>
        /// The Secure time input for this fastener
        /// </summary>
        public double[,] SecureModelInputs;
        /// <summary>
        /// for every part that is locked by this fastener, (if they are more than 2), I add one triangle that is touching the
        /// surface of the fastener. The order is the same as the order of the parts added to "PartsLockedByFastener". I can
        /// use this in order to understand which part is on the top, which one is on the bottom. In other words to understand
        /// the order of the PartsLockedByFastener. This will help later to install fastener at the right moment.
        /// </summary>
        private List<PolygonalFace> TrianglesOnTheLockedParts;
        public static List<string> PotentialCollisionOfFastenerAndSolid;
        public static List<string> PotentialCollisionOfFastenerAndSolidStep2;
        public static List<string> PotentialCollisionOfFastenerAndSolidStep3;
        internal static void AddFastenersInformation(designGraph assemblyGraph, Dictionary<string, List<TessellatedSolid>> solidsNoFastener,
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive)
        {
            var counter = 0;
            foreach (var fastener in FastenerDetector.Fasteners)
            {
                counter++;
                var lockedByTheFastener = PartsLockedByTheFastenerFinder(fastener.Solid, solidsNoFastener, solidPrimitive);
                AddFastenerToArc(assemblyGraph, lockedByTheFastener, fastener);
            }
            // So, by this point, if there is a fastener between two or more parts, a new local variable
            // is added to their arc which shows the direction of freedom of the fastener.
            // So if I want to seperate two parts or two subassemblies, now I know that there is a 
            // fastener holding them to each other. And I also know the removal direction of the fastener

            // There is still a possibility here: if any of the potential fasteners are holding 2 or more parts
            // The point is that they can be either a washer, nut or fastener. But if it is a fastener, I need 
            // to find the parts that it's holding and add it to their arc
            counter = 0;
            foreach (var possible in FastenerDetector.PotentialFastener.Keys)
            {
                counter++;
                var locked = PartsLockedByTheFastenerFinder(possible, solidsNoFastener, solidPrimitive);
                if (locked.Count < 2)
                {
                    if (locked.Count == 1)
                    {
                        var comp = (Component)assemblyGraph[locked[0]];
                        var pin = new Fastener()
                        {
                            RemovalDirection =
                                FastenerDetector.RemovalDirectionFinderUsingObb(possible,
                                    BoundingGeometry.OrientedBoundingBoxDic[possible]),
                            Solid = possible,
                            Diameter = BoundingGeometry.BoundingCylinderDic[possible].Radius,
                            OverallLength = BoundingGeometry.BoundingCylinderDic[possible].Length
                        };
                        if (comp.Pins == null) comp.Pins = new List<Fastener>();
                        comp.Pins.Add(pin);
                    }
                }
                PolygonalFace topPlane = null;
                var fastener = new Fastener()
                {
                    RemovalDirection =
                        FastenerDetector.RemovalDirectionFinderUsingObbWithTopPlane(possible,
                            BoundingGeometry.OrientedBoundingBoxDic[possible], out topPlane),
                    Solid = possible,
                    Diameter = BoundingGeometry.BoundingCylinderDic[possible].Radius,
                    OverallLength = BoundingGeometry.BoundingCylinderDic[possible].Length
                };
                AddFastenerToArc(assemblyGraph, locked, fastener);
                /*(if (fastener.PartsLockedByFastener.Count > 2)
                    // if there are more than 2 parts locked by the fastener, sort them based on their distance to the top plane of the fastener
                {

                }*/
            }
        }

        private static void AddFastenerToArc(designGraph assemblyGraph, List<string> lockedByTheFastener, Fastener fastener)
        {
            fastener.PartsLockedByFastener = new List<int>();
            if (lockedByTheFastener.Count == 1)
            {
                var comp = (Component)assemblyGraph[lockedByTheFastener[0]];
                fastener.PartsLockedByFastener.Add(assemblyGraph.nodes.IndexOf(comp));
                if (comp.Pins == null) comp.Pins = new List<Fastener>();
                if (comp.Pins.All(f => f.Solid != fastener.Solid)) comp.Pins.Add(fastener);
            }
            foreach (
                Connection connection in
                    assemblyGraph.arcs.Where(
                        a => lockedByTheFastener.Contains(a.From.name) && lockedByTheFastener.Contains(a.To.name))
                        .ToList())
            {
                if (lockedByTheFastener.Count > 2)
                {
                    foreach (var solid in lockedByTheFastener)
                    {
                        var nodeInd =
                            assemblyGraph.nodes.IndexOf(assemblyGraph.nodes.Where(n => n.name == solid).ToList()[0]);
                        if (fastener.PartsLockedByFastener.Contains(nodeInd)) continue;
                        fastener.PartsLockedByFastener.Add(nodeInd);
                    }
                }
                connection.Fasteners.Add(fastener);
            }
        }

        private static List<string> PartsLockedByTheFastenerFinder(TessellatedSolid fastener, Dictionary<string, List<TessellatedSolid>> solidsNoFastener,
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive)
        {
            PotentialCollisionOfFastenerAndSolid = new List<string>();
            PotentialCollisionOfFastenerAndSolidStep2 = new List<string>();
            PotentialCollisionOfFastenerAndSolidStep3 = new List<string>();
            var lockedByTheFastener = new List<string>();
            foreach (var subAssem in solidsNoFastener)
            {
                foreach (var solid in subAssem.Value)
                {
                    // This has a way simpler blocking determination code. Check it out:
                    if (!BlockingDetermination.BoundingBoxOverlap(fastener, solid)) continue;
                    if (!BlockingDetermination.ConvexHullOverlap(fastener, solid)) continue;
                    if (!BlockingDetermination.ProximityFastener(fastener, solid)) continue;
                    //if (!FastenerPrimitiveOverlap(solidPrimitive[fastener], solidPrimitives)) continue;
                    lockedByTheFastener.Add(subAssem.Key);
                    break;
                }
            }
            if (!lockedByTheFastener.Any() && PotentialCollisionOfFastenerAndSolid.Any())
                lockedByTheFastener.AddRange(PotentialCollisionOfFastenerAndSolid);
            else if (!lockedByTheFastener.Any() && PotentialCollisionOfFastenerAndSolidStep2.Any())
                lockedByTheFastener.AddRange(PotentialCollisionOfFastenerAndSolidStep2);
            else if (!lockedByTheFastener.Any() && PotentialCollisionOfFastenerAndSolidStep3.Any())
                lockedByTheFastener.AddRange(PotentialCollisionOfFastenerAndSolidStep3);
            return lockedByTheFastener;
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

    public class Nut : FastenerBase
    {
        /// <summary>
        /// The overall length
        /// </summary>
        public double OverallLength;
        /// <summary>
        /// The number of threads if available
        /// </summary>
        public int NumberOfThreads;
        /// <summary>
        /// The nut type
        /// </summary>
        public NutType NutType;
        /// <summary>
        /// The washers if available
        /// </summary>
        public List<Washer> Washer;
    }

    public class Washer : FastenerBase
    {

    }

}