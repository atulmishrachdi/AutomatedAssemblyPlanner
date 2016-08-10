using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth;
using GraphSynth.Representation;
using GraphSynth.Search;
using Assembly_Planner.GraphSynth.BaseClasses;
using TVGL;

namespace Assembly_Planner
{
    public class BeamSearch
    {
        public static Dictionary<int, List<List<Component>>> SccTracker = new Dictionary<int, List<List<Component>>>();
        protected static AssemblyEvaluator assemblyEvaluator;
        internal static List<AssemblyCandidate> Run(designGraph assemblyGraph, List<TessellatedSolid> solids, List<int> globalDirPool)
        {
            //DisassemblyDirections.Directions = TemporaryDirections();
            var solutions = new List<AssemblyCandidate>();
            assemblyEvaluator = new AssemblyEvaluator(solids);

            Updates.UpdateGlobalDirections(globalDirPool);
            assemblyGraph.addHyperArc(assemblyGraph.nodes);
            var iniHy = assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1];
            iniHy.localLabels.Add(DisConstants.SeperateHyperarcs);

            var candidates = new SortedList<List<double>, AssemblyCandidate>(new MO_optimizeSort());
            var beam = new Queue<AssemblyCandidate>(DisConstants.BeamWidth);
            var found = false;
            AssemblyCandidate goal = null;
            beam.Enqueue(new AssemblyCandidate(new candidate(assemblyGraph,1)));
            while (beam.Count != 0 && !found)
            {
                candidates.Clear();
                foreach (var current in beam)
                {
                    var seperateHys = current.graph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SeperateHyperarcs)).ToList();
                    var options = new List<option>();
                    foreach (var cndDirInd in globalDirPool)
                    {
                        foreach (var seperateHy in seperateHys)
                        {
                            SCC.StronglyConnectedComponents(current.graph, seperateHy, cndDirInd);
                            //OptimizedSCC.StronglyConnectedComponents(current.graph, seperateHy, cndDirInd);
                            var blockingDic = DBG.DirectionalBlockingGraph(current.graph, cndDirInd);
                            //OptionGeneratorPro.GenerateOptions(current.graph, seperateHy, blockingDic);
                            options.AddRange(OptionGeneratorPro.GenerateOptions(current.graph, seperateHy, blockingDic, options));
                        }
                    }
                    //var ruleChoices = recogRule.recognize(current.graph);
                    foreach (var opt in options)
                    {
                        var child = (AssemblyCandidate)current.copy();
                        SearchProcess.transferLmappingToChild(child.graph, current.graph, opt);
                        var rest = Updates.AddSecondHyperToOption(child,opt);
                        Updates.ApplyChild(child, opt);
                        //if (assemblyEvaluator.Evaluate(child, opt,rest) > 0)
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
                    if (++count > DisConstants.BeamWidth)
                        break;
                    beam.Enqueue(c);
                }
            }
            solutions.Add(goal);
            return solutions;
        }

        private static List<double[]> TemporaryDirections()
        {
            var list = new List<double[]>
            {
                new[] {1.0, 0.0, 0.0}, 
                new[] {-1.0, 0.0, 0.0},
                new[] {0.0, 0.0, -1.0},
                new[] {0.0, 0.0, 1.0},
                new[] {0.0, 1.0, 0.0},
                new[] {0.0, -1.0, 0.0}
            };
            return list;
        }

        protected static bool isCurrentTheGoal(AssemblyCandidate current)
        {
            return current.graph.hyperarcs.Where(h => h.localLabels.Contains("Done")).Count() == 20;
        }

    }
}
