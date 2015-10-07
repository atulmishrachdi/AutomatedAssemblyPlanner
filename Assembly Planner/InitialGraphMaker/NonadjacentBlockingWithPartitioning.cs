using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using Assembly_Planner.GraphSynth.BaseClasses;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth.Representation;
using StarMathLib;
using TVGL;

namespace Assembly_Planner
{
    class NonadjacentBlockingWithPartitioning
    {
        // Dictonary to store CVH faces of each solid into a HashSet. So it is easier to 
        internal static Dictionary<TessellatedSolid, HashSet<PolygonalFace>> CvhHashSet =
            new Dictionary<TessellatedSolid, HashSet<PolygonalFace>>();
        internal static Dictionary<TessellatedSolid, HashSet<PolygonalFace>> ObbFacesHashSet =
            new Dictionary<TessellatedSolid, HashSet<PolygonalFace>>();
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
        internal static void Run(designGraph graph,
            List<TessellatedSolid> solids, List<int> gDir)
        {
            var stopWat = Stopwatch.StartNew();
            stopWat.Start();
            var solidsL = solids.Where(s => graph.nodes.Any(n => n.name == s.Name)).ToList();
            foreach (var s in solidsL)
            {
                //CvhHashSet.Add(s, new HashSet<PolygonalFace>(s.ConvexHullFaces));
                ObbFacesHashSet.Add(s,
                    new HashSet<PolygonalFace>(PartitioningSolid.TwelveFaceGenerator(PartitioningSolid.OrientedBoundingBoxDic[s].CornerVertices)));
            }
            //solids.Where(s => s.Name.Contains("Interroll-3")).ToList();
            //solidsL.AddRange(solids.Where(s => s.Name.Contains("LexanSide")));
            for (var i = 0; i < solidsL.Count; i++)
            {
                var solidMoving = solidsL[i];
                for (var j = i + 1; j < solidsL.Count; j++)
                {
                    var blocked = false;
                    // check the convex hull of these two solids to find the planes tha can linearly seperate them
                    // solid1 is moving and solid2 is blocking
                    var solidBlocking = solidsL[j];
                    
                    // Add a secondary arc to the 
                    var from = GetNode(graph, solidMoving);
                    var to = GetNode(graph, solidBlocking);
                    graph.addArc(from, to, from.name + to.name, typeof(SecondaryConnection));
                    var lastAddedSecArc = (SecondaryConnection)graph.arcs.Last();
                    if (
                        graph.arcs.Any(
                            a => a is Connection &&
                                ((a.From.name == solidMoving.Name && a.To.name == solidBlocking.Name) ||
                                (a.From.name == solidBlocking.Name && a.To.name == solidMoving.Name)))) continue;
                    var filteredDirections = FilterGlobalDirections(solidMoving,solidBlocking,gDir);
                    // remember this: if solid2 is not blocking solid1, we need to check if solid1 is blocking 2 in the opposite direction.
                    // if filteredDirections.Count == gDir.Count then the CVHs overlap
                    if (filteredDirections.Count == gDir.Count)
                    {
                        //continue;
                        Parallel.ForEach(filteredDirections, filtDir =>
                        //foreach (var filtDir in filteredDirections)
                        {
                            var direction = DisassemblyDirections.Directions[filtDir];
                            blocked = BlockingDeterminationWithCvhOverlapping(direction, solidMoving, solidBlocking);
                            if (!blocked)
                            {
                                // now check solid 1 is blocking solid 2 in opposite direction or not.
                                blocked = BlockingDeterminationWithCvhOverlapping(direction.multiply(-1.0),
                                    solidBlocking, solidMoving);
                            }
                            if (blocked)
                            {
                                lock (lastAddedSecArc.Directions)
                                    lastAddedSecArc.Directions.Add(filtDir);
                            }
                        }
                            );//
                        if (lastAddedSecArc.Directions.Count == 0)
                            graph.removeArc(lastAddedSecArc);
                    }
                    else
                    {
                        //continue;
                        // If CVHs dont overlap:
                        Parallel.ForEach(filteredDirections, filtDir =>
                        //foreach (var filtDir in filteredDirections)
                        {
                            var direction = DisassemblyDirections.Directions[filtDir];
                            blocked = BlockingDeterminationNoCvhOverlapping(direction, solidMoving, solidBlocking);
                            if (!blocked)
                            {
                                // now check solid 1 is blocking solid 2 in opposite direction or not.
                                blocked = BlockingDeterminationNoCvhOverlapping(direction.multiply(-1.0), solidBlocking,
                                    solidMoving);
                            }
                            if (blocked)
                            {
                                lock (lastAddedSecArc.Directions)
                                    lastAddedSecArc.Directions.Add(filtDir);
                            }
                        }
                             );//
                        if (lastAddedSecArc.Directions.Count == 0)
                            graph.removeArc(lastAddedSecArc);
                    }
                }
            }
            stopWat.Stop();
            Console.WriteLine("Nonadjacent Blocking Determination:" + "     " + stopWat.Elapsed);
        }

        private static node GetNode(designGraph graph, TessellatedSolid solid)
        {
            return graph.nodes.Where(n => n.name == solid.Name).Cast<Component>().ToList()[0];
        }

        private static bool BlockingDeterminationWithCvhOverlapping(double[] direction, TessellatedSolid solidMoving,
            TessellatedSolid solidBlocking)
        {
            var boo = false;
            var rays = RayGenerator(solidMoving, direction);
            foreach (var ray in rays)
            {
                var memoFace = new HashSet<PolygonalFace>();
                var affectedPartitions = AffectedPartitionsWithRayCvhOverlaps(solidBlocking, ray);
                //if ()
                //{
                    
                //}
                foreach (var prtn in affectedPartitions)
                {
                    foreach (var tri in prtn.SolidTriangles.Where(t => !memoFace.Contains(t)))
                    {
                        memoFace.Add(tri);
                        if (!NonadjacentBlockingDetermination.RayIntersectsWithFace3(ray, tri))
                            continue;
                        if (NonadjacentBlockingDetermination.DistanceToTheFace(ray.Position, tri) < 0)
                            continue;
                        boo = true;
                        break;
                    }
                    if (boo) break;
                }
                if (!boo) continue;
                //if (!NonadjacentBlockingDetermination._2DProjectionCheck(solidMoving, solidBlocking, direction))
                //    boo = false;
                break;
            }
            return boo;
        }


        private static bool BlockingDeterminationNoCvhOverlapping(Double[] direction, TessellatedSolid solidMoving,
            TessellatedSolid solidBlocking)
        {
            //direction = DisassemblyDirections.Directions[0];
            var rays = RayGenerator(solidMoving, direction);
            foreach (var ray in rays)
            {
                //if (!NonadjacentBlockingDetermination.BoundingBoxBlocking(direction, solidBlocking, solidMoving))
                //    continue;
                //if (!CvhHashSet[solidBlocking].Any(
                //    f => NonadjacentBlockingDetermination.RayIntersectsWithFace3(ray, f))) continue;
                if (!ObbFacesHashSet[solidBlocking].Any(
                    f => RayIntersectsForObb(ray, f))) continue;
                if (RayIntersects(PartitioningSolid.Partitions[solidBlocking], ray))
                    return true;
            }
            return false;
        }

        private static bool RayIntersects(Partition[] partition, Ray ray)
        {
            var memoFace = new HashSet<PolygonalFace>();
            var affectedPartitions = AffectedPartitionsWithRay(partition, ray);
            // I can have a sorting command here to sort affected partitions based on the number of faces they have(?)
            foreach (var prtn in affectedPartitions)
            {
                if (prtn.InnerPartition != null)
                    RayIntersects(prtn.InnerPartition,ray);
                foreach (var tri in prtn.SolidTriangles.Where(t => !memoFace.Contains(t)))
                {
                    memoFace.Add(tri);
                    if (!NonadjacentBlockingDetermination.RayIntersectsWithFace3(ray, tri))
                        continue;
                    if (NonadjacentBlockingDetermination.DistanceToTheFace(ray.Position, tri) < 0)
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
                    prtn => prtn.Faces.Any(f => NonadjacentBlockingDetermination.RayIntersectsWithFace3(ray, f)))
                    .ToList();
        }

        internal static List<Partition> AffectedPartitionsWithRayCvhOverlaps(TessellatedSolid solidBlocking, Ray ray)
        {
            var affected = PartitioningSolid.Partitions[solidBlocking].Where(
                    prtn => prtn.Faces.Any(f => NonadjacentBlockingDetermination.RayIntersectsWithFace3(ray, f)))
                    .ToList();
            var currentPartition =
                PartitioningSolid.Partitions[solidBlocking].Where(
                    p => PartitioningSolid.IsVertexInsidePartition(p, new TVGL.Vertex(ray.Position)));
            affected.AddRange(currentPartition);
            return affected;
        }

        private static List<int> FilterGlobalDirections(TessellatedSolid solid1, TessellatedSolid solid2, List<int> gDir)
        {
            // If there is a plane that can seperate these two CVHs, then we only need to keep the directions
            // that have a positive dot prodoct with the normal of the plane.
            var filteredGlobDirs1 = new List<int>(gDir);
            var filteredGlobDirs2 = new List<int>(gDir);
            var visitedCvhFaces = new HashSet<PolygonalFace>();
            foreach (
                var s1CvhFace in
                    solid1.ConvexHullFaces.Where(
                        s1F =>
                            solid2.ConvexHullVertices.All(
                                s2V =>
                                    s1F.Normal.dotProduct(s2V.Position.subtract(s1F.Vertices[0].Position)) > -0.0001) &&
                            visitedCvhFaces.All(visF => Math.Abs(s1F.Normal.dotProduct(visF.Normal) - 1) < 0.0005)))
            {
                filteredGlobDirs1 =
                    filteredGlobDirs1.Where(gD => DisassemblyDirections.Directions[gD].dotProduct(s1CvhFace.Normal) > 0)
                        .ToList();
                visitedCvhFaces.Add(s1CvhFace);
            }
            visitedCvhFaces.Clear();
            foreach (
                var s2CvhFace in
                    solid2.ConvexHullFaces.Where(
                        s2F =>
                            solid1.ConvexHullVertices.All(
                                s1V => s2F.Normal.dotProduct(s1V.Position.subtract(s2F.Vertices[0].Position)) > -0.0001) &&
                            visitedCvhFaces.All(visF => Math.Abs(s2F.Normal.dotProduct(visF.Normal) - 1) < 0.0005)))
            {
                filteredGlobDirs2 =
                    filteredGlobDirs2.Where(gD => DisassemblyDirections.Directions[gD].dotProduct(s2CvhFace.Normal) < 0)
                        .ToList();
                visitedCvhFaces.Add(s2CvhFace);
            }
            if (filteredGlobDirs1.Count <= filteredGlobDirs2.Count) return filteredGlobDirs1;
            return filteredGlobDirs2;

        }

        private static List<Ray> RayGenerator(TessellatedSolid solidMoving, double[] direction)
        {
            var rays = solidMoving.ConvexHullVertices.Select(
                vertex =>
                    new Ray(
                        new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1],
                            vertex.Position[2]),
                        new Vector(direction[0], direction[1], direction[2]))).ToList();
            // add more vertices to the ray
            rays.AddRange(
                NonadjacentBlockingDetermination.AddingMoreRays(
                    solidMoving.ConvexHullEdges.Where(e => e != null && e.Length > 2).ToArray(), direction));
            return rays;
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
