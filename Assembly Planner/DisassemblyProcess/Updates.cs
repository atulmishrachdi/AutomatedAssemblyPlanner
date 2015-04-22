using System;
using System.Collections.Generic;
using System.IO;
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

        internal static void AddPartsProperties(designGraph assemblyGraph)
        {
            var reader = new StreamReader(File.OpenRead(@"..\..\..\Test\Pump Assembly\Properties.csv"));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                var list = new List<double>();
                var i = 1;
                var node = assemblyGraph.nodes.Where(n => n.name == Convert.ToString(values[0])).ToList()[0];
                while (i < 9)
                {
                    node.localVariables.Add(Convert.ToDouble(values[i]));
                    i++;
                }
            }
        }

        internal static void UpdateChildGraph(AssemblyCandidate c, List<node>[] install)
        {
            // This is happening in Evaluation
            for (var j = 0; j < c.graph.hyperarcs.Count; j++)
            {
                var hy = c.graph.hyperarcs[j];
                if (hy.localLabels.Contains(DisConstants.SeperateHyperarcs) || hy.localLabels.Contains(DisConstants.SingleNode)) continue;
                c.graph.removeHyperArc(c.graph.hyperarcs[j]);
                j--;
            }

            foreach (var list in install)
            {
                if (list.Count == 1)
                {
                    c.graph.addHyperArc(list);
                    c.graph.hyperarcs[c.graph.hyperarcs.Count - 1].localLabels.Add(DisConstants.SingleNode);
                }
                else
                {
                    c.graph.addHyperArc(list);
                    c.graph.hyperarcs[c.graph.hyperarcs.Count - 1].localLabels.Add(DisConstants.SeperateHyperarcs);
                }
            }
        }
    }
}
