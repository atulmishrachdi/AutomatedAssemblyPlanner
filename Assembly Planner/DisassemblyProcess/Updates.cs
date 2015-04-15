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
        internal static void ApplyChild(AssemblyCandidate child, option opt)
        {
            // The function removes hyperarcs with "SCC" or "Seperate" lables
            for (var i = 0; i < child.graph.hyperarcs.Count; i++)
            {
                var hy = child.graph.hyperarcs[i];
                if (hy.localLabels.Contains(DisConstants.SeperateHyperarcs) && hy.nodes.Any(n=> opt.hyperarcs[0].nodes.Contains(n)))
                {
                    child.graph.removeHyperArc(hy);
                    i--;
                    continue;
                }
                if ((hy.localLabels.Contains(DisConstants.SeperateHyperarcs) && !hy.nodes.Any(n => opt.hyperarcs[0].nodes.Contains(n))) || hy.localLabels.Contains(DisConstants.SingleNode))
                    continue;

                if (!hy.localLabels.Contains(DisConstants.Removable))// Maybe all of them contains "Removable"
                {
                    child.graph.removeHyperArc(hy);
                    i--;
                }
                else
                {
                    if (hy.localLabels.Contains(DisConstants.SCC))
                        hy.localLabels.Remove(DisConstants.SCC);
                }
            }
        }

        internal static void UpdateAssemblyGraph(designGraph assemblyGraph)
        {
            // The function removes hyperarcs with "Removable" lables
            for (var i = 0; i < assemblyGraph.hyperarcs.Count; i++)
            {
                var h = assemblyGraph.hyperarcs[i];
                if (!h.localLabels.Contains(DisConstants.Removable))
                    continue;
                    assemblyGraph.removeHyperArc(h);
                    i--;
            }
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

        internal static List<hyperarc> UpdatePreceedings(List<hyperarc> Preceedings)
        {
            var list = new List<hyperarc>();
            foreach (var preceeding in Preceedings.Where(p=>!list.Contains(p)))
                list.Add(preceeding);
            return list;
        }

        internal static hyperarc AddSecondHyperToOption(AssemblyCandidate child, option opt)
        {
            foreach (var sepHy in child.graph.hyperarcs.Where(a => a.localLabels.Contains(DisConstants.SeperateHyperarcs) && opt.hyperarcs[0].nodes.All(n => a.nodes.Contains(n)))) //
            {
                var otherNodes = sepHy.nodes.Where(n => !opt.hyperarcs[0].nodes.Contains(n)).ToList();
                return new hyperarc("", otherNodes);
            }
            return null;
        }
    }
}
