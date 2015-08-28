using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GraphSynth.Representation;
using Assembly_Planner.GraphSynth.BaseClasses;

namespace Assembly_Planner
{
    internal class OptionGeneratorProBinary
    {
        internal static List<option> GenerateOptions(designGraph assemblyGraph, List<Component> seperate,
            Dictionary<hyperarc, List<hyperarc>> blockingDic)
        {
            var freeSCCs = blockingDic.Keys.Where(k => blockingDic[k].Count == 0).ToList();
            var combinations = CombinationsCreator(freeSCCs);
            var options = new List<option>();
            //AddingOptionsToGraph(assemblyGraph, combinations, seperate.nodes.Count);
            options.AddRange(AddingOptionsToGraph(combinations, seperate, options));
            var counter = 0;
            var cp1 = new HashSet<HashSet<hyperarc>>();
            var cp2 = new HashSet<HashSet<hyperarc>>();

            do
            {
                if (counter == 0)
                    cp1 = combinations;
                cp2.Clear();
                foreach (var opt in cp1)
                {
                    freeSCCs.Clear();
                    foreach (var key in blockingDic.Keys)
                    {
                        if (opt.Contains(key)) continue;
                        if (!blockingDic[key].All(bl => opt.Contains(bl))) continue;
                        var newOp = new HashSet<hyperarc>(opt) { key };
                        if (!cp1.Where(cp => cp.All(hy => newOp.Contains(hy))).Any(cp => newOp.All(cp.Contains)))
                            freeSCCs.Add(key);
                    }

                    if (freeSCCs.Count == 0) continue;
                    combinations = CombinationsCreator(freeSCCs);
                    var combAndPar = AddingParents(opt, combinations);
                    //AddingOptionsToGraph(assemblyGraph, combAndPar, seperate.nodes.Count);
                    options.AddRange(AddingOptionsToGraph(combAndPar, seperate, options));
                    cp2.UnionWith(combAndPar);
                }
                counter = 1;
                cp1 = new HashSet<HashSet<hyperarc>>(cp2);
            } while (cp1.Count > 0);

            foreach (var SCCHy in assemblyGraph.hyperarcs.Where(hyScc =>
                hyScc.localLabels.Contains(DisConstants.SCC) &&
                !hyScc.localLabels.Contains(DisConstants.Removable)).ToList())
                assemblyGraph.hyperarcs.Remove(SCCHy);

            return options;
        }

        private static HashSet<option> AddingOptionsToGraph(HashSet<HashSet<hyperarc>> combAndPar, List<Component> seperate,
            List<option> options)
        {
            var optionList = new HashSet<option>();
            var sep = seperate.Count;
            foreach (var opt in combAndPar)
            {
                var nodes = new List<node>();
                var rule = new grammarRule();
                rule.L = new designGraph();
                var newOption = new option(rule);
                foreach (var hy in opt)
                    nodes.AddRange(hy.nodes);
                var otherHalf = seperate.Where(n => !nodes.Contains(n)).ToList();

                if (nodes.Count == sep) continue;
                if (optionList.Any(o => o.nodes.All(nodes.Contains) && nodes.All(o.nodes.Contains))) continue;
                if (optionList.Any(o => o.nodes.All(otherHalf.Contains) && otherHalf.All(o.nodes.Contains))) continue;
                if (options.Any(o => o.nodes.All(nodes.Contains) && nodes.All(o.nodes.Contains))) continue;
                if (options.Any(o => o.nodes.All(otherHalf.Contains) && otherHalf.All(o.nodes.Contains))) continue;

                newOption.nodes.AddRange(nodes);
                optionList.Add(newOption);
            }
            return optionList;
        }

        private static HashSet<HashSet<hyperarc>> AddingParents(HashSet<hyperarc> opt, HashSet<HashSet<hyperarc>> combinations)
        {
            var comb2 = new HashSet<HashSet<hyperarc>>();
            foreach (var c in combinations)
            {
                foreach (var h in opt)
                    c.Add(h);
                comb2.Add(c);
            }
            return comb2;
        }

        private static HashSet<HashSet<hyperarc>> CombinationsCreator(List<hyperarc> freeSCCs)
        {
            var comb = new HashSet<HashSet<hyperarc>>();
            var lastGroup = new List<List<hyperarc>>();
            foreach (var hy in freeSCCs)
            {
                lastGroup.Add(new List<hyperarc> { hy });
                comb.Add(new HashSet<hyperarc> { hy });
            }

            var i = 0;
            while (i < freeSCCs.Count - 1)
            {
                var newGroup = new HashSet<HashSet<hyperarc>>();
                for (var j = 0; j < freeSCCs.Count - 1; j++)
                {
                    var hy1 = freeSCCs[j];
                    for (var k = j + 1; k < lastGroup.Count; k++)
                    {
                        var hy2 = lastGroup[k];
                        var com = new HashSet<hyperarc> { hy1 };
                        com.UnionWith(hy2);
                        if ((newGroup.Any(hy => hy.All(com.Contains) && com.All(hy.Contains))) || hy2.Contains(hy1))
                            continue;
                        newGroup.Add(com);
                    }
                }
                comb.UnionWith(newGroup);
                lastGroup.Clear();
                lastGroup.AddRange(newGroup.Select(nG => nG.ToList()));
                i++;
            }
            return comb;
        }
    }
}
