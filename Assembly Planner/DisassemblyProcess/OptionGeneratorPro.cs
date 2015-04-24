using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GraphSynth.Representation;

namespace Assembly_Planner
{
    internal class OptionGeneratorPro
    {
        static List<hyperarc> Preceedings = new List<hyperarc>();
        private static int co;

        internal static void GenerateOptions(designGraph assemblyGraph, hyperarc seperate,
            Dictionary<hyperarc, List<hyperarc>> blockingDic)
        {
            blockingDic = UpdateBlockingDic(blockingDic);
            var freeSCCs = blockingDic.Keys.Where(k => blockingDic[k].Count == 0).ToList();
            var combinations = CombinationsCreator(freeSCCs);
            AddingOptionsToGraph(assemblyGraph, combinations);

            var counter = 0;
            var cp1 = new List<List<hyperarc>>();
            var cp2 = new List<List<hyperarc>>();

            do
            {
                if (counter == 0)
                    cp1 = combinations;
                cp2.Clear();
                foreach (var opt in cp1)
                {
                    freeSCCs =
                        blockingDic.Keys.Where(k => blockingDic[k].All(opt.Contains) && opt.All(blockingDic[k].Contains))
                            .ToList();
                    combinations = CombinationsCreator(freeSCCs);
                    var combAndPar = AddingParents(opt, combinations, seperate.nodes.Count);
                    AddingOptionsToGraph(assemblyGraph, combAndPar);
                    cp2.AddRange(combAndPar);
                }
                counter = 1;
                cp1 = new List<List<hyperarc>>(cp2);
            } while (cp1.Count > 0);
            
            foreach (var SCCHy in assemblyGraph.hyperarcs.Where(hyScc =>
                            hyScc.localLabels.Contains(DisConstants.SCC) &&
                            !hyScc.localLabels.Contains(DisConstants.Removable)).ToList())
                assemblyGraph.hyperarcs.Remove(SCCHy);
        }

        private static void AddingOptionsToGraph(designGraph assemblyGraph, List<List<hyperarc>> combAndPar)
        {
            foreach (var opt in combAndPar)
            {
                var nodes = new List<node>();
                foreach (var hy in opt)
                    nodes.AddRange(hy.nodes);
                assemblyGraph.addHyperArc(nodes);
                assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1].localLabels.Add(
                    DisConstants.Removable);
            }
        }

        private static List<List<hyperarc>> AddingParents(List<hyperarc> opt, List<List<hyperarc>> combinations, int seperateNodesCount)
        {
            var comb2 = new List<List<hyperarc>>();
            foreach (var c in combinations)
                foreach (var h in opt)
                    if (c.Sum(hy => hy.nodes.Count) + opt.Sum(hy => hy.nodes.Count) != seperateNodesCount)
                    {
                        c.Add(h);
                        comb2.Add(c);
                    }
            return comb2;
        }

        private static List<List<hyperarc>> CombinationsCreator(List<hyperarc> freeSCCs)
        {
            var comb = new List<List<hyperarc>>();
            var lastGroup = new List<List<hyperarc>>();
            foreach (var hy in freeSCCs)
            {
                lastGroup.Add(new List<hyperarc> { hy });
                comb.Add(new List<hyperarc> { hy });
            }
                
            var i = 0;
            while (i < freeSCCs.Count - 1)
            {
                var newGroup = new List<List<hyperarc>>();
                for (var j = 0; j< freeSCCs.Count-1; j++)
                {
                    var hy1 = freeSCCs[j];
                    for (var k = j+1; k < lastGroup.Count; k++)
                    {
                        var hy2 = lastGroup[k];
                        var com = new List<hyperarc>{hy1};
                        com.AddRange(hy2);
                        if ((newGroup.Any(hy => hy.All(com.Contains) && com.All(hy.Contains))) || hy2.Contains(hy1) )
                            continue;
                        newGroup.Add(com);
                    }
                }
                comb.AddRange(newGroup);
                lastGroup = newGroup;
                i++;
            }
            return comb;
        }

        private static Dictionary<hyperarc, List<hyperarc>> UpdateBlockingDic(Dictionary<hyperarc, List<hyperarc>> blockingDic)
        {
            var newBlocking = new Dictionary<hyperarc, List<hyperarc>>();
            foreach (var sccHy in blockingDic.Keys)
            {
                Preceedings.Clear();
                co = 0;
                PreceedingFinder(sccHy, blockingDic);
                Preceedings = Updates.UpdatePreceedings(Preceedings);
                var cpy = new List<hyperarc>(Preceedings);
                newBlocking.Add(sccHy, cpy);
        }
            return newBlocking;
        }

        private static void PreceedingFinder(hyperarc sccHy, Dictionary<hyperarc, List<hyperarc>> blockingDic)
        {
            co++;
            if (co != 1)
                Preceedings.Add(sccHy);
            foreach (var value in blockingDic[sccHy])
                PreceedingFinder(value, blockingDic);
        }
    }
}
