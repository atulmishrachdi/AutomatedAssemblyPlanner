using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using BaseClasses;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using BaseClasses.Representation;
using StarMathLib;
using TVGL;

namespace Geometric_Reasoning
{
    public class NonadjacentBlockingWithPartitioning
    {
        // Dictonary to store CVH faces of each solid into a HashSet. So it is easier to 
        internal static Dictionary<TessellatedSolid, HashSet<PolygonalFace>> CvhHashSet =
            new Dictionary<TessellatedSolid, HashSet<PolygonalFace>>();
        internal static Dictionary<TessellatedSolid, HashSet<PolygonalFace>> ObbFacesHashSet =
            new Dictionary<TessellatedSolid, HashSet<PolygonalFace>>();
        public static Dictionary<string, TVGLConvexHull> CombinedCVHForMultipleGeometries;
        // This class is added as an alternative for current Nonadjacent blocking determination approach.
        // The overal approach is the same as before (ray shooting), but number of both rays and blocking 
        // triangles are droped to speedup the function.
        // Rays: Instead of checking blockings for every direction, for every two parts, their possible
        //       blocking directions are found based upon the planes that can seperate the two CVHs linearlly.
        //       (If the CVHs are not linearly seperable we cannot apply this.)
        // Triangles: Number of triangles (of the blocking solid) is the most affecting factor in blocking
        //       determination. Code gets really really slow when it goes to check intersection of the ray
        //       and all the triangles of the solid. We are avoiding this problem here by partitionaning
        //       our search space into k number of sections obtained originally from OBB of the solid.
        public static void Run(designGraph graph, Dictionary<string, List<TessellatedSolid>> subAssems, List<int> gDir)
        {
            Console.WriteLine("\n\nNonadjacent Blocking Determination is running ....");
            long totalCases = 0;
            var subAssemsToList = subAssems.ToList();
            for (var i = 0; i < subAssems.Count - 1; i++)
            {
                var subAssem1 = subAssemsToList[i];
                for (var j = i + 1; j < subAssems.Count; j++)
                {
                    var subAssem2 = subAssemsToList[j];
                    var tri2Sub1 = subAssem1.Value.Sum(s => s.Faces.Length);
                    var tri2Sub2 = subAssem2.Value.Sum(s => s.Faces.Length);
                    totalCases += tri2Sub1 * tri2Sub2;
                }
            }
            ObbFacesHashSet = new Dictionary<TessellatedSolid, HashSet<PolygonalFace>>();
            CombinedCVHForMultipleGeometries = new Dictionary<string, TVGLConvexHull>();

            long counter = 0;
            foreach (var subAssem in subAssems)
            {
                foreach (var s in subAssem.Value)
                {
                    //CvhHashSet.Add(s, new HashSet<PolygonalFace>(s.ConvexHull.Faces));
                    ObbFacesHashSet.Add(s,
                        new HashSet<PolygonalFace>(
                            PartitioningSolid.TwelveFaceGenerator(
                                BoundingGeometry.OrientedBoundingBoxDic.First(b=> b.Key.Name == s.Name).Value.CornerVertices.Select(
                                    cv => new Vertex(cv.Position)).ToArray())));
                }
            }

            CreateCombinedCVHs(subAssems);

            

            var solidsL = subAssems.ToList();


            int width = 55;
            int total = (solidsL.Count + 1) * (solidsL.Count / 2);
            int refresh = (int)Math.Ceiling(((float)total) / ((float)(width)));
            int check = 0;
            //LoadingBar.start(width, 0);


            for (var i = 0; i < solidsL.Count; i++)
            {
                var solidMoving = solidsL[i].Value;
                for (var j = i + 1; j < solidsL.Count; j++)
                {
                    
                    if (check % refresh == 0)
                    {
                        //LoadingBar.refresh(width, ((float)check) / ((float)total));
                    }
                    check++;

                    var blocked = false;
                    // check the convex hull of these two solids to find the planes tha can linearly seperate them
                    // solid1 is moving and solid2 is blocking
                    var solidBlocking = solidsL[j].Value;
                    counter += solidMoving.Sum(s => s.Faces.Length) * solidBlocking.Sum(s => s.Faces.Length);
                    if (
                        graph.arcs.Any(
                            a => a is Connection &&
                                 ((a.From.name == solidsL[i].Key && a.To.name == solidsL[j].Key) ||
                                  (a.From.name == solidsL[j].Key && a.To.name == solidsL[i].Key))))
                    {
                        continue;
                    }
                    // Add a secondary arc to the 
                    var from = GetNode(graph, solidsL[i].Key);
                    var to = GetNode(graph, solidsL[j].Key);
                    graph.addArc(from, to, from.name + to.name, typeof(SecondaryConnection));
                    var lastAddedSecArc = (SecondaryConnection)graph.arcs.Last();
                    var filteredDirections = FilterGlobalDirections(solidMoving, solidBlocking, gDir);
                    var oppositeFiltrdDirs = filteredDirections.Select(d => StartProcess.DirectionsAndOppositsForGlobalpool[d]).ToList();
                    // remember this: if solid2 is not blocking solid1, we need to check if solid1 is blocking 2 in the opposite direction.
                    // if filteredDirections.Count == gDir.Count then the CVHs overlap
                    // Only directions need to be checked which the moving part can move along them:
                    var scndFilteredDirectionsMoving = FinalSetOfDirectionsFinder(graph, solidMoving, filteredDirections);
                    var scndFilteredDirectionsBlocking = new List<int>();
                    scndFilteredDirectionsBlocking = FinalSetOfDirectionsFinder(graph, solidBlocking,
                        filteredDirections.Count == gDir.Count ? filteredDirections : oppositeFiltrdDirs);
                    foreach (
                        var d in
                            scndFilteredDirectionsMoving.Where(
                                d =>
                                    !scndFilteredDirectionsBlocking.Contains(
                                        StartProcess.DirectionsAndOppositsForGlobalpool[d])))
                        scndFilteredDirectionsBlocking.Add(StartProcess.DirectionsAndOppositsForGlobalpool[d]);
                    foreach (
                        var d in
                            scndFilteredDirectionsBlocking.Where(
                                d =>
                                    !scndFilteredDirectionsMoving.Contains(
                                        StartProcess.DirectionsAndOppositsForGlobalpool[d])))
                        scndFilteredDirectionsMoving.Add(StartProcess.DirectionsAndOppositsForGlobalpool[d]);
                    if (filteredDirections.Count == gDir.Count)
                    {
                        //continue;
                        Parallel.ForEach(scndFilteredDirectionsMoving, filtDir =>
                        //foreach (var filtDir in filteredDirections)
                        {
                            var direction = StartProcess.Directions[filtDir];
                            blocked = BlockingDeterminationWithCvhOverlapping(direction, solidMoving, solidBlocking);
                            if (blocked)
                            {
                                lock (lastAddedSecArc.Directions)
                                    lastAddedSecArc.Directions.Add(filtDir);
                                if (
                                    scndFilteredDirectionsBlocking.Contains(
                                        StartProcess.DirectionsAndOppositsForGlobalpool[filtDir]))
                                    scndFilteredDirectionsBlocking.Remove(
                                        StartProcess.DirectionsAndOppositsForGlobalpool[filtDir]);
                            }
                        });
                        Parallel.ForEach(scndFilteredDirectionsBlocking, filtDir =>
                        //foreach (var filtDir in filteredDirections)
                        {
                            var direction = StartProcess.Directions[filtDir];
                            blocked = BlockingDeterminationWithCvhOverlapping(direction, solidBlocking, solidMoving);
                            if (blocked)
                            {
                                lock (lastAddedSecArc.Directions)
                                    lastAddedSecArc.Directions.Add(StartProcess.DirectionsAndOppositsForGlobalpool[filtDir]);
                            }
                        });
                        if (lastAddedSecArc.Directions.Count == 0)
                            graph.removeArc(lastAddedSecArc);
                    }
                    else
                    {
                        //continue;
                        // If CVHs dont overlap:
                        Parallel.ForEach(scndFilteredDirectionsMoving, filtDir =>
                        //foreach (var filtDir in filteredDirections)
                        {
                            var direction = StartProcess.Directions[filtDir];
                            blocked = BlockingDeterminationNoCvhOverlapping(direction, solidMoving, solidBlocking);
                            if (blocked)
                            {
                                lock (lastAddedSecArc.Directions)
                                    lastAddedSecArc.Directions.Add(filtDir);
                                if (
                                    scndFilteredDirectionsBlocking.Contains(
                                        StartProcess.DirectionsAndOppositsForGlobalpool[filtDir]))
                                    scndFilteredDirectionsBlocking.Remove(
                                        StartProcess.DirectionsAndOppositsForGlobalpool[filtDir]);
                            }
                        });
                        Parallel.ForEach(scndFilteredDirectionsBlocking, filtDir =>
                        //foreach (var filtDir in filteredDirections)
                        {
                            var direction = StartProcess.Directions[filtDir];
                            blocked = BlockingDeterminationNoCvhOverlapping(direction, solidBlocking, solidMoving);
                            if (blocked)
                            {
                                lock (lastAddedSecArc.Directions)
                                    lastAddedSecArc.Directions.Add(StartProcess.DirectionsAndOppositsForGlobalpool[filtDir]);
                            }
                        });
                        if (lastAddedSecArc.Directions.Count == 0)
                            graph.removeArc(lastAddedSecArc);
                    }
                }
            }
            //LoadingBar.refresh(width, 1);
            CreateSameDirectionDictionary(gDir);
        }

        private static node GetNode(designGraph graph, string solidName)
        {
            return graph.nodes.Where(n => n.name == solidName).Cast<Component>().ToList()[0];
        }

        private static void CreateSameDirectionDictionary(List<int> gDir)
        {
            StartProcess.DirectionsAndSame = new Dictionary<int, HashSet<int>>();
            foreach (var gD in gDir)
            {
                if (StartProcess.DirectionsAndSame.ContainsKey(gD)) continue;
                var sameDirs =
                    gDir.Where(
                        d =>
                            /*d != gD &&*/
                            Math.Abs(1 -
                                     StartProcess.Directions[d].dotProduct(StartProcess.Directions[gD])) <
                            OverlappingFuzzification.CheckWithGlobDirsParall2);

                StartProcess.DirectionsAndSame.Add(gD, new HashSet<int>(sameDirs));
            }
        }

        private static List<int> FinalSetOfDirectionsFinder(designGraph graph, List<TessellatedSolid> solid, List<int> filteredDirections)
        {
            var dirs = new List<int>();
            var arcFrom = graph.arcs.Where(a => a is Connection).Cast<Connection>().Where(a => a.From.name == solid[0].Name).ToList();
            //var dirs = arcFrom.SelectMany(a => a.InfiniteDirections).Where(filteredDirections.Contains).ToList();
            //dirs.AddRange(arcFrom.SelectMany(a => a.FiniteDirections).Where(filteredDirections.Contains));
            foreach (var c in arcFrom)
            {
                foreach (var i in c.InfiniteDirections.Where(filteredDirections.Contains))
                {
                    //var oppos = DisassemblyDirections.DirectionsAndOppositsForGlobalpool[i];
                    if (dirs.Contains(i)) continue;
                    dirs.Add(i);
                }
                foreach (var i in c.FiniteDirections.Where(filteredDirections.Contains))
                {
                    //var oppos = DisassemblyDirections.DirectionsAndOppositsForGlobalpool[i];
                    if (dirs.Contains(i)) continue;
                    dirs.Add(i);
                }
            }
            var arcTo = graph.arcs.Where(a => a is Connection).Cast<Connection>().Where(a => a.To.name == solid[0].Name).ToList();
            foreach (var c in arcTo)
            {
                foreach (var i in c.InfiniteDirections.Where(i => filteredDirections.Contains(StartProcess.DirectionsAndOppositsForGlobalpool[i])))
                {
                    var oppos = StartProcess.DirectionsAndOppositsForGlobalpool[i];
                    if (dirs.Contains(oppos)) continue;
                    dirs.Add(oppos);
                }
                foreach (var i in c.FiniteDirections.Where(i => filteredDirections.Contains(StartProcess.DirectionsAndOppositsForGlobalpool[i])))
                {
                    var oppos = StartProcess.DirectionsAndOppositsForGlobalpool[i];
                    if (dirs.Contains(oppos)) continue;
                    dirs.Add(oppos);
                }
            }
            //dirs.AddRange(arcTo.SelectMany(a => a.FiniteDirections).Where(d => filteredDirections.Contains(DisassemblyDirections.DirectionsAndOppositsForGlobalpool[d])));
            //dirs.AddRange(arcTo.SelectMany(a => a.InfiniteDirections).Where(d => filteredDirections.Contains(DisassemblyDirections.DirectionsAndOppositsForGlobalpool[d])));
            return dirs;
        }
        private static bool BlockingDeterminationWithCvhOverlapping(double[] direction, List<TessellatedSolid> subAssemMoving,
            List<TessellatedSolid> subAssemBlocking)
        {
            var rays = RayGeneratorForCVHOverlap(subAssemMoving, direction);
            foreach (var ray in rays)
            {
                foreach (var solidBlocking in subAssemBlocking)
                {
                    if (RayIntersectsCVHOverlap(PartitioningSolid.Partitions[solidBlocking], ray))
                        return true;
                }
            }
            return false;
        }

        private static bool RayIntersectsCVHOverlap(Partition[] partitions, Ray ray)
        {
            var memoFace = new HashSet<PolygonalFace>();
            var affectedPartitions = AffectedPartitionsWithRayCvhOverlapsNABD(partitions, ray);
            foreach (var prtn in affectedPartitions)
            {
                if (prtn.InnerPartition != null)
                    RayIntersectsCVHOverlap(prtn.InnerPartition, ray);
                foreach (var tri in prtn.SolidTriangles.Where(t => !memoFace.Contains(t)))
                {
                    memoFace.Add(tri);
                    if (!GeometryFunctions.RayIntersectsWithFaceNABD(ray, tri))
                        continue;
                    return true;
                }
            }
            return false;
            //if (!NonadjacentBlockingDetermination._2DProjectionCheck(solidMoving, solidBlocking, direction))
            //    boo = false;
        }

        public static void CreateCombinedCVHs(Dictionary<string, List<TessellatedSolid>> subAssems)
        {
            foreach (var subAssem in subAssems)
            {
                if (subAssem.Value.Count == 1)
                    CombinedCVHForMultipleGeometries.Add(subAssem.Key, subAssem.Value[0].ConvexHull);
                else
                {
                    var vers = subAssem.Value.SelectMany(s => s.ConvexHull.Vertices);
                    CombinedCVHForMultipleGeometries.Add(subAssem.Key, new TVGLConvexHull(vers.ToArray(),subAssem.Value[0].SameTolerance));
                }
            }
        }

        private static bool BlockingDeterminationNoCvhOverlapping(Double[] direction, List<TessellatedSolid> subAssemMoving,
            List<TessellatedSolid> subAssemBlocking)
        {
            //direction = DisassemblyDirections.Directions[0];
            var rays = RayGeneratorForNoCVHOverlap(subAssemMoving, direction);
            foreach (var ray in rays)
            {
                foreach (var solidBlocking in subAssemBlocking)
                {
                    if (!ObbFacesHashSet[solidBlocking].Any(
                        f => RayIntersectsForObb(ray, f))) continue;
                    if (RayIntersects(PartitioningSolid.Partitions[solidBlocking], ray))
                        return true;
                }
            }
            return false;
        }

        private static bool RayIntersects(Partition[] partition, Ray ray)
        {
            var memoFace = new HashSet<PolygonalFace>();
            var affectedPartitions = AffectedPartitionsWithRayNABD(partition, ray);
            // I can have a sorting command here to sort affected partitions based on the number of faces they have(?)
            foreach (var prtn in affectedPartitions)
            {
                if (prtn.InnerPartition != null)
                    RayIntersects(prtn.InnerPartition, ray);
                foreach (var tri in prtn.SolidTriangles.Where(t => !memoFace.Contains(t) && t.Vertices.Count == 3))
                {
                    memoFace.Add(tri);
                    if (!GeometryFunctions.RayIntersectsWithFaceNABD(ray, tri))
                        continue;
                    return true;
                }
            }
            return false;
        }

        private static List<Partition> AffectedPartitionsWithRay(Partition[] partition, Ray ray)
        {
            return
                partition.Where(
                    prtn =>
                        prtn.SolidTriangles.Count > 0 &&
                        prtn.Faces.Any(f => GeometryFunctions.RayIntersectsWithFace(ray, f))).ToList();
        }

        internal static List<Partition> AffectedPartitionsWithRayCvhOverlapsNABD(Partition[] partitions, Ray ray)
        {
            var affected = partitions.Where(
                prtn =>
                    prtn.SolidTriangles.Count > 0 &&
                    prtn.Faces.Any(f => GeometryFunctions.RayIntersectsWithFaceNABD(ray, f))).ToList();
            var currentPartition =
                partitions.Where(p => PartitioningSolid.IsVertexInsidePartition(p, new Vertex(ray.Position)));
            affected.AddRange(currentPartition);
            return affected;
        }

        internal static List<Partition> AffectedPartitionsWithRayCvhOverlaps(Partition[] partitions, Ray ray)
        {
            var affected = partitions.Where(
                prtn =>
                    prtn.SolidTriangles.Count > 0 &&
                    prtn.Faces.Any(f => GeometryFunctions.RayIntersectsWithFace(ray, f))).ToList();
            var currentPartition =
                partitions.Where(p => PartitioningSolid.IsVertexInsidePartition(p, new TVGL.Vertex(ray.Position)));
            affected.AddRange(currentPartition);
            return affected;
        }

        private static List<Partition> AffectedPartitionsWithRayNABD(Partition[] partition, Ray ray)
        {
            return
                partition.Where(
                    prtn =>
                        prtn.SolidTriangles.Count > 0 &&
                        prtn.Faces.Any(f => GeometryFunctions.RayIntersectsWithFaceNABD(ray, f))).ToList();
        }

        private static List<int> FilterGlobalDirections(List<TessellatedSolid> subAssem1, List<TessellatedSolid> subAssem2,
            List<int> gDir)
        {
            // If there is a plane that can seperate these two CVHs, then we only need to keep the directions
            // that have a positive dot prodoct with the normal of the plane.
            var subAssem1CVHVerts = CombinedCVHForMultipleGeometries[subAssem1[0].Name].Vertices;
            var subAssem2CVHVerts = CombinedCVHForMultipleGeometries[subAssem2[0].Name].Vertices;
            var subAssem1CVHFaces = CombinedCVHForMultipleGeometries[subAssem1[0].Name].Faces;
            var subAssem2CVHFaces = CombinedCVHForMultipleGeometries[subAssem2[0].Name].Faces;


            var filteredGlobDirs1 = new List<int>(gDir);
            var filteredGlobDirs2 = new List<int>(gDir);
            //var visitedCvhFaces = new HashSet<PolygonalFace>();
            foreach (
                var s1CvhFace in
                    subAssem1CVHFaces.Where(
                        s1F =>
                            subAssem2CVHVerts.All(
                                s2V =>
                                    s1F.Normal.dotProduct(s2V.Position.subtract(s1F.Vertices[0].Position)) > -0.0001) /*&&
                            visitedCvhFaces.All(visF => Math.Abs(s1F.Normal.dotProduct(visF.Normal) - 1) < 0.0005)*/))
            {
                filteredGlobDirs1 =
                    filteredGlobDirs1.Where(gD => StartProcess.Directions[gD].dotProduct(s1CvhFace.Normal) > 0)
                        .ToList();
                //visitedCvhFaces.Add(s1CvhFace);
                break;
            }
            //visitedCvhFaces.Clear();
            foreach (
                var s2CvhFace in
                    subAssem2CVHFaces.Where(
                        s2F =>
                            subAssem1CVHVerts.All(
                                s1V => s2F.Normal.dotProduct(s1V.Position.subtract(s2F.Vertices[0].Position)) > -0.0001) /*&&
                            visitedCvhFaces.All(visF => Math.Abs(s2F.Normal.dotProduct(visF.Normal) - 1) < 0.0005)*/))
            {
                filteredGlobDirs2 =
                    filteredGlobDirs2.Where(gD => StartProcess.Directions[gD].dotProduct(s2CvhFace.Normal) < 0)
                        .ToList();
                //visitedCvhFaces.Add(s2CvhFace);
                break;
            }
            if (filteredGlobDirs1.Count <= filteredGlobDirs2.Count) return filteredGlobDirs1;
            return filteredGlobDirs2;

        }

        private static List<Ray> RayGeneratorForCVHOverlap(List<TessellatedSolid> solidMoving, double[] direction)
        {
            var rays = CombinedCVHForMultipleGeometries[solidMoving[0].Name].Vertices.Select(
                vertex =>
                    new Ray(
                        new Vertex(new[]{vertex.Position[0], vertex.Position[1],
                            vertex.Position[2]}),
                        new[] { direction[0], direction[1], direction[2] })).ToList();
            // and shuffle them:
            return rays.OrderBy(a => Guid.NewGuid()).ToList();
        }
        private static List<Ray> RayGeneratorForNoCVHOverlap(List<TessellatedSolid> solidMoving, double[] direction)
        {
            var rays = CombinedCVHForMultipleGeometries[solidMoving[0].Name].Vertices.Select(
                vertex =>
                    new Ray(
                        new Vertex(new[]{vertex.Position[0], vertex.Position[1],
                            vertex.Position[2]}),
                        new[] { direction[0], direction[1], direction[2] })).ToList();
            // add more vertices to the ray
            rays.AddRange(
                NonadjacentBlockingDetermination.AddingMoreRays(
                    CombinedCVHForMultipleGeometries[solidMoving[0].Name].Edges.Where(
                    e => e != null && e.Length > 2).ToArray(), direction));
            return rays.OrderBy(a => Guid.NewGuid()).ToList();
        }

        public static bool RayIntersectsForObb(Ray ray, PolygonalFace face)
        {
            if (Math.Abs(ray.Direction.dotProduct(face.Normal)) < 0.06) return false;
            if (OnTheWrongSide(ray,face)) return false;
            var point = face.Vertices[0].Position;
            var w = new double[] { ray.Position[0] - point[0], ray.Position[1] - point[1], ray.Position[2] - point[2] };
            var s1 = (face.Normal.dotProduct(w)) / (face.Normal.dotProduct(ray.Direction));
            //var v = new double[] { w[0] + s1 * ray.Direction[0] + point[0], w[1] + s1 * ray.Direction[1] + point[1], w[2] + s1 * ray.Direction[2] + point[2] };
            var v = new double[] { ray.Position[0] - s1 * ray.Direction[0], ray.Position[1] - s1 * ray.Direction[1], ray.Position[2] - s1 * ray.Direction[2] };
            foreach (var corner in face.Vertices)
            {
                var otherCorners = face.Vertices.Where(ver => ver != corner).ToList();
                var v1 = otherCorners[0].Position.subtract(corner.Position);
                var v2 = otherCorners[1].Position.subtract(corner.Position);
                var v0 = v.subtract(corner.Position);
                if (v1.crossProduct(v0).dotProduct(v2.crossProduct(v0)) > -0.15) //   -0.09 
                    return false;
            }
            return true;

        }

        private static bool OnTheWrongSide(Ray ray, PolygonalFace face)
        {
            return ray.Direction.dotProduct(ray.Position.subtract(face.Vertices[0].Position)) > 0;

        }
    }
}
