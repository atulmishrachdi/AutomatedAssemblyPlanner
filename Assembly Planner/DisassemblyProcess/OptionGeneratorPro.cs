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
    internal class OptionGeneratorPro
    {
        internal static List<option> GenerateOptions(designGraph assemblyGraph, hyperarc seperate,
            Dictionary<hyperarc, List<hyperarc>> blockingDic, List<option> gOptions)
        {
            var freeSCCs = blockingDic.Keys.Where(k => blockingDic[k].Count == 0).ToList();
            var combinations = CombinationsCreatorPro(assemblyGraph, freeSCCs);
            var options = new List<option>();
            //AddingOptionsToGraph(assemblyGraph, combinations, seperate.nodes.Count);
            options.AddRange(AddingOptionsToGraph(combinations, seperate, options, gOptions));
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
                    freeSCCs.Clear();
                    foreach (var key in blockingDic.Keys)
                    {
                        if (opt.Contains(key) || blockingDic[key].Count==0) continue;
                        if (!blockingDic[key].All(bl => opt.Contains(bl))) continue;
                        var newOp = new List<hyperarc>(opt) { key };
                        if (!cp1.Where(cp => cp.All(hy => newOp.Contains(hy))).Any(cp => newOp.All(cp.Contains)))
                            freeSCCs.Add(key);
                    }

                    if (freeSCCs.Count == 0) continue;
                    combinations = CombinationsCreatorPro2(assemblyGraph, freeSCCs, opt);
                    var combAndPar = AddingParents(opt, combinations);
                    options.AddRange(AddingOptionsToGraph(combAndPar, seperate, options, gOptions));
                    cp2.AddRange(combAndPar);
                }
                counter = 1;
                cp1 = new List<List<hyperarc>>(cp2);
            } while (cp1.Count > 0);

            foreach (var SCCHy in assemblyGraph.hyperarcs.Where(hyScc =>
                hyScc.localLabels.Contains(DisConstants.SCC) &&
                !hyScc.localLabels.Contains(DisConstants.Removable)).ToList())
                assemblyGraph.hyperarcs.Remove(SCCHy);

            return options;
        }

        private static List<option> AddingOptionsToGraph(List<List<hyperarc>> combAndPar, hyperarc seperate,
            List<option> options, List<option> gOptions)
        {
            var optionList = new List<option>();
            var sep = seperate.nodes.Count;
            foreach (var opt in combAndPar)
            {
                var nodes = new List<node>();
                var rule = new grammarRule();
                rule.L = new designGraph();
                var newOption = new option(rule);
                foreach (var hy in opt)
                    nodes.AddRange(hy.nodes);
                var otherHalf = seperate.nodes.Where(n => !nodes.Contains(n)).ToList();

                if (nodes.Count == sep) continue;
                if (optionList.Any(o => o.nodes.All(nodes.Contains) && nodes.All(o.nodes.Contains))) continue;
                if (optionList.Any(o => o.nodes.All(otherHalf.Contains) && otherHalf.All(o.nodes.Contains))) continue;
                if (options.Any(o => o.nodes.All(nodes.Contains) && nodes.All(o.nodes.Contains))) continue;
                if (options.Any(o => o.nodes.All(otherHalf.Contains) && otherHalf.All(o.nodes.Contains))) continue;
                if (gOptions.Any(o => o.nodes.All(nodes.Contains) && nodes.All(o.nodes.Contains))) continue;
                if (gOptions.Any(o => o.nodes.All(otherHalf.Contains) && otherHalf.All(o.nodes.Contains))) continue;
                
                newOption.nodes.AddRange(nodes);
                optionList.Add(newOption);
            }
            return optionList;
        }

        private static List<List<hyperarc>> AddingParents(List<hyperarc> opt, List<List<hyperarc>> combinations)
        {
            var comb2 = new List<List<hyperarc>>();
            foreach (var c in combinations)
            {
                foreach (var h in opt)
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
                for (var j = 0; j < freeSCCs.Count - 1; j++)
                {
                    var hy1 = freeSCCs[j];
                    for (var k = j + 1; k < lastGroup.Count; k++)
                    {
                        var hy2 = lastGroup[k];
                        var com = new List<hyperarc> { hy1 };
                        com.AddRange(hy2);
                        if ((newGroup.Any(hy => hy.All(com.Contains) && com.All(hy.Contains))) || hy2.Contains(hy1))
                            continue;
                        newGroup.Add(com);
                    }
                }
                comb.AddRange(newGroup);
                lastGroup.Clear();
                lastGroup.AddRange(newGroup.Select(nG => nG.ToList()));
                i++;
            }
            return comb;
        }

        internal static List<List<hyperarc>> CombinationsCreatorPro(designGraph assemblyGraph, List<hyperarc> freeSCCs)
        {
            // The combinations must meet these two conditions:
            //   1. The SCCs in the combinations must be physically connected to each other and connected to their parent.
            //   2. or if they are not connected to each other, each of them individually needs to be connected to the parent.
            var combinationsHash = new List<List<hyperarc>>();
            var combinations = new List<List<hyperarc>>();
            combinations.AddRange(freeSCCs.Select(ini => new List<hyperarc> {ini}));
            
            var doubleConnected = new List<List<hyperarc>>();
            for (var i = 0; i < freeSCCs.Count - 1; i++)
            {
                for (var j = i + 1; j < freeSCCs.Count; j++)
                {
                    if (
                        !assemblyGraph.arcs.Where(a=>a is Connection).Cast<Connection>().Any(
                            a =>
                                ((freeSCCs[i].nodes.Contains(a.From) && freeSCCs[j].nodes.Contains(a.To)) ||
                                (freeSCCs[j].nodes.Contains(a.From) && freeSCCs[i].nodes.Contains(a.To))) && 
                                (a.Fasteners.Count>0))) 
                        continue;
                    doubleConnected.Add(new List<hyperarc>{freeSCCs[i], freeSCCs[j]});
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
                combinationsHash.Add(new List<hyperarc>(com));

            return combinationsHash;
        }

        internal static List<List<hyperarc>> CombinationsCreatorPro2(designGraph assemblyGraph, List<hyperarc> freeSCCs, List<hyperarc> parents)
        {
            // ACCEPTABLE COMBINATIONS:
            // Screwed to each other
            var finalCombination = new List<List<hyperarc>>();
            var dic = new Dictionary<hyperarc,List<hyperarc>>();
            foreach (var scc in freeSCCs)
                dic.Add(scc, ScrewedToScc(assemblyGraph, freeSCCs, scc));

            var generated = new Queue<List<hyperarc>>();
            var screwed = new List<hyperarc>();
            foreach (var parent in parents)
                screwed.AddRange(ScrewedToScc(assemblyGraph, freeSCCs, parent));
            var combin = CombinationsCreator(screwed);
            foreach (var comb in combin)
                generated.Enqueue(comb);

            while (generated.Any())
            {
                var opt = generated.Dequeue();
                if (!(finalCombination.Any(hys => hys.All(opt.Contains) && opt.All(hys.Contains))))
                    finalCombination.Add(opt);
                var screwedToOption = ScrewedToOption(dic, opt);
                var combination = CombinationsCreator(screwedToOption);
                foreach (var com in combination)
                {
                    var merged = new List<hyperarc>(com);
                    merged.AddRange(opt);
                    if (generated.Any(hys => hys.All(merged.Contains) && merged.All(hys.Contains)))
                        continue;
                    generated.Enqueue(merged);
                }
            }

            return finalCombination;
        }

        private static List<hyperarc> ScrewedToOption(Dictionary<hyperarc, List<hyperarc>> dic, List<hyperarc> opt)
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
                    assemblyGraph.arcs.Where(a=> a is Connection).Cast<Connection>().Any(
                        a =>
                            ((hy.nodes.Contains(a.From) && scc.nodes.Contains(a.To)) ||
                            (scc.nodes.Contains(a.From) && hy.nodes.Contains(a.To))) && 
                            (a.Fasteners.Count>0)))
                    screwed.Add(scc);
            }
            return screwed;
        }

        private static List<List<hyperarc>> MergeConnectedListOfHyperarcs(List<List<hyperarc>> groupsOfHyperarcs)
        {
            var merged = new List<List<hyperarc>>();
            for (var i = 0; i < groupsOfHyperarcs.Count - 1; i++)
            {
                for (var j = i+1; j < groupsOfHyperarcs.Count; j++)
                {
                    if (!groupsOfHyperarcs[i].Any(hy => groupsOfHyperarcs[j].Contains(hy)))
                        continue;
                    // Merge them:
                    var n = new List<hyperarc>(groupsOfHyperarcs[i]);
                    n.AddRange(groupsOfHyperarcs[j].Where(hy=>!n.Contains(hy)));
                    if (merged.Any(g => g.All(n.Contains) && n.All(g.Contains))) continue;
                    merged.Add(n);
                }
            }
            return merged;
        }

    }
}
