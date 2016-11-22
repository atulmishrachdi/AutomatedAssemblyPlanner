using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Assembly_Planner;
using GraphSynth.Representation;
using StarMathLib;
using TVGL;
using TVGL;
using PrimitiveClassificationOfTessellatedSolids;

namespace Assembly_Planner
{
    internal class BlockingDetermination
    {
        public static List<OverlappedSurfaces> OverlappingSurfaces = new List<OverlappedSurfaces>();

        internal static Dictionary<TessellatedSolid, List<PrimitiveSurface>> PrimitiveMaker(List<TessellatedSolid> parts)
        {
            Console.WriteLine();
            Console.WriteLine("Classifying Primitives for " + parts.Count + " unique parts ....");
            var partPrimitive = new Dictionary<TessellatedSolid, List<PrimitiveSurface>>();


            int width = 55;
            int refresh = (int) Math.Ceiling(((float) parts.Count) / ((float)(width)) );
            int check = 0;
            LoadingBar.start(width, 0);

            Parallel.ForEach(parts, solid =>
            //foreach (var solid in parts)
            {
                if (check % refresh == 0)
                {
                    LoadingBar.refresh(width, ((float)check) / ((float)parts.Count));
                }
                check++;

                var solidPrim = TesselationToPrimitives.Run(solid);
                lock (partPrimitive)
                    partPrimitive.Add(solid, solidPrim);
            }
             );//
            LoadingBar.refresh(width, 1);
            return partPrimitive;
        }

        internal static List<TessellatedSolid> PartsTobeClassifiedIntoPrimitives(Dictionary<string, List<TessellatedSolid>> solids)
        {
            // from the parts with multiple geometries, not all of them are necessary to be classified into their containing primitives
            var geometriesToBeClassified = new List<TessellatedSolid>();
            foreach (var solid in solids.Values)
            {
                if (solid.Count == 1)
                {
                    geometriesToBeClassified.Add(solid[0]);
                    continue;
                }
                foreach (var geometry1 in solid)
                {
                    foreach (var key in solids.Keys.Where(k => k != geometry1.Name))
                    {
                        foreach (var geometry2 in solids[key])
                        {
                            if (!BoundingBoxOverlap(geometry1, geometry2)) continue;
                            if (!ConvexHullOverlap(geometry1, geometry2)) continue;
                            if (!geometriesToBeClassified.Contains(geometry1))
                                geometriesToBeClassified.Add(geometry1);
                        }
                    }
                }
            }
            return geometriesToBeClassified;
        }
        internal static bool DefineBlocking(TessellatedSolid solid1, TessellatedSolid solid2,
            List<int> globalDirPool, List<int> localDirInd, out double certainty)
        {

            if (BoundingBoxOverlap(solid1, solid2))
            {
                if (ConvexHullOverlap(solid1, solid2))
                {
                    if (ProximityBoosted(solid1, solid2, localDirInd, out certainty))
                    {
                        var final = new HashSet<int>();
                        foreach (var i in localDirInd)
                        {
                            if (
                                final.Any(
                                    d =>
                                        1 -
                                        DisassemblyDirections.Directions[d].dotProduct(
                                            DisassemblyDirections.Directions[i]) < 0.07)) continue;
                            final.Add(i);
                        }
                        lock (globalDirPool)
                        {
                            if (final.Count < 3)
                            {
                                localDirInd.Clear();
                                foreach (var i in final)
                                {
                                    if (!globalDirPool.Contains(i))
                                        globalDirPool.Add(i);
                                    if (!localDirInd.Contains(i))
                                        localDirInd.Add(i);
                                }
                                return true;
                            }
                            var finalLocalDirInd = new List<int>();
                            foreach (var i in final)
                            {
                                var temp =
                                    globalDirPool.Where(
                                        d =>
                                            1 -
                                            DisassemblyDirections.Directions[d].dotProduct(
                                                DisassemblyDirections.Directions[i]) < 0.01).ToList();
                                if (!temp.Any())
                                {
                                    if (!finalLocalDirInd.Contains(i))
                                        finalLocalDirInd.Add(i);
                                    if (!globalDirPool.Contains(i))
                                        globalDirPool.Add(i);
                                }
                                else
                                {
                                    if (!finalLocalDirInd.Contains(temp[0]))
                                        finalLocalDirInd.Add(temp[0]);
                                }
                            }
                            localDirInd.Clear();
                            AddSixMainAxes(finalLocalDirInd, globalDirPool);
                            foreach (var i in finalLocalDirInd)
                                localDirInd.Add(i);
                        }
                        return true;
                    }
                    //if (GearGear(assemblyGraph, solid1, solid2))
                    //{
                    // one more condition to check. If Gear-Gear, check and see if the normals of the solids are parallel
                    //    if (!ParallelNormals(assemblyGraph, solid1, solid2))
                    //    {
                    //        dirInd = null;
                    //        return false;
                    //    }
                    //    dirInd = GearNormal(assemblyGraph, solid1);
                    //    return true;
                    //}
                    /*var localDirInd = new HashSet<int>();
                    for (var i = 0; i < DisassemblyDirections.Directions.Count; i++)
                        localDirInd.Add(i);
                    ProximityBoosted(solid1, solid2, localDirInd, out certainty);
                    var overlappedPrimitives = new List<PrimitiveSurface[]>();
                    if (ProximityBeta(solid1, solid2, localDirInd.ToList()))
                    {
                        var a = 3;
                    }
                    if (PrimitivePrimitiveInteractions.PrimitiveOverlap(solid1P, solid2P, localDirInd.ToList(),
                        out overlappedPrimitives, out certainty))
                    {
                        var overlappingSurface = new OverlappedSurfaces
                        {
                            Solid1 = solid1,
                            Solid2 = solid2,
                            Overlappings = overlappedPrimitives
                        };
                        OverlappingSurfaces.Add(overlappingSurface);
                        // dirInd is the list of directions that must be added to the arc between part1 and part2
                        globalDirPool.AddRange(localDirInd.Where(d => !globalDirPool.Contains(d)));
                        if (printResults > 0)
                        {
                            if (printResults == 1)
                                Console.WriteLine(@"An overlap is detected between   " + solid1.Name + "   and   " +
                                                  solid2.Name);
                            else
                            {
                                Console.WriteLine(@"An overlap is detected between   " + solid1.Name + "   and   " +
                                                  solid2.Name);
                                foreach (var i in localDirInd)
                                {
                                    Console.WriteLine(DisassemblyDirections.Directions[i][0] + " " +
                                                      DisassemblyDirections.Directions[i][1] + " " +
                                                      DisassemblyDirections.Directions[i][2]);
                                }
                            }

                        }
                        dirInd = localDirInd.ToList();
                        return true;
                    }*/
                }
            }
            certainty = 1.0;
            return false;
        }
        private static void AddSixMainAxes(List<int> finalLocalDirInd, List<int> globalDirPool)
        {
            var temp = new List<int>(finalLocalDirInd);
            var mainDris = new List<double[]>
            {
                new[] {0, 0, 1.0},
                new[] {0, 1.0, 0},
                new[] {1.0, 0, 0},
                new[] {0, 0, -1.0},
                new[] {0, -1.0, 0},
                new[] {-1.0, 0, 0}
            };
            foreach (var dir in mainDris)
            {
                var index = -1;
                for (var i = 0; i < DisassemblyDirections.Directions.Count; i++)
                {
                    var d = DisassemblyDirections.Directions[i];
                    if (d[0] == dir[0] && d[1] == dir[1] && d[2] == dir[2]) index = i;
                }
                if (!finalLocalDirInd.Contains(index))
                {
                    var firstFilter = temp.Where(d => 1 - dir.dotProduct(DisassemblyDirections.Directions[d]) <= 0.07).ToList();
                    if (firstFilter.Any())
                    {
                        var secondFilter =
                            firstFilter.Where(d => 1 - dir.dotProduct(DisassemblyDirections.Directions[d]) <= 0.007)
                                .ToList();
                        finalLocalDirInd.Add(index);
                        if (!globalDirPool.Contains(index)) globalDirPool.Add(index);
                        if (secondFilter.Any())
                            foreach (var i in secondFilter)
                                finalLocalDirInd.Remove(i);
                        continue;
                    }
                }
            }
        }
        internal static bool ProximityBoosted(TessellatedSolid solid1, TessellatedSolid solid2, List<int> localDirInd, out double certainty)
        {
            var OverlapAABBPartitions = new List<PartitionAABB[]>();
            PartitionOverlapFinder(PartitioningSolid.PartitionsAABB[solid1], PartitioningSolid.PartitionsAABB[solid2], OverlapAABBPartitions);
            var memoFace = new HashSet<HashSet<PolygonalFace>>(HashSet<PolygonalFace>.CreateSetComparer());
            var memoNormal = new HashSet<double[]>();
            var counter1 = 0;
            var counter2 = 0;
            var counter3 = 0;
            var counter4 = 0;
            // first one is for solid1, second one is for solid2
            var triTriOver = new HashSet<PolygonalFace[]>();
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
                        counter4++;
                        var localProb = 0.0;
                        var parallel = Math.Abs(a.Normal.dotProduct(b.Normal) + 1);
                        var probPara = OverlappingFuzzification.FuzzyProbabilityCalculator(0.0055, 0.006, parallel);
                        if (probPara == 0) continue; // 0.0055
                        var aAverageEdgeLength = a.Edges.Sum(e => e.Length) / 3.0;
                        var bAverageEdgeLength = b.Edges.Sum(e => e.Length) / 3.0;
                        // if they are on the wrong side of each other
                        if (a.Vertices.All(av => (av.Position.subtract(b.Vertices[0].Position)).dotProduct(b.Normal) / aAverageEdgeLength < -1e-5) ||
                            b.Vertices.All(bv => (bv.Position.subtract(a.Vertices[0].Position)).dotProduct(a.Normal) / bAverageEdgeLength < -1e-5)) continue;
                        counter2++;
                        var q = a.Center;
                        var p = b.Center;
                        var pq = q.subtract(p);
                        var qp = p.subtract(q);
                        var devisionFactor = Math.Min(aAverageEdgeLength, bAverageEdgeLength);
                        var samePlane1 = Math.Abs(pq.dotProduct(a.Normal)) / devisionFactor; // I need to devide it by a better factor
                        var samePlane2 = Math.Abs(qp.dotProduct(b.Normal)) / devisionFactor;
                        var probPlane1 = OverlappingFuzzification.FuzzyProbabilityCalculator(0.1, 0.4, samePlane1); //0.4, 0.5
                        var probPlane2 = OverlappingFuzzification.FuzzyProbabilityCalculator(0.1, 0.4, samePlane2); //0.4, 0.5
                        if (probPlane1 == 0 || probPlane2 == 0) continue; //0.11 //0.005
                        counter3++;
                        if (!TriangleOverlapping(a, b)) continue;
                        triTriOver.Add(new[] { a, b });
                        counter1++;
                        localProb = Math.Min(probPara, Math.Max(probPlane1, probPlane2));
                        if (localProb > finalProb) finalProb = localProb;
                        var newNorm = b.Normal.multiply(-1);
                        if (memoNormal.Count < localDirInd.Count * 2.0)
                        {
                            if (memoNormal.Any(n => Math.Abs(a.Normal.dotProduct(n) - 1) < 0.000001 ||
                                Math.Abs(newNorm.dotProduct(n) - 1) < 0.000001))
                                continue;
                        }
                        var delete1 =
                            localDirInd.Where(dir => a.Normal.dotProduct(DisassemblyDirections.Directions[dir]) < -0.06).ToList();
                        List<int> delete2;
                        if (Math.Abs(a.Normal.dotProduct(newNorm) - 1) < 0.000001)
                            delete2 = delete1;
                        else
                            delete2 =
                            localDirInd.Where(dir => newNorm.dotProduct(DisassemblyDirections.Directions[dir]) < -0.06).ToList();
                        if (delete1.Count() < delete2.Count())
                        {
                            foreach (var i1 in delete1)
                                localDirInd.Remove(i1);
                            memoNormal.Add(a.Normal);
                        }
                        else
                        {
                            foreach (var i1 in delete2)
                                localDirInd.Remove(i1);
                            memoNormal.Add(newNorm);
                        }
                        //break;
                    }
                }

            }
            certainty = finalProb;
            if (counter1 == 0) return false;
            var primOver = new List<PrimitiveSurface[]>();
            foreach (var tto in triTriOver)
            {
                var prim1 =
                    DisassemblyDirectionsWithFastener.SolidPrimitive[solid1].Where(sp => sp.Faces.Contains(tto[0])).ToList();
                var prim2 =
                    DisassemblyDirectionsWithFastener.SolidPrimitive[solid2].Where(sp => sp.Faces.Contains(tto[1])).ToList();
                if (!prim1.Any() || !prim2.Any()) continue;
                if (primOver.Any(p => p[0] == prim1[0] && p[1] == prim2[0])) continue;
                primOver.Add(new[] { prim1[0], prim2[0] });
            }
            FilteroutBadPrimitiveProximities(primOver);
            OverlappingSurfaces.Add(new OverlappedSurfaces { Solid1 = solid1, Solid2 = solid2, Overlappings = primOver });
            return true;
        }

        private static void FilteroutBadPrimitiveProximities(List<PrimitiveSurface[]> primOver)
        {
            for (var i = 0; i < primOver.Count; i++)
            {
                var pp = primOver[i];
                if (pp[0] is Cylinder || pp[0] is Cone || pp[0] is Sphere)
                {
                    if (pp[0].Faces.Count < 7)
                    {
                        primOver.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
                if (pp[1] is Cylinder || pp[1] is Cone || pp[1] is Sphere)
                {
                    if (pp[1].Faces.Count < 7)
                    {
                        primOver.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
                if (pp[0] is Flat && pp[1] is Cylinder)
                {
                    var cy = (Cylinder)pp[1];
                    if (!cy.IsPositive)
                    {
                        primOver.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
                if (pp[0] is Cylinder && pp[1] is Flat)
                {
                    var cy = (Cylinder)pp[0];
                    if (!cy.IsPositive)
                    {
                        primOver.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
                if (pp[0] is Flat && pp[1] is Cone)
                {
                    var cy = (Cone)pp[1];
                    if (!cy.IsPositive)
                    {
                        primOver.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
                if (pp[0] is Cone && pp[1] is Flat)
                {
                    var cy = (Cone)pp[0];
                    if (!cy.IsPositive)
                    {
                        primOver.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
            }
        }

        internal static bool ProximityFastener(TessellatedSolid solid1, TessellatedSolid solid2)
        {
            var OverlapAABBPartitions = new List<PartitionAABB[]>();
            PartitionOverlapFinder(PartitioningSolid.PartitionsAABB[solid1], PartitioningSolid.PartitionsAABB[solid2], OverlapAABBPartitions);
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
                        if (!TriangleOverlapping(a, b)) continue;
                        return true;
                    }
                }
            }
            if (counter3 > 0)
                Fastener.PotentialCollisionOfFastenerAndSolid.Add(solid2.Name);
            else if (counter2 > 0)
                Fastener.PotentialCollisionOfFastenerAndSolidStep2.Add(solid2.Name);
            else if (counter1 > 0)
                Fastener.PotentialCollisionOfFastenerAndSolidStep3.Add(solid2.Name);
            return false;
        }

        internal static bool TriangleOverlapping(PolygonalFace tri1, PolygonalFace tri2)
        {
            var edges1 = new HashSet<Vertex[]>();
            var edges2 = new HashSet<Vertex[]>();
            // project tri2 on tri1 plane
            edges1.Add(new[] { tri1.Vertices[0], tri1.Vertices[1] });
            edges1.Add(new[] { tri1.Vertices[0], tri1.Vertices[2] });
            edges1.Add(new[] { tri1.Vertices[1], tri1.Vertices[2] });

            var newTri2Verts = new Vertex[3];
            for (int i = 0; i < tri2.Vertices.Count; i++)
            {
                var ver = tri2.Vertices[i];
                var w = ver.Position.subtract(tri1.Vertices[0].Position);
                var s1 = (tri1.Normal.dotProduct(w)) / (tri1.Normal.dotProduct(tri2.Normal));
                newTri2Verts[i] = new Vertex(new[]
                {
                    ver.Position[0] - s1*tri2.Normal[0], ver.Position[1] - s1*tri2.Normal[1],
                    ver.Position[2] - s1*tri2.Normal[2]
                });
            }
            edges2.Add(new[] { newTri2Verts[0], newTri2Verts[1] });
            edges2.Add(new[] { newTri2Verts[0], newTri2Verts[2] });
            edges2.Add(new[] { newTri2Verts[1], newTri2Verts[2] });
            return edges1.Any(movEdge => edges2.Any(refEdge => DoIntersect(movEdge, refEdge)));
        }

        private static bool DoIntersect(Vertex[] movEdge, Vertex[] refEdge)
        {
            var p1 = movEdge[0];
            var q1 = movEdge[1];
            var p2 = refEdge[0];
            var q2 = refEdge[1];

            // Find the four orientations
            var o1 = CrossP(p1, q1, p2);
            var o2 = CrossP(p1, q1, q2);
            var o3 = CrossP(p2, q2, p1);
            var o4 = CrossP(p2, q2, q1);
            if (o1[0] < 1e-5 && o1[1] < 1e-5 && o1[2] < 1e-5 && OnSegment(p1, p2, q1)) return true;
            if (o2[0] < 1e-5 && o2[1] < 1e-5 && o2[2] < 1e-5 && OnSegment(p1, q2, q1)) return true;
            if (o3[0] < 1e-5 && o3[1] < 1e-5 && o3[2] < 1e-5 && OnSegment(p2, p1, q2)) return true;
            if (o4[0] < 1e-5 && o4[1] < 1e-5 && o4[2] < 1e-5 && OnSegment(p2, q1, q2)) return true;
            if (Math.Abs(o1.normalize().dotProduct(o2.normalize()) + 1) < 0.0001 &&
                Math.Abs(o3.normalize().dotProduct(o4.normalize()) + 1) < 0.0001)
                return true;
            return false;
        }
        private static bool OnSegment(Vertex p, Vertex q, Vertex r)
        {
            if (q.Position[0] <= Math.Max(p.Position[0], r.Position[0]) &&
                q.Position[0] >= Math.Min(p.Position[0], r.Position[0]) &&
                q.Position[1] <= Math.Max(p.Position[1], r.Position[1]) &&
                q.Position[1] >= Math.Min(p.Position[1], r.Position[1]) &&
                q.Position[2] <= Math.Max(p.Position[2], r.Position[2]) &&
                q.Position[2] >= Math.Min(p.Position[2], r.Position[2]))
                return true;

            return false;
        }

        private static double[] CrossP(Vertex p, Vertex q, Vertex r)
        {
            return p.Position.subtract(r.Position).crossProduct(q.Position.subtract(r.Position));
        }
        private static void PartitionOverlapFinder(PartitionAABB[] partitionAABB1, PartitionAABB[] partitionAABB2, List<PartitionAABB[]> overlapAabbPartitions)
        {
            foreach (var prtn1 in partitionAABB1)
            {
                foreach (var prtn2 in partitionAABB2)
                {
                    if (BoundingBoxOverlap(prtn1.X[0], prtn1.X[1], prtn1.Y[0], prtn1.Y[1], prtn1.Z[0], prtn1.Z[1],
                        prtn2.X[0], prtn2.X[1], prtn2.Y[0], prtn2.Y[1], prtn2.Z[0], prtn2.Z[1]))
                    {
                        if (prtn1.InnerPartition != null && prtn2.InnerPartition != null)
                        {
                            PartitionOverlapFinder(prtn1.InnerPartition, prtn2.InnerPartition, overlapAabbPartitions);
                            continue;
                        }
                        if (prtn1.InnerPartition == null && prtn2.InnerPartition == null)
                        {
                            // add them to list here
                            overlapAabbPartitions.Add(new[] { prtn1, prtn2 });
                            continue;
                        }
                        if (prtn1.InnerPartition == null && prtn2.InnerPartition != null)
                        {
                            PartitionOverlapFinder(new[] { prtn1 }, prtn2.InnerPartition, overlapAabbPartitions);
                            continue;
                        }
                        if (prtn1.InnerPartition != null && prtn2.InnerPartition == null)
                        {
                            PartitionOverlapFinder(prtn1.InnerPartition, new[] { prtn2 }, overlapAabbPartitions);
                            continue;
                        }
                    }
                }
            }
        }
        internal static bool BoundingBoxOverlap(double xminA, double xmaxA, double yminA, double ymaxA, double zminA, double zmaxA,
    double xminB, double xmaxB, double yminB, double ymaxB, double zminB, double zmaxB)
        {
            var aveXLength = (Math.Abs(xmaxA - xminA) + Math.Abs(xmaxB - xminB)) / 2.0;
            var aveYLength = (Math.Abs(ymaxA - yminA) + Math.Abs(ymaxB - yminB)) / 2.0;
            var aveZLength = (Math.Abs(zmaxA - zminA) + Math.Abs(zmaxB - zminB)) / 2.0;
            // There are some cases that two boxes are touching each other. So the bounding box or the CVH must not
            // return false. Define a threshold:
            if (xminA > xmaxB + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveXLength
                || yminA > ymaxB + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveYLength
                || zminA > zmaxB + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveZLength
                || xminB > xmaxA + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveXLength
                || yminB > ymaxA + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveYLength
                || zminB > zmaxA + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveZLength)
                return false;
            return true;
        }
        private static bool ParallelNormals(designGraph assemblyGraph, TessellatedSolid solid1, TessellatedSolid solid2)
        {
            var dir1 = VariableOfTheIndex(DisConstants.GearNormal, assemblyGraph[solid1.Name].localVariables);
            var dir2 = VariableOfTheIndex(DisConstants.GearNormal, assemblyGraph[solid1.Name].localVariables);
            return 1 - Math.Abs(dir1.dotProduct(dir2)) < OverlappingFuzzification.EqualToZeroL;
        }

        private static double[] VariableOfTheIndex(double p, List<double> vars)
        {
            var ind = vars.IndexOf(p);
            return new[] { vars[ind + 1], vars[ind + 2], vars[ind + 3] };

        }

        private static List<int> GearNormal(designGraph assemblyGraph, TessellatedSolid solid1)
        {
            var dir = VariableOfTheIndex(DisConstants.GearNormal, assemblyGraph[solid1.Name].localVariables);
            return NormalIndexInGlobalDirns(dir);
        }

        private static bool GearGear(designGraph assemblyGraph, TessellatedSolid solid1, TessellatedSolid solid2)
        {
            return assemblyGraph[solid1.Name].localLabels.Contains(DisConstants.Gear) &&
                   assemblyGraph[solid2.Name].localLabels.Contains(DisConstants.Gear);
        }

        internal static bool ConvexHullOverlap(TessellatedSolid a, TessellatedSolid b)
        {
            foreach (var f in a.ConvexHull.Faces)
            {
                var dStar = (f.Normal.dotProduct(f.Vertices[0].Position));
                if (b.ConvexHull.Vertices.All(pt => (f.Normal.dotProduct(pt.Position)) > dStar + 0.001)) // 0.001
                    return false;
            }
            foreach (var f in b.ConvexHull.Faces)
            {
                var dStar = (f.Normal.dotProduct(f.Vertices[0].Position));
                if (a.ConvexHull.Vertices.All(pt => (f.Normal.dotProduct(pt.Position)) > dStar + 0.001)) // 0.001
                    return false;
            }
            return true;
        }

        internal static bool BoundingBoxOverlap(TessellatedSolid a, TessellatedSolid b)
        {
            var aveXLength = (Math.Abs(a.XMax - a.XMin) + Math.Abs(b.XMax - b.XMin)) / 2.0;
            var aveYLength = (Math.Abs(a.YMax - a.YMin) + Math.Abs(b.YMax - b.YMin)) / 2.0;
            var aveZLength = (Math.Abs(a.ZMax - a.ZMin) + Math.Abs(b.ZMax - b.ZMin)) / 2.0;
            // There are some cases that two boxes are touching each other. So the bounding box or the CVH must not
            // return false. Define a threshold:
            if (a.XMin > b.XMax + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveXLength
                || a.YMin > b.YMax + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveYLength
                || a.ZMin > b.ZMax + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveZLength
                || b.XMin > a.XMax + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveXLength
                || b.YMin > a.YMax + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveYLength
                || b.ZMin > a.ZMax + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveZLength)
                return false;
            return true;
        }

        internal static List<int> NormalIndexInGlobalDirns(double[] p)
        {
            var dirs =
                DisassemblyDirections.Directions.Where(
                    globalDirn => 1 - Math.Abs(p.dotProduct(globalDirn)) < OverlappingFuzzification.EqualToZeroL)
                    .ToList();
            return dirs.Select(dir => DisassemblyDirections.Directions.IndexOf(dir)).ToList();
        }


        private static bool ProximityBeta(TessellatedSolid solid1, TessellatedSolid solid2, List<int> localDirInd)
        {
            var counter1 = 0;
            var counter2 = 0;
            var counter3 = 0;
            var s = new Stopwatch();
            s.Start();
            for (int i = 0; i < solid1.Faces.Length; i++)
            {
                var a = solid1.Faces[i];
                for (int j = 0; j < solid2.Faces.Length; j++)
                {
                    var b = solid2.Faces[j];
                    if (Math.Abs(a.Normal.dotProduct(b.Normal) + 1) > 0.0055) continue; // 0.0055
                    counter2 ++;
                    var q = a.Center;
                    var p = b.Center;
                    var pq = new[] {q[0] - p[0], q[1] - p[1], q[2] - p[2]};
                    if (Math.Abs(pq.dotProduct(a.Normal)) > 0.011) continue; //0.11 //0.005
                    counter3++;
                    /*var overl = true;
                    foreach (var edge in a.Edges)
                    {
                        var edgeVector = edge.Vector;
                        var third = a.Vertices.First(ad => ad != edge.From && ad != edge.To).Position;
                        var checkVec = third.subtract(edge.From.Position);
                        double[] cross1 = edgeVector.crossProduct(checkVec);
                        var c = 0;
                        foreach (var vertexB in b.Vertices)
                        {
                            var newVec = vertexB.Position.subtract(edge.From.Position);
                            var cross2 = edgeVector.crossProduct(newVec);
                            if ((Math.Sign(cross1[0]) != Math.Sign(cross2[0]) ||
                                 (Math.Sign(cross1[0]) == 0 && Math.Sign(cross2[0]) == 0)) &&
                                (Math.Sign(cross1[1]) != Math.Sign(cross2[1]) ||
                                 (Math.Sign(cross1[1]) == 0 && Math.Sign(cross2[1]) == 0)) &&
                                (Math.Sign(cross1[2]) != Math.Sign(cross2[2]) ||
                                 (Math.Sign(cross1[2]) == 0 && Math.Sign(cross2[2]) == 0)))
                            {
                                c++;
                            }
                        }
                        if (c == 3)
                        {
                            overl = false;
                            break;
                        }
                    }
                    if (!overl) continue;
                    foreach (var edge in b.Edges)
                    {
                        var edgeVector = edge.Vector;
                        var third = b.Vertices.First(ad => ad != edge.From && ad != edge.To).Position;
                        var checkVec = third.subtract(edge.From.Position);
                        double[] cross1 = edgeVector.crossProduct(checkVec);
                        var c = 0;
                        foreach (var vertexB in a.Vertices)
                        {
                            var newVec = vertexB.Position.subtract(edge.From.Position);
                            var cross2 = edgeVector.crossProduct(newVec);
                            if ((Math.Sign(cross1[0]) != Math.Sign(cross2[0]) ||
                                 (Math.Sign(cross1[0]) == 0 && Math.Sign(cross2[0]) == 0)) &&
                                (Math.Sign(cross1[1]) != Math.Sign(cross2[1]) ||
                                 (Math.Sign(cross1[1]) == 0 && Math.Sign(cross2[1]) == 0)) &&
                                (Math.Sign(cross1[2]) != Math.Sign(cross2[2]) ||
                                 (Math.Sign(cross1[2]) == 0 && Math.Sign(cross2[2]) == 0)))
                            {
                                c++;
                            }
                        }
                        if (c == 3)
                        {
                            overl = false;
                            break;
                        }
                    }
                    if (!overl) continue;*/
                    var aProj = _3Dto2D.Get2DProjectionPoints(a.Vertices, a.Normal);
                    var a2D = new _3Dto2D { Points = aProj, Edges = _3Dto2D.Get2DEdgesFromFace(a, aProj) };
                    var bProj = _3Dto2D.Get2DProjectionPoints(b.Vertices, a.Normal);
                    var b2D = new _3Dto2D { Points = bProj, Edges = _3Dto2D.Get2DEdgesFromFace(b, bProj) };
                    if (!NonadjacentBlockingDeterminationPro.IsItBlocked(a2D, b2D)) continue;
                    counter1++;
                    for (var k = 0; k < localDirInd.Count; k++)
                    {
                        var dir = DisassemblyDirections.Directions[localDirInd[k]];
                        if (a.Normal.dotProduct(dir) < -0.04)
                        {
                            localDirInd.Remove(localDirInd[k]);
                            k--;
                        }
                    }
                    break;
                }
            }
            // Merge removal directions:
            var final = new List<int>();
            foreach (var i in localDirInd)
            {
                if (
                    final.Any(
                        d =>
                            1 - DisassemblyDirections.Directions[d].dotProduct(DisassemblyDirections.Directions[i]) <
                            0.05)) continue;
                final.Add(i);
            }
            s.Stop();
            Console.WriteLine(s.Elapsed);
            return true;
        }
    }

    internal class OverlappedSurfaces
    {
        // This class is written for Weifeng's stability code
        /// <summary>
        /// Gets or sets the Solid1.
        /// </summary>
        /// <value>The Solid1.</value>
        internal TessellatedSolid Solid1 { set; get; }

        /// <summary>
        /// Gets or sets the Solid2.
        /// </summary>
        /// <value>The Solid2.</value>
        internal TessellatedSolid Solid2 { set; get; }

        /// <summary>
        /// The first element of array is surface of the Solid1 and
        /// the second one is surface of Solid2
        /// </summary>
        /// <value>The Overlapping surfaces</value>
        internal List<PrimitiveSurface[]> Overlappings {set; get;} 

    }
}
