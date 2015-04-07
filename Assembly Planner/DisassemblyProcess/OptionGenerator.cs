using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;

namespace Assembly_Planner
{
    internal class OptionGenerator
    {
        static List<hyperarc> Preceedings = new List<hyperarc>();
        internal static void GenerateOptions(designGraph assemblyGraph, hyperarc hy,
            Dictionary<hyperarc, List<hyperarc>> blockingDic)
        {
            // I need to use the blocking information to find the removable SCCs.
            var trash = new List<hyperarc>();
            foreach (var sccHy in blockingDic.Keys)
            {
                if (blockingDic[sccHy].Count == 0)
                {
                    sccHy.localLabels.Add(DisConstants.Removable);
                    trash.Add(sccHy);
                }
                else
                {
                    if (blockingDic[sccHy].All(trash.Contains))
                    {
                        PreceedingFinder(sccHy, blockingDic);
                        var nodes = new List<node>();
                        foreach (var hyperarc in Preceedings)
                            nodes.AddRange(hyperarc.nodes);
                        Preceedings.Clear();
                        assemblyGraph.addHyperArc(nodes);
                        assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1].localLabels.Add(DisConstants.Removable);
                    }
                }

            }
        }

        private static void PreceedingFinder(hyperarc sccHy, Dictionary<hyperarc, List<hyperarc>> blockingDic)
        {
            Preceedings.Add(sccHy);
            foreach (var value in blockingDic[sccHy])
            {
                PreceedingFinder(value, blockingDic);
            }
        }
    }
}
