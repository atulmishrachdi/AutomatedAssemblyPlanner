using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth.Representation;
using StarMathLib;

namespace Assembly_Planner
{
    class Updates
    {
        internal static AssemblyCandidate ApplyChild(AssemblyCandidate child)
        {
            // The function removes hyperarcs with "SCC" or "Seperate" lables
            for (var i = 0; i < child.graph.hyperarcs.Count; i++)
            {
                var hy = child.graph.hyperarcs[i];
                if (!hy.localLabels.Contains(DisConstants.SCC) &&
                    !hy.localLabels.Contains(DisConstants.SeperateHyperarcs)) continue;
                child.graph.removeHyperArc(hy);
                i--;
            }
            return child;
        }

        internal static void UpdateAssemblyGraph(designGraph assemblyGraph)
        {
            // The function removes hyperarcs with "Removable" lables
            foreach (var hy in assemblyGraph.hyperarcs.Where(h=>h.localLabels.Contains(DisConstants.Removable)))
                assemblyGraph.removeHyperArc(hy);
        }

        internal static void UpdateGlobalDirections(List<int> globalDirPool)
        {
            // The function removes one of each parallel directions pair.
            for (var i = 0; i < globalDirPool.Count - 1; i++)
            {
                var dir1 = DisassemblyDirections.Directions[globalDirPool[i]];
                for (var j = i+1; j < globalDirPool.Count; j++)
                {
                    var dir2 = DisassemblyDirections.Directions[globalDirPool[j]];
                    if (Math.Abs(1 + dir1.dotProduct(dir2)) > DisConstants.Parallel) continue;
                    globalDirPool.RemoveAt(j);
                    j--;
                }
            }
        }
    }
}
