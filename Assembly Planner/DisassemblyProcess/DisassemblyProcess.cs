using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth;
using GraphSynth.Representation;

namespace Assembly_Planner
{
    public class DisassemblyProcess : AbstractAssemblySearch
    {
        internal static void Run(designGraph assemblyGraph, List<int> globalDirPool)
        {
            // This is a surrogate graph-based approach for "disassembly"
            // take a direction from the pool
            //   find the SCCs
            //   create the DBG
            //   generate the options
            
            assemblyGraph.addHyperArc(assemblyGraph.nodes);
            var iniHy = assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1];
            iniHy.localLabels.Add(DisConstants.SeperateHyperarcs);

            var candidates = new SortedList<List<double>, AssemblyCandidate>(new MO_optimizeSort());
            var beam = new Queue<AssemblyCandidate>(DisConstants.BeamWidth);
            var found = false;
            AssemblyCandidate goal = null;
            beam.Enqueue(new AssemblyCandidate(seedCandidate));
            var recogRule = new ruleSet();

            while (beam.Count != 0 && !found)
            {
                candidates.Clear();
                foreach (var current in beam)
                {
                    foreach (var i in globalDirPool)
                    {
                        var cndDir = DisassemblyDirections.Directions[i];
                        foreach (var hy in assemblyGraph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SeperateHyperarcs)))
                        {
                            SCC.StronglyConnectedComponents(assemblyGraph, hy, cndDir);
                            var blockingDic = DBG.DirectionalBlockingGraph(assemblyGraph, hy, cndDir);
                            OptionGenerator.GenerateOptions(assemblyGraph, hy, cndDir, blockingDic);
                        }
                    }
                    var ruleChoices = recogRule.recognize(assemblyGraph);
                    foreach (var opt in ruleChoices)
                    {
                        var child = (AssemblyCandidate)current.copy();
                        transferLmappingToChild(child.graph, current.graph, opt);
                        child = ApplyChild(child);
                        if (assemblyEvaluator.Evaluate(child, opt) > 0)
                            lock (candidates)
                                candidates.Add(child.performanceParams, child); 
                        child.addToRecipe(opt);
                    }
                }
                beam.Clear();
                var count = 0;
                foreach (var c in candidates.Values)
                {
                    if (isCurrentTheGoal(c))
                    {
                        goal = c;
                        found = true;
                        break;
                    }
                    beam.Enqueue(c);
                    if (++count > DisConstants.BeamWidth) 
                        break;
                }
            }

            // Now all the options are generated and it is the time to do the search
            // After apply, we can erase everything and start making the SCCs and DBGs from the beginning, 
            //    But majarity of the later options had beed found before, so I need to find a way to store them
            //    and use them again.

            // After apply, add the "seperate" lable to the chosen Hyperarc
        }

        private static AssemblyCandidate ApplyChild(AssemblyCandidate child)
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
    }
}
