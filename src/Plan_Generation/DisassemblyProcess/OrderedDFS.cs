using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assembly_Planner;
using GraphSynth;
using GraphSynth.Representation;
//using GraphSynth.Search;
using TVGL.IOFunctions;
using TVGL;
using Assembly_Planner.GraphSynth.BaseClasses;

namespace Assembly_Planner
{
    class OrderedDFS
    {
        private TessellatedSolid tessellatedSolid;
        public static Dictionary<int, List<List<Component>>> SccTracker = new Dictionary<int, List<List<Component>>>();
        protected static AssemblyEvaluator assemblyEvaluator;
        internal static List<AssemblyCandidate> Run(designGraph assemblyGraph, List<TessellatedSolid>solids, List<int> globalDirPool, List<TessellatedSolid> solides)
        {
            //DisassemblyDirections.Directions = TemporaryDirections();
            var solutions = new List<AssemblyCandidate>();
            assemblyEvaluator = new AssemblyEvaluator(solids);
            //Updates.UpdateGlobalDirections(globalDirPool);
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
                        var blockingDic = DBG.DirectionalBlockingGraph(current.graph, cndDirInd);
                        options.AddRange(OptionGeneratorPro.GenerateOptions(current.graph, seperateHy, blockingDic, options));
                    }
                }
                foreach (var opt in options)
                {
                    //var child = (AssemblyCandidate) current.copy();
                    //SearchProcess.transferLmappingToChild(child.graph, current.graph, opt);
                    //var rest = Updates.AddSecondHyperToOption(child, opt);
                    //Updates.ApplyChild(child, opt);
                    //if (assemblyEvaluator.Evaluate(child, opt, rest, solides) > 0)
                    //    candidates.Add(child.performanceParams, child);
                    //child.addToRecipe(opt);
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
            return current.graph.hyperarcs.Where(h => h.localLabels.Contains("Done")).Count() == 12;
        }

        private static void TemporaryFixingSequence(AssemblyCandidate goal)
        {
            var subAsms = goal.Sequence.Subassemblies;
            for (var i = subAsms.Count - 1; i > 0; i--)
            {
                foreach (var sub in subAsms)
                {
                    if (sub.Install.Moving.PartNames.All(n => subAsms[i].PartNames.Contains(n)) &&
                        subAsms[i].PartNames.All(n => sub.Install.Moving.PartNames.Contains(n)))
                        sub.Install.Moving = subAsms[i];
                    if (sub.Install.Reference.PartNames.All(n => subAsms[i].PartNames.Contains(n)) &&
                        subAsms[i].PartNames.All(n => sub.Install.Reference.PartNames.Contains(n)))
                        sub.Install.Reference = subAsms[i];
                }
            }
            
        }
       
    }
}
