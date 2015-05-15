using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using StarMathLib;
using TVGL;
using TVGL.Tessellation;

namespace Assembly_Planner
{
    class BoltAndGearDetection
    {
        internal static List<TessellatedSolid> ScrewAndBoltDetector(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive)
        {
            // Here are my thoughts about a bolt:
            // Since all of the threads are classified as cone, 
            //    if the number of cones are more than 30 percent of the total number of primitives
            //    AND, the summation of area of cone primitivies are more than 30 percent of the solid surface area
            var bolts = new List<TessellatedSolid>();
            foreach (var solid in solidPrimitive.Keys)
            {
                var cones = solidPrimitive[solid].Where(p => p is Cone).ToList();
                if (cones.Count < ConstantsPrimitiveOverlap.ConePortion * solidPrimitive[solid].Count ||
                    cones.Sum(p => p.Area) < ConstantsPrimitiveOverlap.ConeAreaPortion * solid.SurfaceArea)
                    continue;
                Console.WriteLine("Is " + solid.Name + " a Bolt or Screw? 'y' or 'n'");
                var read = Convert.ToString(Console.ReadLine());
                if (read == "y")
                    bolts.Add(solid);
            }
            return bolts;
        }

        internal static List<TessellatedSolid> GearDetector(Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive)
        {
            var gears = new List<TessellatedSolid>();
            foreach (var solid in solidPrimitive.Keys)
            {
                var flats = solidPrimitive[solid].Where(p => p is Flat&& p.Faces.Count>100).ToList();
                foreach (var flatPrim in flats)
                {
                    var patches = SortedConnectedPatches(flatPrim.OuterEdges);
                    foreach (var patch in patches)
                    {
                        // The clustering must be added here. Do the clustering only if the 
                        // length of the longest to the length of the shortest is more than s.th.
                        var cluster = new List<Edge>[2];
                        var newPatch = new List<Edge>();
                        if (ContainsDense(patch))
                            cluster = ClusteringDenseSparse(patch);
                        if (cluster[0] != null && cluster[1].Count > 150)
                            newPatch = MergingDenseEdges(patch, cluster);
                        var crossP = new List<double[]>();
                        for (var i = 0; i < patch.Count - 1; i++)
                        {
                            for (var j = i + 1; j < patch.Count; j++)
                            {
                                var cross = new[] {0.0, 0, 0};
                                var vec1 = patch[i].Vector.normalize();
                                var vec2 = patch[j].Vector.normalize();
                                if (patch[i].To == patch[j].From)
                                {
                                    if (SmoothAngle(vec1, vec2))
                                        break;
                                    cross = vec1.crossProduct(vec2);
                                }
                                else //patch[i].To == patch[j].To:
                                {
                                    if (SmoothAngle(vec1, vec2.multiply(-1)))
                                        break;
                                    cross = vec1.crossProduct(vec2.multiply(-1));
                                }
                                crossP.Add(cross);
                                break;
                            }
                        }
                        if (crossP.Count == 0) continue;
                        var crossSign = ConvertCrossToSign(crossP);
                    }
                }
            }
            return gears;
        }

        private static List<Edge> MergingDenseEdges(List<Edge> patch, List<Edge>[] cluster)
        {
            var copyPatch = new List<Edge>(patch);
            if (cluster[0].Contains(patch[0])) //if the starting face is sparse
            {
                var localDense = new List<Edge>();
                for (var i = 1; i < patch.Count; i++)
                {
                    if (cluster[0].Contains(patch[i]))
                    {
                        if (localDense.Count > 5)
                        {
                            // here I must replace the localDense with a new edge
                            localDense[0].To = localDense[localDense.Count - 1].To;

                        }
                        localDense.Clear();
                        continue;
                    }
                    localDense.Add(patch[i]);
                }
            }

            else //if the starting face is dense
            {
                // take the dense edges from the begining and the last group of dense edges at the end (if exists)
                var localDense = new List<Edge>();

            }
        }

        private static bool ContainsDense(List<Edge> patch)
        {
            return (patch.Max(e => e.Length)/patch.Min(e => e.Length)) > 55;
        }

        private static List<Edge>[] ClusteringDenseSparse(List<Edge> patch)
        {
            var cluster = new List<Edge>[2];
            // 0 is sparse
            // 1 is dense
            var ini1 = new List<Edge>();
            cluster[0] = ini1;
            var ini2 = new List<Edge>();
            cluster[1] = ini2;
            var averageLengthMax = patch.Max(e => e.Length);
            var averageLengthMin = patch.Min(e => e.Length);
            foreach (var edge in patch)
            {
                if (Math.Abs(edge.Length - averageLengthMax) <= Math.Abs(edge.Length - averageLengthMin))
                    cluster[0].Add(edge);
                else
                    cluster[1].Add(edge);
            }
            return cluster;
        }

        private static List<List<Edge>> SortedConnectedPatches(List<Edge> outerEdges)
        {
            var outer = new List<Edge>(outerEdges);
            var patches = new List<List<Edge>>();
            var c = -1;

            while (outer.Count>0)
            {
                c++;
                patches.Add(new List<Edge>());
                var cop = outerEdges.Where(e => e == outer[0]).ToList()[0];
                patches[c].Add(cop);
                outer.Remove(outer[0]);
                var count1 = patches[c].Count;
                var count2 = 0;
                while (count1!=count2)
                {
                    count1 = patches[c].Count;
                    var ed1 = patches[c][patches[c].Count - 1];
                    for (var j = 0; j < outer.Count - 1; j++)
                    {
                        var ed2 = outer[j];
                        //if (patches[c].Contains(ed2)) continue;
                        if (ed1.To != ed2.From &&
                            ed1.To != ed2.To)
                            continue;
                        var copy = outerEdges.Where(e => e == ed2).ToList()[0];
                        patches[c].Add(copy);
                        count2 = patches[c].Count;
                        outer.Remove(ed2);
                        break;
                    }
                    if (count2 == 0) break;
                }
            }
            return patches;
        }

        private static bool SmoothAngle(double[] vec1, double[] vec2)
        {
            return Math.Abs(vec1.dotProduct(vec2)) > 0.9;
        }

        private static List<int> ConvertCrossToSign(List<double[]> crossP)
        {
            var signs = new List<int>();
            signs.Add(1);
            var mainCross = crossP[0];
            for (var i = 1; i < crossP.Count; i++)
            {
                var cross2 = crossP[i];
                if ((Math.Sign(mainCross[0]) != Math.Sign(cross2[0]) ||
                     (Math.Sign(mainCross[0]) == 0 && Math.Sign(cross2[0]) == 0)) &&
                    (Math.Sign(mainCross[1]) != Math.Sign(cross2[1]) ||
                     (Math.Sign(mainCross[1]) == 0 && Math.Sign(cross2[1]) == 0)) &&
                    (Math.Sign(mainCross[2]) != Math.Sign(cross2[2]) ||
                     (Math.Sign(mainCross[2]) == 0 && Math.Sign(cross2[2]) == 0)))
                    signs.Add(-1);
                else
                    signs.Add(1);
            }
                return signs;
        }
    }
}
