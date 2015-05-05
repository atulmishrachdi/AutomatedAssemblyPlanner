using AssemblyEvaluation;
using GeometryReasoning;
using GraphSynth.Representation;
using GraphSynth.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GraphSynth
{
    public abstract class AbstractAssemblySearch : SearchProcess
    {
        protected static AssemblyEvaluator assemblyEvaluator;
        private readonly Random rnd = new Random();

        protected AbstractAssemblySearch()
        {
            RequiredNumRuleSets = 5;
            RequireSeed = true;
            AutoPlay = true;
        }


        /// <summary>
        /// Gets or sets the nunber of solutions to generate.
        /// </summary>
        /// <value>
        /// The number of solutions, n.
        /// </value>
        public int n { get; set; }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        protected override void Run()
        {
          var  inputData = new InputData(settings);
            assemblyEvaluator = new AssemblyEvaluator(inputData.ConvexHullDictionary);

            //var CAD2Graph = new AssemblyClass();
            //CAD2Graph.Perform(settings.DefaultSeedFileName);
            var solutions = new List<AssemblyCandidate>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            //rulesets[GetRuleSetIndex("bubble4")].rules.RemoveAt(2);
            //rulesets[GetRuleSetIndex("bubble4")].rules.RemoveAt(1);

            //for (int i = 0; i < n; i++)
            //{
                var newSeed = PreProcessSeed();
                newSeed.f0 = newSeed.f1 = newSeed.f2 = newSeed.f3 = newSeed.f4 = 0.0;
                getAssemblyPlan(newSeed, solutions);
            //}
            //WorkerAllocation.Run(solutions);

            stopwatch.Stop();
            outputSolutionPlans(solutions, stopwatch.Elapsed);

        }

        protected int GetRuleSetIndex(string name)
        {
            for (int i = 0; i < rulesets.GetLength(0); i++)
                if (rulesets[i].name == name) return i;
            return -1;
        }


        private AssemblyCandidate PreProcessSeed()
        {
            return new AssemblyCandidate(seedCandidate.copy());
            var seed = new AssemblyCandidate(seedCandidate.copy());
            var options = rulesets[0].recognize(seed.graph);
            while (options.Count > 0)
            {
                options[0].apply(seed.graph, null);
                options = rulesets[0].recognize(seed.graph, false);
            }
            options = rulesets[0].recognize(seed.graph);
            while (options.Count > 0)
            {
                options[0].apply(seed.graph, null);
                options = rulesets[0].recognize(seed.graph, false);
            }
            Dictionary<string, int> NodeOrdering = GetNodeOrderingSeries((AssemblyCandidate)seed.copy());
            //Dictionary<string, int> NodeOrdering = GetNodeOrderingParallel((AssemblyCandidate)seed.copy());
            int maxIndex = NodeOrdering.Values.Max();
            foreach (var n in seed.graph.nodes)
            {
                n.localVariables.Add(-8000);
                var score = ((double)(maxIndex - (NodeOrdering[n.name]))) / maxIndex;
                n.localVariables.Add(score);
                SearchIO.output(n.name + ": " + score);
            }
            
            foreach (var n in seed.graph.nodes.Where(n => n.degree == 0))
                SearchIO.output("Node " + n.name + "does not connect to any other nodes!!", 0);
            seed.graph.nodes.RemoveAll(n => n.degree == 0);
            foreach (var h in seed.graph.hyperarcs)
            {
                h.localVariables.Add(0);
            }
            return seed;
        }

        private Dictionary<string, int> GetNodeOrderingSeries(AssemblyCandidate current)
        {
            var NodeOrdering = new Dictionary<string, int>();
            int layerNumber = 0;
            int overallFreeRSIndex = GetRuleSetIndex("overallfree");
            if (overallFreeRSIndex == -1)
                throw new Exception("missing rulesets, put the following rulesets in the following order: overallfree, disassembly, disassemblyhousekeeping");
            current.activeRuleSetIndex = overallFreeRSIndex;
            var disassemblyRSIndex = overallFreeRSIndex + 1;
            var housekeepingRSIndex = disassemblyRSIndex + 1;
            while (current.activeRuleSetIndex >= overallFreeRSIndex)
            //preprocessing ruleset goes before this, order of rulesets: overallfree, disassembly, disassembly housekeeping
            {
                int rsIndex = current.activeRuleSetIndex;
                List<option> ruleChoices = rulesets[rsIndex].recognize(current.graph);
                if (ruleChoices.Count == 0)
                {
                    if (rsIndex == disassemblyRSIndex) break;
                }
                else
                {
                    if (rsIndex == overallFreeRSIndex || rsIndex == housekeepingRSIndex)
                        ruleChoices[0].apply(current.graph, null);
                    else if (rsIndex == disassemblyRSIndex)
                    {
                        var opt = ruleChoices[rnd.Next(ruleChoices.Count)];
                        opt.apply(current.graph, null);
                        current.activeRuleSetIndex = setStatusAndNewRuleSet(current, opt);
                        NodeOrdering.Add(opt.nodes[0].name, layerNumber);

                        layerNumber++;
                    }
                }
                if (ruleChoices.Count == 0)
                {
                    current.GenerationStatus[rsIndex] = GenerationStatuses.NoRules;
                    current.activeRuleSetIndex = nextRuleSet(rsIndex, current.GenerationStatus[rsIndex]);
                    if (current.activeRuleSetIndex == 0)
                        current.activeRuleSetIndex = overallFreeRSIndex;
                }
            }
            /* for any left at end, give them the highest value. */
            foreach (var n in current.graph.nodes)
                NodeOrdering.Add(n.name, layerNumber);
            SearchIO.output(IntCollectionConverter.Convert(current.optionNumbersInRecipe), 3);
            return NodeOrdering;
        }
        private Dictionary<string, int> GetNodeOrderingParallel(AssemblyCandidate current)
        {
            var NodeOrdering = new Dictionary<string, int>();
            int layerNumber = 0;
            int overallFreeRSIndex = GetRuleSetIndex("overallfree");
            if (overallFreeRSIndex == -1)
                throw new Exception("missing rulesets, put the following rulesets in the following order: overallfree, disassembly, disassemblyhousekeeping");
            current.activeRuleSetIndex = overallFreeRSIndex;
            var disassemblyRSIndex = overallFreeRSIndex + 1;
            var housekeepingRSIndex = disassemblyRSIndex + 1;
            while (current.activeRuleSetIndex >= overallFreeRSIndex)
            //preprocessing ruleset goes before this, order of rulesets: overallfree, disassembly, disassembly housekeeping
            {
                int rsIndex = current.activeRuleSetIndex;
                List<option> ruleChoices = rulesets[rsIndex].recognize(current.graph);
                if (ruleChoices.Count == 0)
                {
                    if (rsIndex == disassemblyRSIndex) break;
                }
                else
                {
                    if (rsIndex == overallFreeRSIndex || rsIndex == housekeepingRSIndex)
                        ruleChoices[0].apply(current.graph, null);
                    else if (rsIndex == disassemblyRSIndex)
                    {
                        foreach (var opt in ruleChoices) //if we're in ruleset 0 apply all recognized options
                        {
                            opt.apply(current.graph, null);
                            ruleChoices = rulesets[rsIndex].recognize(current.graph);
                            current.activeRuleSetIndex = setStatusAndNewRuleSet(current, opt);
                            NodeOrdering.Add(opt.nodes[0].name, layerNumber);
                        }
                        layerNumber++;
                    }
                }
                if (ruleChoices.Count == 0)
                {
                    current.GenerationStatus[rsIndex] = GenerationStatuses.NoRules;
                    current.activeRuleSetIndex = nextRuleSet(rsIndex, current.GenerationStatus[rsIndex]);
                    if (current.activeRuleSetIndex == 0)
                        current.activeRuleSetIndex = overallFreeRSIndex;
                }
            }
            /* for any left at end, give them the highest value. */
            foreach (var n in current.graph.nodes)
                NodeOrdering.Add(n.name, layerNumber);
            SearchIO.output(IntCollectionConverter.Convert(current.optionNumbersInRecipe), 3);
            return NodeOrdering;
        }


        private void outputSolutionPlans(List<AssemblyCandidate> solutions, TimeSpan timeSpan)
        {
            var step = 0;
            while (step < solutions.Count && step < n)
            {
                var goal = solutions[step];
                if (goal != null)
                {
                    SearchIO.output("\n**** Optimized Assembly Plan: " + step + "****");
                    SearchIO.output("    Total time = " + goal.TotalTime);
                    SearchIO.output("    MakeSpan (Max. Condensed time) = " + goal.MakeSpan);
                    SearchIO.output("    Number of Workers to achieve MakeSpan = " + (goal.performanceParams.Count - 3));
                    SearchIO.output("    Accessibility Score = " + goal.AccessibilityScore);
                    SearchIO.output("    Stability Score = " + goal.StabilityScore);
                    //SearchIO.output("    Order Score = " + goal.OrderScore);
                    goal.SaveToDisk(settings.OutputDirAbs + "BestCandidate" + step + ".xml");
                    PathDeterminationEvaluator.SaveToDisk(settings.OutputDirAbs + "BestCandidate" + step + ".txt", goal);
                }
                step++;
            }
            SearchIO.output("*********** completed! ************** (Press any key to close).");
            //Console.ReadKey();
        }
        protected abstract void getAssemblyPlan(AssemblyCandidate seed, List<AssemblyCandidate> solutions);

        protected   bool isCurrentTheGoal(AssemblyCandidate current)
        {
            var result = (current.graph.hyperarcs.Count == 1 &&
                !current.graph.globalLabels.Contains("invalid"));
            var saveMe = false;
            if (saveMe)
                Save("debugGraph" + DateTime.Now.Millisecond, current.graph);
            return result;
        }
        protected int setStatusAndNewRuleSet(AssemblyCandidate child, option opt)
        {
            //  child.addToRecipe(opt);

            var rsIndex = child.activeRuleSetIndex;
            if (opt.ruleNumber == rulesets[rsIndex].TriggerRuleNum)
            {
                /* your ruleset loops until a trigger rule and the trigger rule was just called. */
                SearchIO.output("The trigger rule has been chosen.", 4);
                child.GenerationStatus[rsIndex] = GenerationStatuses.TriggerRule;
            }
            else
            {
                /* Normal operation */
                SearchIO.output("RCA loop executed normally.", 4);
                child.GenerationStatus[rsIndex] = GenerationStatuses.Normal;
            }
            rsIndex = nextRuleSet(rsIndex, child.GenerationStatus[rsIndex]);
            return rsIndex;
        }
        protected node[] hyperarcnodelist(hyperarc ha)
        {
            var nodelist = ha.nodes.ToArray();
            return nodelist;
        }
    }
}

