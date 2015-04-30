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

        internal static List<option> GenerateOptions(designGraph assemblyGraph, hyperarc seperate,
            Dictionary<hyperarc, List<hyperarc>> blockingDic)
        {
            blockingDic = UpdateBlockingDic(blockingDic);
            var freeSCCs = blockingDic.Keys.Where(k => blockingDic[k].Count == 0).ToList();
            var combinations = CombinationsCreator(freeSCCs);
            var options = new List<option>();
            //AddingOptionsToGraph(assemblyGraph, combinations, seperate.nodes.Count);
            options.AddRange(AddingOptionsToGraph(combinations, seperate.nodes.Count));
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
                    if (freeSCCs.Count == 0) continue;
                    combinations = CombinationsCreator(freeSCCs);
                    var combAndPar = AddingParents(opt, combinations);
                    //AddingOptionsToGraph(assemblyGraph, combAndPar, seperate.nodes.Count);
                    options.AddRange(AddingOptionsToGraph(combAndPar, seperate.nodes.Count));
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

        private static void AddingOptionsToGraph(designGraph assemblyGraph, List<List<hyperarc>> combAndPar, int sep)
        {
            foreach (var opt in combAndPar)
            {
                var nodes = new List<node>();
                foreach (var hy in opt)
                    nodes.AddRange(hy.nodes);

                if (nodes.Count == sep) continue;
                var newHyperarc = assemblyGraph.addHyperArc(nodes);
                newHyperarc.localLabels.Add(DisConstants.Removable);
            }
        }

        private static List<option> AddingOptionsToGraph(List<List<hyperarc>> combAndPar, int sep)
        {
            var optionList = new List<option>();
            foreach (var opt in combAndPar)
            {
                var nodes = new List<node>();
                var rule = new grammarRule();
                rule.L = new designGraph();
                var newOption = new option(rule);
                foreach (var hy in opt)
                    nodes.AddRange(hy.nodes);
                if (nodes.Count == sep) continue;  
                newOption.nodes.AddRange(nodes);
                // careful are you adding nodes more than once!      use Linq Distinct?
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
