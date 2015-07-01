using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth;
using GraphSynth.Representation;

namespace Assembly_Planner
{
    class OrderedDFS
    {
        public static Dictionary<int, List<List<node>>> SccTracker = new Dictionary<int, List<List<node>>>();
        protected static AssemblyEvaluator assemblyEvaluator;
        internal static List<AssemblyCandidate> Run(ConvexHullAndBoundingBox inputData, List<int> globalDirPool)
        {
            var assemblyGraph = inputData.graphAssembly;
            //DisassemblyDirections.Directions = TemporaryDirections();
            var solutions = new List<AssemblyCandidate>();
            assemblyEvaluator = new AssemblyEvaluator(inputData.ConvexHullDictionary);

            Updates.UpdateGlobalDirections(globalDirPool);
            assemblyGraph.addHyperArc(assemblyGraph.nodes);
            var iniHy = assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1];
            iniHy.localLabels.Add(DisConstants.SeperateHyperarcs);

            var candidates = new SortedList<List<double>, AssemblyCandidate>(new MO_optimizeSort());
            var found = false;
            AssemblyCandidate goal = null;
            var ini = new AssemblyCandidate(new candidate(assemblyGraph, 1));
            candidates.Add(new List<double>(), ini);

            while (candidates.Count != 0 && !found)
            {
                var current = candidates.Values[0];
                candidates.Clear();
                if (isCurrentTheGoal(current))
                {
                    goal = current;
                    found = true;
                    break;
                }
                var options = new List<option>();
                foreach (var cndDirInd in globalDirPool)
                {
                    foreach (
                        var seperateHy in
                            current.graph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SeperateHyperarcs))
                                .ToList())
                    {
                        SCC.StronglyConnectedComponents(current.graph, seperateHy, cndDirInd);
                        //BoostedSCC.StronglyConnectedComponents(current.graph, seperateHy, cndDirInd);
                        var blockingDic = DBG.DirectionalBlockingGraph(current.graph, seperateHy, cndDirInd);
                        options.AddRange(OptionGeneratorPro.GenerateOptions(current.graph, seperateHy, blockingDic));
                    }
                }
                foreach (var opt in options)
                {
                    var child = (AssemblyCandidate) current.copy();
                    option.transferLmappingToChild(child.graph, current.graph, opt);
                    var rest = Updates.AddSecondHyperToOption(child, opt);
                    Updates.ApplyChild(child, opt);
                    if (assemblyEvaluator.Evaluate(child, opt, rest) > 0)
                        lock (candidates)
                            candidates.Add(child.performanceParams, child);
                    child.addToRecipe(opt);
                }
            }
            solutions.Add(goal);
            TemporaryFixingSequence(goal);
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
            return current.graph.hyperarcs.Where(h => h.localLabels.Contains("Done")).Count() == 15;
        }

        private static void TemporaryFixingSequence(AssemblyCandidate goal)
        {
            var subAsms = goal.Sequence.Subassemblies;
            for (var i = subAsms.Count - 1; i > 0; i--)
            {
                foreach (var sub in subAsms)
                {
                    if (sub.Install.Moving.PartNodes.All(n=>subAsms[i].PartNodes.Contains(n)) &&
                        subAsms[i].PartNodes.All(n=>sub.Install.Moving.PartNodes.Contains(n)))
                        sub.Install.Moving = subAsms[i];
                    if (sub.Install.Reference.PartNodes.All(n => subAsms[i].PartNodes.Contains(n)) &&
                        subAsms[i].PartNodes.All(n => sub.Install.Reference.PartNodes.Contains(n)))
                        sub.Install.Reference = subAsms[i];
                }
            }
            
        }
       
    }
}
