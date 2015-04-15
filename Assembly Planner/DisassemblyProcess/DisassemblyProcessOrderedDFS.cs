using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth;
using GraphSynth.Representation;
using GraphSynth.Search;

namespace Assembly_Planner
{
    class DisassemblyProcessOrderedDFS
    {
        public static Dictionary<int, List<List<node>>> SccTracker = new Dictionary<int, List<List<node>>>();
        protected static AssemblyEvaluator assemblyEvaluator;
        internal static void Run(ConvexHullAndBoundingBox inputData, List<int> globalDirPool)
        {
            var assemblyGraph = inputData.graphAssembly;
            DisassemblyDirections.Directions = TemporaryDirections();
            //DisassemblyDirections.Directions = Icosahedron.DirectionGeneration();
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

            var recogRule = new grammarRule();
            recogRule.L = new designGraph();
            var haRemovable = new ruleHyperarc();
            haRemovable.localLabels.Add(DisConstants.Removable);
            recogRule.L.addHyperArc(haRemovable);

            while (candidates.Count != 0 && !found)
            {
                var current = candidates.Values[0];
                candidates.RemoveAt(0);
                if (isCurrentTheGoal(current))
                {
                    goal = current;
                    found = true;
                    break;
                }
                foreach (var cndDirInd in globalDirPool)
                {
                    foreach (
                        var seperateHy in
                            current.graph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SeperateHyperarcs))
                                .ToList())
                    {
                        SCC.StronglyConnectedComponents(assemblyGraph, seperateHy, cndDirInd);
                        //OptimizedSCC.StronglyConnectedComponents(current.graph, seperateHy, cndDirInd);
                        var blockingDic = DBG.DirectionalBlockingGraph(current.graph, seperateHy, cndDirInd);
                        OptionGenerator.GenerateOptions(current.graph, seperateHy, blockingDic);
                    }
                }
                var ruleChoices = recogRule.recognize(current.graph);
                foreach (var opt in ruleChoices)
                {
                    var child = (AssemblyCandidate) current.copy();
                    SearchProcess.transferLmappingToChild(child.graph, current.graph, opt);
                    Updates.ApplyChild(child, opt);
                    opt.hyperarcs.Add(Updates.AddSecondHyperToOption(child, opt));
                    if (assemblyEvaluator.Evaluate(child, opt) > 0)
                        lock (candidates)
                            candidates.Add(child.performanceParams, child);
                    child.addToRecipe(opt);
                }
                Updates.UpdateAssemblyGraph(current.graph);

            }
            solutions.Add(goal);
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
            var result = (current.graph.hyperarcs.Count == 1 &&
                !current.graph.globalLabels.Contains("invalid"));
            var saveMe = false;
            //if (saveMe)
            //Save("debugGraph" + DateTime.Now.Millisecond, current.graph);
            return result;
        }
    }
}
