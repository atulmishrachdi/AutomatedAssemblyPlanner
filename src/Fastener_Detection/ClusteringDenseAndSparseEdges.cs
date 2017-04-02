using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;

namespace Fastener_Detection
{
    class ClusteringDenseAndSparseEdges
    {
        internal static List<GearEdge>[] ClusteringDenseSparse(List<GearEdge> patch)
        {
            var cluster = new List<GearEdge>[2];
            // [0] is sparse
            // [1] is dense
            var ini1 = new List<GearEdge>();
            cluster[0] = ini1;
            var ini2 = new List<GearEdge>();
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

        internal static bool ContainsDense(List<GearEdge> patch)
        {
            return (patch.Max(e => e.Length) / patch.Min(e => e.Length)) > 55;
        }

        internal static List<GearEdge> ReplacingDenseEdges(List<GearEdge> patch, List<GearEdge>[] cluster)
        {
            var copyPatch = new List<GearEdge>(patch);
            var count = patch.Count;
            var firstSparseInd = 0;
            if (cluster[1].Contains(patch[0])) //if the starting face is sparse
            {
                var numRem = 0;
                for (var i = 0; ; i++)
                {
                    if (cluster[0].Contains(patch[i]))
                        break;
                    copyPatch.Remove(patch[i]);
                    numRem++;
                }
                for (var i = patch.Count - 1; ; i--)
                {
                    if (cluster[0].Contains(patch[i]))
                        break;
                    copyPatch.Remove(patch[i]);
                    numRem++;
                }
                firstSparseInd = patch.IndexOf(copyPatch[0]);
                count = patch.Count - numRem;
            }
            
            var localDense = new List<GearEdge>();
            for (var i = firstSparseInd; i < count; i++)
            {
                if (cluster[0].Contains(patch[i]))
                {
                    if (localDense.Count > 5)
                    {
                        // here I must replace the localDense with a new edge
                        var newVec = localDense[localDense.Count - 1].To.Position.subtract(localDense[0].From.Position);
                        var newGearEdge = new GearEdge
                        {
                            To = localDense[localDense.Count - 1].To,
                            From = localDense[0].From,
                            Vector = new[] {newVec[0], newVec[1], newVec[2]}
                        };
                        var ind = copyPatch.IndexOf(localDense[0]);
                        copyPatch[ind] = newGearEdge;
                        for (var d = 1; d < localDense.Count; d++)
                            copyPatch.Remove(localDense[d]);
                    }
                    localDense.Clear();
                    continue;
                }
                localDense.Add(patch[i]);
            }
            return copyPatch;
        }
    }
}
