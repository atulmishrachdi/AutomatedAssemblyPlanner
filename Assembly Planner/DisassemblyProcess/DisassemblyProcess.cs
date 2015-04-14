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

namespace Assembly_Planner
{
    public class DisassemblyProcess //: AbstractAssemblySearch
    {
        public static Dictionary<int, List<List<node>>> SccTracker = new Dictionary<int, List<List<node>>>();
        protected static AssemblyEvaluator assemblyEvaluator;
        internal static void Run(ConvexHullAndBoundingBox inputData, List<int> globalDirPool)
        {
            var assemblyGraph = inputData.graphAssembly;
            DisassemblyDirections.Directions = TemporaryDirections();
            //DisassemblyDirections.Directions = Icosahedron.DirectionGeneration();
            // take a direction from the pool
            //   find the SCCs
            //   create the DBG
            //   generate the options
            assemblyEvaluator = new AssemblyEvaluator(inputData.ConvexHullDictionary);

            Updates.UpdateGlobalDirections(globalDirPool);
            assemblyGraph.addHyperArc(assemblyGraph.nodes);
            var iniHy = assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1];
            iniHy.localLabels.Add(DisConstants.SeperateHyperarcs);

            var candidates = new SortedList<List<double>, AssemblyCandidate>(new MO_optimizeSort());
            var beam = new Queue<AssemblyCandidate>(DisConstants.BeamWidth);
            var found = false;
            AssemblyCandidate goal = null;
            beam.Enqueue(new AssemblyCandidate(new candidate(assemblyGraph,1)));
            var recogRule = new grammarRule();
            recogRule.L = new designGraph();
            var haRemovable = new ruleHyperarc();
            haRemovable.localLabels.Add(DisConstants.Removable);
            recogRule.L.addHyperArc(haRemovable);

            while (beam.Count != 0 && !found)
            {
                candidates.Clear();
                foreach (var current in beam)
                {
                    foreach (var cndDirInd in globalDirPool)
                    {
                        foreach (var seperateHy in current.graph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SeperateHyperarcs)).ToList())
                        {
                            //SCC.StronglyConnectedComponents(assemblyGraph, seperateHy, cndDirInd);
                            OptimizedSCC.StronglyConnectedComponents(current.graph, seperateHy, cndDirInd);
                            var blockingDic = DBG.DirectionalBlockingGraph(current.graph, seperateHy, cndDirInd);
                            OptionGenerator.GenerateOptions(current.graph, seperateHy, blockingDic);
                        }
                    }
                    var ruleChoices = recogRule.recognize(current.graph);
                    foreach (var opt in ruleChoices)
                    {
                        var child = (AssemblyCandidate)current.copy();
                        SearchProcess.transferLmappingToChild(child.graph, current.graph, opt);
                        Updates.ApplyChild(child);
                        opt.hyperarcs.Add(Updates.AddSecondHyperToOption(child,opt));
                        if (assemblyEvaluator.Evaluate(child, opt) > 0)
                            lock (candidates)
                                candidates.Add(child.performanceParams, child);
                        child.addToRecipe(opt);
                    }
                    Updates.UpdateAssemblyGraph(current.graph);
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

        private static bool isCurrentTheGoal(AssemblyCandidate c)
        {
            throw new NotImplementedException();
        }

        //public override string text
        //{
        //    get { throw new NotImplementedException(); }
        //}

        //protected override void getAssemblyPlan(AssemblyCandidate seed, List<AssemblyCandidate> solutions)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
