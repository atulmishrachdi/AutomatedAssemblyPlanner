using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Assembly_Planner.GraphSynth.BaseClasses;
using GraphSynth.Representation;

namespace Assembly_Planner
{
    internal class OptionGeneratorProBinary
    {
        internal static List<option> GenerateOptions(designGraph assemblyGraph, HashSet<Component> seperate,
            Dictionary<hyperarc, List<hyperarc>> blockingDic, Dictionary<option, HashSet<int>> gOptions, int cndDirInd)
        {
            var freeSCCs = blockingDic.Keys.Where(k => blockingDic[k].Count == 0).ToList();
            var combinations = CombinationsCreatorPro(assemblyGraph, freeSCCs);
            var options = new HashSet<option>();
            //AddingOptionsToGraph(assemblyGraph, combinations, seperate.nodes.Count);
            options.UnionWith(AddingOptionsToGraph(combinations, seperate, options, gOptions, cndDirInd));
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
                        if (opt.Contains(key) || blockingDic[key].Count == 0) continue;
                        if (!blockingDic[key].All(bl => opt.Contains(bl))) continue;
                        var newOp = new List<hyperarc>(opt) { key };
                        if (!cp1.Where(cp => cp.All(hy => newOp.Contains(hy))).Any(cp => newOp.All(cp.Contains)))
                            freeSCCs.Add(key);
                    }

                    if (freeSCCs.Count == 0) continue;
                    combinations = CombinationsCreatorPro2(assemblyGraph, freeSCCs, opt);
                    var combAndPar = AddingParents(opt, combinations);
                    options.UnionWith(AddingOptionsToGraph(combAndPar, seperate, options, gOptions, cndDirInd));
                    cp2.UnionWith(combAndPar);
                    if (cp2.Count > 100) break;
                }
                counter = 1;
                cp1 = new HashSet<HashSet<hyperarc>>(cp2);
            } while (cp1.Count > 0);

            foreach (var SCCHy in assemblyGraph.hyperarcs.Where(hyScc =>
                hyScc.localLabels.Contains(DisConstants.SCC) &&
                !hyScc.localLabels.Contains(DisConstants.Removable)).ToList())
                assemblyGraph.hyperarcs.Remove(SCCHy);

            return options.ToList();
        }

        private static HashSet<option> AddingOptionsToGraph(HashSet<HashSet<hyperarc>> combAndPar, HashSet<Component> seperate,
            HashSet<option> options, Dictionary<option, HashSet<int>> gOptions, int cndDirInd)
        {
            var optionList = new HashSet<option>();
            foreach (var opt in combAndPar)
            {
                var nodes = new List<node>();
                var rule = new grammarRule();
                rule.L = new designGraph();
                var newOption = new option(rule);
                foreach (var hy in opt)
                    nodes.AddRange(hy.nodes);
                var otherHalf = seperate.Where(n => !nodes.Contains(n)).ToList();

                if (nodes.Count == seperate.Count) continue;
                if (optionList.Any(o => o.nodes.All(nodes.Contains) && nodes.All(o.nodes.Contains))) continue;
                if (optionList.Any(o => o.nodes.All(otherHalf.Contains) && otherHalf.All(o.nodes.Contains))) continue;
                if (options.Any(o => o.nodes.All(nodes.Contains) && nodes.All(o.nodes.Contains))) continue;
                if (options.Any(o => o.nodes.All(otherHalf.Contains) && otherHalf.All(o.nodes.Contains))) continue;
                var exist =
                    gOptions.Keys.Where(o => o.nodes.All(nodes.Contains) && nodes.All(o.nodes.Contains)).ToList();
                if (exist.Any())
                {
                    gOptions[exist[0]].Add(cndDirInd);
                    continue;
                }
                if (gOptions.Keys.Any(o => o.nodes.All(otherHalf.Contains) && otherHalf.All(o.nodes.Contains)))
                    continue;

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
                if (comb.Count > 100) break;
                lastGroup.Clear();
                lastGroup.AddRange(newGroup.Select(nG => nG.ToList()));
                i++;
            }
            return comb;
        }

        internal static HashSet<HashSet<hyperarc>> CombinationsCreatorPro(designGraph assemblyGraph, List<hyperarc> freeSCCs)
        {
            // The combinations must meet these two conditions:
            //   1. The SCCs in the combinations must be physically connected to each other and connected to their parent.
            //   2. or if they are not connected to each other, each of them individually needs to be connected to the parent.
            var combinationsHash = new HashSet<HashSet<hyperarc>>();
            var combinations = new List<List<hyperarc>>();
            combinations.AddRange(freeSCCs.Select(ini => new List<hyperarc> { ini }));

            var doubleConnected = new List<List<hyperarc>>();
            for (var i = 0; i < freeSCCs.Count - 1; i++)
            {
                for (var j = i + 1; j < freeSCCs.Count; j++)
                {
                    if (
                        !assemblyGraph.arcs.Where(a => a is Connection).Cast<Connection>().Any(
                            a =>
                                ((freeSCCs[i].nodes.Contains(a.From) && freeSCCs[j].nodes.Contains(a.To)) ||
                                (freeSCCs[j].nodes.Contains(a.From) && freeSCCs[i].nodes.Contains(a.To))) /*&&
                                (a.Fasteners.Count > 0)*/))
                        continue;
                    doubleConnected.Add(new List<hyperarc> { freeSCCs[i], freeSCCs[j] });
                }
            }
            combinations.AddRange(doubleConnected);
            var merged = MergeConnectedListOfHyperarcs(doubleConnected);
            while (merged.Any())
            {
                combinations.AddRange(new List<List<hyperarc>>(merged));
                merged = MergeConnectedListOfHyperarcs(merged);
            }
            foreach (var com in combinations)
                combinationsHash.Add(new HashSet<hyperarc>(com));

            return combinationsHash;
        }

        internal static HashSet<HashSet<hyperarc>> CombinationsCreatorPro2(designGraph assemblyGraph, List<hyperarc> freeSCCs, HashSet<hyperarc> parents)
        {
            // ACCEPTABLE COMBINATIONS:
            // Screwed to each other
            var finalCombination = new HashSet<HashSet<hyperarc>>();
            var dic = new Dictionary<hyperarc, List<hyperarc>>();
            foreach (var scc in freeSCCs)
                dic.Add(scc, PhisicallyConnected(assemblyGraph, freeSCCs, scc));

            var generated = new Queue<HashSet<hyperarc>>();
            var screwed = new List<hyperarc>();
            foreach (var parent in parents)
                screwed.AddRange(PhisicallyConnected(assemblyGraph, freeSCCs, parent));
            var combin = CombinationsCreator(screwed);
            foreach (var comb in combin)
                generated.Enqueue(comb);

            while (generated.Any())
            {
                var opt = generated.Dequeue();
                if (!(finalCombination.Any(hys => hys.All(opt.Contains) && opt.All(hys.Contains))))
                    finalCombination.Add(opt);
                if (finalCombination.Count > 100) break;
                var screwedToOption = ScrewedToOption(dic, opt);
                var combination = CombinationsCreator(screwedToOption);
                foreach (var com in combination)
                {
                    var merged = new HashSet<hyperarc>(com);
                    merged.UnionWith(opt);
                    if (generated.Any(hys => hys.All(merged.Contains) && merged.All(hys.Contains)))
                        continue;
                    generated.Enqueue(merged);
                }
            }

            return finalCombination;
        }

        private static List<hyperarc> ScrewedToOption(Dictionary<hyperarc, List<hyperarc>> dic, HashSet<hyperarc> opt)
        {
            // screwed to Opt and not included in Opt
            var screwed = new List<hyperarc>();
            foreach (var hy in opt)
            {
                foreach (var scrs in dic[hy])
                {
                    if (opt.Contains(scrs) || screwed.Contains(scrs)) continue;
                    screwed.Add(scrs);
                }
            }
            return screwed;
        }

        private static List<hyperarc> ScrewedToScc(designGraph assemblyGraph, List<hyperarc> freeSccs, hyperarc hy)
        {
            var screwed = new List<hyperarc>();
            foreach (var scc in freeSccs)
            {
                if (
                    assemblyGraph.arcs.Where(a => a is Connection).Cast<Connection>().Any(
                        a =>
                            ((hy.nodes.Contains(a.From) && scc.nodes.Contains(a.To)) ||
                            (scc.nodes.Contains(a.From) && hy.nodes.Contains(a.To))) &&
                            (a.Fasteners.Count > 0)))
                    screwed.Add(scc);
            }
            return screwed;
        }

        private static List<hyperarc> PhisicallyConnected(designGraph assemblyGraph, List<hyperarc> freeSccs, hyperarc hy)
        {
            // only if there is any connection between them
            var connected = new List<hyperarc>();
            foreach (var scc in freeSccs)
            {
                if (
                    assemblyGraph.arcs.Where(a => a is Connection).Cast<Connection>().Any(
                        a =>
                            ((hy.nodes.Contains(a.From) && scc.nodes.Contains(a.To)) ||
                            (scc.nodes.Contains(a.From) && hy.nodes.Contains(a.To)))))
                    connected.Add(scc);
            }
            return connected;
        }

        private static List<List<hyperarc>> MergeConnectedListOfHyperarcs(List<List<hyperarc>> groupsOfHyperarcs)
        {
            var merged = new List<List<hyperarc>>();
            for (var i = 0; i < groupsOfHyperarcs.Count - 1; i++)
            {
                for (var j = i + 1; j < groupsOfHyperarcs.Count; j++)
                {
                    if (!groupsOfHyperarcs[i].Any(hy => groupsOfHyperarcs[j].Contains(hy)))
                        continue;
                    // Merge them:
                    var n = new List<hyperarc>(groupsOfHyperarcs[i]);
                    n.AddRange(groupsOfHyperarcs[j].Where(hy => !n.Contains(hy)));
                    if (merged.Any(g => g.All(n.Contains) && n.All(g.Contains))) continue;
                    merged.Add(n);
                }
            }
            return merged;
        }

    }
}
