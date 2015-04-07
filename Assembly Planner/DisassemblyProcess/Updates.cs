using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth.Representation;

namespace Assembly_Planner
{
    class Updates
    {
        internal static AssemblyCandidate ApplyChild(AssemblyCandidate child)
        {
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
            foreach (var hy in assemblyGraph.hyperarcs.Where(h=>h.localLabels.Contains(DisConstants.Removable)))
                assemblyGraph.removeHyperArc(hy);
        }
    }
}
