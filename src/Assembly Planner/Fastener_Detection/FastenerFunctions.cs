using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseClasses;
using BaseClasses.Representation;
using Geometric_Reasoning;
using StarMathLib;
using TVGL;

namespace Fastener_Detection
{
    public class FastenerFunctions
    {
        private List<PolygonalFace> TrianglesOnTheLockedParts;
        public static List<string> PotentialCollisionOfFastenerAndSolid;
        public static List<string> PotentialCollisionOfFastenerAndSolidStep2;
        public static List<string> PotentialCollisionOfFastenerAndSolidStep3;
        public static void AddFastenersInformation(designGraph assemblyGraph, Dictionary<string, List<TessellatedSolid>> solidsNoFastener,
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
                    if (!ProximityFastener(fastener, solid)) continue;
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

        /*private static bool FastenerPrimitiveOverlap(List<PrimitiveSurface> fastenertPrimitives, List<PrimitiveSurface> solidPrimitives)
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
        }*/

        public static bool ProximityFastener(TessellatedSolid solid1, TessellatedSolid solid2)
        {
            var OverlapAABBPartitions = new List<PartitionAABB[]>();
            BlockingDetermination.PartitionOverlapFinder(PartitioningSolid.PartitionsAABB[solid1], PartitioningSolid.PartitionsAABB[solid2], OverlapAABBPartitions);
            var memoFace = new HashSet<HashSet<PolygonalFace>>(HashSet<PolygonalFace>.CreateSetComparer());
            var counter1 = 0;
            var counter2 = 0;
            var counter3 = 0;
            var counter4 = 0;
            // first one is for solid1, second one is for solid2
            var finalProb = 0.0;

            foreach (var overlapPrtn in OverlapAABBPartitions)
            {
                foreach (var a in overlapPrtn[0].SolidTriangles)
                {
                    if (a.Vertices.Count < 3) continue;
                    foreach (var b in overlapPrtn[1].SolidTriangles)
                    {
                        if (b.Vertices.Count < 3) continue;
                        var newSet = new HashSet<PolygonalFace> { a, b };
                        if (memoFace.Contains(newSet)) continue;
                        memoFace.Add(newSet);
                        counter1++;
                        var localProb = 0.0;
                        var parallel = Math.Abs(a.Normal.dotProduct(b.Normal) + 1);
                        var probPara = OverlappingFuzzification.FuzzyProbabilityCalculator(0.0055, 0.01, parallel);
                        if (probPara == 0) continue; // 0.0055
                        // if they are on the wrong side of each other
                        if (a.Vertices.All(av => (av.Position.subtract(b.Vertices[0].Position)).dotProduct(b.Normal) < 0.0) ||
                            b.Vertices.All(bv => (bv.Position.subtract(a.Vertices[0].Position)).dotProduct(a.Normal) < 0.0)) continue;
                        counter2++;
                        var aAverageEdgeLength = a.Edges.Sum(e => e.Length) / 3.0;
                        var bAverageEdgeLength = b.Edges.Sum(e => e.Length) / 3.0;
                        var q = a.Center;
                        var p = b.Center;
                        var pq = q.subtract(p);
                        var qp = p.subtract(q);
                        var devisionFactor = Math.Min(aAverageEdgeLength, bAverageEdgeLength);
                        var samePlane1 = Math.Abs(pq.dotProduct(a.Normal)) / devisionFactor; // I need to devide it by a better factor
                        var samePlane2 = Math.Abs(qp.dotProduct(b.Normal)) / devisionFactor;
                        var probPlane1 = OverlappingFuzzification.FuzzyProbabilityCalculator(0.1, 0.6, samePlane1); //0.4, 0.5
                        var probPlane2 = OverlappingFuzzification.FuzzyProbabilityCalculator(0.1, 0.6, samePlane2); //0.4, 0.5
                        if (probPlane1 == 0 && probPlane2 == 0) continue; //0.11 //0.005
                        counter3++;
                        if (!BlockingDetermination.TriangleOverlapping(a, b)) continue;
                        return true;
                    }
                }
            }
            if (counter3 > 0)
                PotentialCollisionOfFastenerAndSolid.Add(solid2.Name);
            else if (counter2 > 0)
                PotentialCollisionOfFastenerAndSolidStep2.Add(solid2.Name);
            else if (counter1 > 0)
                PotentialCollisionOfFastenerAndSolidStep3.Add(solid2.Name);
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
}
