using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using StarMathLib;

namespace Assembly_Planner
{
    class AutoThreadedFastenerDetection
    {
        internal static void Run(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs)
        {
            // This is mostly similar to the auto fastener detection with no thread, but instead of learning
            // from the area of the cylinder and for example flat, we will learn from the number of faces.
            // why? because if we have thread, we will have too many triangles. And this can be useful.
            // I can also detect helix and use this to detect the threaded fasteners

            // Important: if the fasteners are threaded using solidworks Fastener toolbox, it will not
            //            have helix. The threads will be small cones with the same axis and equal area.

            var firstFilter =  FastenerDetector.SmallObjectsDetector(multipleRefs); //multipleRefs.Keys.ToList();
            var equalPrimitivesForEverySolid = FastenerDetector.EqualFlatPrimitiveAreaFinder(firstFilter, solidPrimitive);
            var groupedPotentialFasteners = FastenerDetector.GroupingSmallParts(firstFilter);
            List<int> learnerVotes;
            var learnerWeights = FastenerLearner.ReadingLearnerWeightsAndVotesFromCsv(out learnerVotes);
            foreach (var solid in firstFilter)
            {
                // if it is not any of those, we can still give it another chance:
                if (ThreadDetector(solid, solidPrimitive[solid]))
                {
                   // CommonHeadFatener();
                    continue;
                }
                // We may still have some threaded fasteners that could not be recognized by the 
                // "ThreadDetector" function.
                // Solution: Voted Perceptron classifier
                // run it here. How? 
                if (FastenerLearner.FastenerPerceptronLearner(solidPrimitive[solid], solid, learnerWeights, learnerVotes))
                    continue;
                // One more approach which actually turned out to be really interesting:
                //    Create the OBB around the object. take any of the side faces. generate 
                //    bunch of rays on the middle line of the side face. Shoot the ray and
                //    find the smallest distance to the closest triangle. Plot these calculated
                //    distances. Take a look at the trend and try to learn from it. 
                if (FastenerPolynomialTrend.PolynomialTrendDetector(solid)) continue;
            }
        }

        private static bool ThreadDetector(TessellatedSolid solid, List<PrimitiveSurface> primitiveSurfaces)
        {
            // Consider these two cases:
            //      1. Threads are helix
            //      2. Threads are seperate cones
            if (ThreadsAreSeperateCones(solid, primitiveSurfaces))
                return true;
            return SolidHasHelix(solid);

        }

        private static bool ThreadsAreSeperateCones(TessellatedSolid solid, List<PrimitiveSurface> primitiveSurfaces)
        {
            var cones = primitiveSurfaces.Where(p => p is Cone).Cast<Cone>().ToList();
            foreach (var cone in cones.Where(c => c.Faces.Count > 30))
            {
                var threads =
                    cones.Where(
                        c =>
                            (Math.Abs(c.Axis.dotProduct(cone.Axis) - 1) < 0.001 ||
                             Math.Abs(c.Axis.dotProduct(cone.Axis) + 1) < 0.001) &&
                            (Math.Abs(c.Faces.Count - cone.Faces.Count) < 3) &&
                            (Math.Abs(c.Area - cone.Area) < 0.001) &&
                            (Math.Abs(c.Aperture - cone.Aperture) < 0.001)).ToList();
                if (threads.Count < 10) continue;
                if (ConeThreadIsInternal(threads))
                    FastenerDetector.Nuts.Add(new Nut { Solid = solid });
                FastenerDetector.Fasteners.Add(new Fastener
                {
                    Solid = solid,
                    RemovalDirection =
                        FastenerDetector.RemovalDirectionFinderUsingObb(solid, PartitioningSolid.OrientedBoundingBoxDic[solid])
                });
                return true;
            }
            return false;
        }

        private static bool SolidHasHelix(TessellatedSolid solid)
        {
            // Idea: find an edge which has an internal angle equal to one of the following cases.
            // This only works if at least one of outer or inner threads have a sharo edge.
            // take the connected edges (from both sides) which have the same feature.
            // If it rotates couple of times, it is a helix.
            // It seems to be expensive. Let's see how it goes.
            // Standard thread angles:
            //       60     55     29     45     30    80 
            foreach (var edge in solid.Edges.Where(e => Math.Abs(e.InternalAngle - 2.08566845) < 0.04))
            // 2.0943951 is equal to 120 degree
            {
                // To every side of the edge if there is one edge with the IA of 120, this edge is unique and we dcannot find the second one. 
                var visited = new HashSet<Edge> { edge };
                var stack = new Stack<Edge>();
                var possibleHelixEdges = FindHelixEdgesConnectedToAnEdge(solid.Edges, edge, visited);
                // It can have 0, 1 or 2 edges
                if (possibleHelixEdges == null) continue;
                foreach (var e in possibleHelixEdges)
                    stack.Push(e);

                while (stack.Any() && visited.Count < 1000)
                {
                    var e = stack.Pop();
                    visited.Add(e);
                    var cand = FindHelixEdgesConnectedToAnEdge(solid.Edges, e, visited);
                    // if yes, it will only have one edge.
                    if (cand == null) continue;
                    stack.Push(cand[0]);
                }
                if (visited.Count < 1000) // Is it very big?
                    continue;
                // if the thread is internal, classify it as nut, else fastener
                if (HelixThreadIsInternal(visited))
                    FastenerDetector.Nuts.Add(new Nut { Solid = solid });
                FastenerDetector.Fasteners.Add(new Fastener
                {
                    Solid = solid,
                    RemovalDirection =
                        FastenerDetector.RemovalDirectionFinderUsingObb(solid, PartitioningSolid.OrientedBoundingBoxDic[solid])
                });
                return true;
            }
            return false;
        }

        private static bool ConeThreadIsInternal(List<Cone> threads)
        {
            // If it is seperated cones, it's easy: negative cones make internal thread
            // To make it robust, if 70 percent of the cones are negative, it is internal
            var neg = threads.Count(cone => !cone.IsPositive);
            if (neg >= 0.7 * threads.Count) return true;
            return false;
        }

        private static bool HelixThreadIsInternal(HashSet<Edge> helixEdges)
        {
            return false;
        }

        private static Edge[] FindHelixEdgesConnectedToAnEdge(Edge[] edges, Edge edge, HashSet<Edge> visited)
        {

            var m = new List<Edge>();
            var e1 =
                edges.Where(
                    e =>
                        (edge.From == e.From || edge.From == e.To) &&
                        Math.Abs(e.InternalAngle - 2.08566845) < 0.04 && !visited.Contains(e)).ToList();
            var e2 =
                edges.Where(
                    e =>
                        (edge.To == e.From || edge.To == e.To) &&
                        Math.Abs(e.InternalAngle - 2.08566845) < 0.04 && !visited.Contains(e)).ToList();
            if (!e1.Any() && !e2.Any()) return null;
            if (e1.Any()) m.Add(e1[0]);
            if (e2.Any()) m.Add(e2[0]);
            return m.ToArray();
        }
    }
}
