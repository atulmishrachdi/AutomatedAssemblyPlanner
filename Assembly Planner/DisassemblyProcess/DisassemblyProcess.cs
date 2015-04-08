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
        public static Dictionary<int, List<List<node>>> SccTracker = new Dictionary<int, List<List<node>>>();
        internal static void Run(designGraph assemblyGraph, List<int> globalDirPool)
        {
            // This is a surrogate graph-based approach for "disassembly"
            // take a direction from the pool
            //   find the SCCs
            //   create the DBG
            //   generate the options
            
            // check list:

            Updates.UpdateGlobalDirections(globalDirPool);
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
                    foreach (var cndDirInd in globalDirPool)
                    {
                        foreach (var seperateHy in assemblyGraph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SeperateHyperarcs)))
                        {
                            SCC.StronglyConnectedComponents(assemblyGraph, seperateHy, cndDirInd);
                            var blockingDic = DBG.DirectionalBlockingGraph(assemblyGraph, seperateHy, cndDirInd);
                            OptionGenerator.GenerateOptions(assemblyGraph, seperateHy, blockingDic);
                        }
                        var ruleChoices = recogRule.recognize(assemblyGraph);
                        foreach (var opt in ruleChoices)
                        {
                            var child = (AssemblyCandidate)current.copy();
                            transferLmappingToChild(child.graph, current.graph, opt);
                            child = Updates.ApplyChild(child);
                            if (assemblyEvaluator.Evaluate(child, opt) > 0)
                                lock (candidates)
                                    candidates.Add(child.performanceParams, child);
                            child.addToRecipe(opt);
                        }
                        Updates.UpdateAssemblyGraph(assemblyGraph);
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

    }
}
