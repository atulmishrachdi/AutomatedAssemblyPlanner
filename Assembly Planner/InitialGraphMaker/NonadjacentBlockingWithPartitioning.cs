using System;
using System.Collections.Generic;
using System.Linq;
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
        internal static Dictionary<TessellatedSolid, Partition[]> Partitions = new Dictionary<TessellatedSolid, Partition[]>();
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
            foreach (var s in solids)
                Partitions.Add(s, PartitioningSolid.Run2(s));
            var solidsL = solids.Where(s => graph.nodes.Any(n => n.name == s.Name)).ToList();
            for (var i = 0; i < solidsL.Count; i++)
            {
                var solidMoving = solidsL[i];
                for (var j = i + 1; j < solidsL.Count; j++)
                {
                    var blocked = false;
                    // check the convex hull of these two solids to find the planes tha can linearly seperate them
                    // solid1 is moving and solid2 is blocking
                    var solidBlocking = solidsL[j];
                    var filteredDirections = FilterGlobalDirections(solidMoving,solidBlocking,gDir);
                    // remember this: if solid2 is not blocking solid1, we need to check if solid1 is blocking 2 in the opposite direction.
                    // if filteredDirections.Count == gDir.Count then the CVHs overlap
                    if (filteredDirections.Count == gDir.Count)
                    {
                        continue;
                    }
                    else
                    {
                        foreach (var filtDir in filteredDirections)
                        {
                            var direction = DisassemblyDirections.Directions[filtDir];
                            blocked = BlockingDeterminationNoCvhOverlapping(direction, solidMoving, solidBlocking);
                            if (!blocked)
                            {
                                // now check solid 1 is blocking solid 2 in opposite direction or not.
                                blocked = BlockingDeterminationNoCvhOverlapping(direction.multiply(-1.0), solidBlocking, solidMoving);
                            }
                            if (blocked) break;
                        }
                    }
                }
            }
        }

        private static bool BlockingDeterminationNoCvhOverlapping(Double[] direction, TessellatedSolid solidMoving, TessellatedSolid solidBlocking)
        {
            var rays =
                solidMoving.ConvexHullVertices.Select(
                    vertex =>
                        new Ray(
                            new AssemblyEvaluation.Vertex(vertex.Position[0], vertex.Position[1],
                                vertex.Position[2]),
                            new Vector(direction[0], direction[1], direction[2]))).ToList();
            // add more vertices to the ray
            rays.AddRange(
                NonadjacentBlockingDetermination.AddingMoreRays(
                    solidMoving.ConvexHullEdges.Where(e => e != null && e.Length > 2).ToArray(), direction));
            foreach (var ray in rays)
            {
                if (!NonadjacentBlockingDetermination.BoundingBoxBlocking(direction, solidBlocking, solidMoving)) continue;
                var memoFace = new List<PolygonalFace>();
                if (solidBlocking.ConvexHullFaces.Any(
                        f => NonadjacentBlockingDetermination.RayIntersectsWithFace3(ray, f)))
                {
                    var affectedPartitions = AffectedPartitionsWithRay(solidBlocking, ray);
                    foreach (var prtn in affectedPartitions)
                    {
                        foreach (var tri in prtn.SolidTriangles.Where(t => !memoFace.Contains(t)))
                        {
                            memoFace.Add(tri);
                            if (!NonadjacentBlockingDetermination.RayIntersectsWithFace3(ray, tri))
                                continue;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static List<Partition> AffectedPartitionsWithRay(TessellatedSolid solidBlocking, Ray ray)
        {
            return
                Partitions[solidBlocking].Where(
                    prtn => prtn.Faces.Any(f => NonadjacentBlockingDetermination.RayIntersectsWithFace3(ray, f)))
                    .ToList();
        }

        private static List<int> FilterGlobalDirections(TessellatedSolid solid1, TessellatedSolid solid2, List<int> gDir)
        {
            // If there is a plane that can seperate these two CVHs, then we only need to keep the directions
            // that have a positive dot prodoct with the normal of the plane.
            var filteredGlobDirs1 = new List<int>(gDir);
            var filteredGlobDirs2 = new List<int>(gDir);
            var visitedCvhFaces = new List<PolygonalFace>();
            foreach (
                var s1CvhFace in
                    solid1.ConvexHullFaces.Where(
                        s1F =>
                            solid2.ConvexHullVertices.All(
                                s2V =>
                                    s1F.Normal.dotProduct(new[]
                                    {
                                        s2V.Position[0] - s1F.Vertices[0].Position[0],
                                        s2V.Position[1] - s1F.Vertices[0].Position[1],
                                        s2V.Position[2] - s1F.Vertices[0].Position[2]
                                    }) > -0.0001) &&
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
                                s1V =>
                                    s2F.Normal.dotProduct(new[]
                                    {
                                        s1V.Position[0] - s2F.Vertices[0].Position[0],
                                        s1V.Position[1] - s2F.Vertices[0].Position[1],
                                        s1V.Position[2] - s2F.Vertices[0].Position[2]
                                    }) > -0.0001) &&
                            visitedCvhFaces.All(visF => Math.Abs(s2F.Normal.dotProduct(visF.Normal) - 1) < 0.0005)))
            {
                filteredGlobDirs2 =
                    filteredGlobDirs2.Where(gD => DisassemblyDirections.Directions[gD].dotProduct(s2CvhFace.Normal) > 0)
                        .ToList();
                visitedCvhFaces.Add(s2CvhFace);
            }
            if (filteredGlobDirs1.Count >= filteredGlobDirs2.Count) return filteredGlobDirs1;
            return
                filteredGlobDirs1.Where(
                    gD1 =>
                        filteredGlobDirs2.Any(
                            gD2 =>
                                Math.Abs(
                                    DisassemblyDirections.Directions[gD1].dotProduct(
                                        DisassemblyDirections.Directions[gD2]) - 1) < 0.00000001)).ToList();

        }
    }
}
