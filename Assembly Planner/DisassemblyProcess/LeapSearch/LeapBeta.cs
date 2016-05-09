using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth;
using GraphSynth.Representation;
using GraphSynth.Search;
using Assembly_Planner.GraphSynth.BaseClasses;
using TVGL;
using Constants = AssemblyEvaluation.Constants;

namespace Assembly_Planner
{
    class LeapBeta
    {
        protected static EvaluationForBinaryTree AssemblyEvaluator;
        protected static int[] Count = new int[100];
        protected static designGraph Graph;
        protected static List<int> DirPool;
        protected static int TimeEstm;
        protected static int TimeEstmCounter;
        protected static SortedList<double, HashSet<TreeCandidate>> SortedStack;

        protected static Dictionary<HashSet<Component>, MemoData> Memo =
            new Dictionary<HashSet<Component>, MemoData>(HashSet<Component>.CreateSetComparer());

        protected static Dictionary<HashSet<Component>, HashSet<TreeCandidate>> MemoCandidates =
            new Dictionary<HashSet<Component>, HashSet<TreeCandidate>>(HashSet<Component>.CreateSetComparer());

        internal static HashSet<HashSet<Component>> FrozenSequence = new HashSet<HashSet<Component>>();

        internal static AssemblySequence Run(designGraph graph, Dictionary<string, List<TessellatedSolid>> solids,
            List<int> globalDirPool, int beamWidth)
        {
            //if (Bridge.RestartBoolean) ClearEveryThing();
            Graph = graph;
            AssemblyEvaluator = new EvaluationForBinaryTree(solids);
            TimeEstm = 50;
            TimeEstmCounter = 0;
            Constants.Values = new Constants();
            DirPool = globalDirPool;
            InitializeMemo();
            var w = new Stopwatch();
            w.Start();
            SubAssembly tree = null;
            SortedStack = new SortedList<double, HashSet<TreeCandidate>>(new CandidateComparer());
            var cands = GetCandidates(new HashSet<Component>(graph.nodes.Cast<Component>()));
            foreach (var c in cands)
                SortedStack.Add(c.G + c.H, new HashSet<TreeCandidate> {c});
            while (SortedStack.Any())
            {
                TimeEstmCounter++;
                TimeEstm++;
                UpdateSortedStackWithBeamWidth(beamWidth);
                var cand = SortedStack.First().Value;
                SortedStack.RemoveAt(0);
                //var all = new List<SortedList<double, HashSet<TreeCandidate>>>();
                var newCand = new HashSet<TreeCandidate>();
                //Parallel.ForEach(cand, treeCandidate =>
                // if all the members of cand have moving and references that exist in the memo, this is the goal
                var counter = 0;
                var temp = new HashSet<TreeCandidate>();
                foreach (var treeCandidate in cand)
                {
                    var localSorted = new SortedList<double, HashSet<TreeCandidate>>(new CandidateComparer());
                    if (Memo.ContainsKey(treeCandidate.MovNodes) && Memo.ContainsKey(treeCandidate.RefNodes))
                    {
                        var time = Memo[treeCandidate.MovNodes].Value + Memo[treeCandidate.RefNodes].Value +
                                   treeCandidate.sa.Install.Time;
                        var d = new MemoData(time, treeCandidate.sa);
                        var a = new HashSet<Component>(treeCandidate.MovNodes);
                        a.UnionWith(treeCandidate.RefNodes);
                        if (!Memo.ContainsKey(a))
                            Memo.Add(a, d);
                        else
                        {
                            if (Memo[a].Value > d.Value)
                                Memo[a] = d;
                        }
                        temp.Add(treeCandidate);
                        treeCandidate.sa.Install.Moving = Memo[treeCandidate.MovNodes].sa;
                        treeCandidate.sa.Install.Reference = Memo[treeCandidate.RefNodes].sa;
                        counter++;
                        continue;
                    }
                    if (Memo.ContainsKey(treeCandidate.MovNodes))
                    {
                        treeCandidate.sa.Install.Moving = Memo[treeCandidate.MovNodes].sa;
                        var refCands = GetCandidates(treeCandidate.RefNodes);
                        foreach (var rC in refCands)
                        {
                            rC.parent = treeCandidate;
                            localSorted.Add(rC.G + rC.H, new HashSet<TreeCandidate> {rC});
                        }
                        var otherTC1 = cand.Where(c => c != treeCandidate);
                        var cost1 = otherTC1.Sum(tc => tc.G + tc.H);
                        foreach (var lsl in localSorted)
                        {
                            var merged = lsl.Value;
                            merged.UnionWith(otherTC1);
                            merged.UnionWith(temp);
                            SortedStack.Add(cost1 + lsl.Key, merged);
                        }
                        //all.Add(localSorted);
                        //continue;
                        break;
                    }
                    if (Memo.ContainsKey(treeCandidate.RefNodes))
                    {
                        treeCandidate.sa.Install.Reference = Memo[treeCandidate.RefNodes].sa;
                        var movCands = GetCandidates(treeCandidate.MovNodes);
                        foreach (var mC in movCands)
                        {
                            mC.parent = treeCandidate;
                            localSorted.Add(mC.G + mC.H, new HashSet<TreeCandidate> {mC});
                        }
                        //all.Add(localSorted);
                        var otherTC2 = cand.Where(c => c != treeCandidate);
                        var cost2 = otherTC2.Sum(tc => tc.G + tc.H);
                        foreach (var lsl in localSorted)
                        {
                            var merged = lsl.Value;
                            merged.UnionWith(otherTC2);
                            merged.UnionWith(temp);
                            SortedStack.Add(cost2 + lsl.Key, merged);
                        }
                        //continue;
                        break;
                    }
                    /*HashSet<TreeCandidate> refCandsF = null, movCandsF = null;
                    var tasks = new Task[2];
                    tasks[0] = Task.Factory.StartNew(() => refCandsF = GetCandidates(treeCandidate.RefNodes));
                    tasks[1] = Task.Factory.StartNew(() => movCandsF = GetCandidates(treeCandidate.MovNodes));
                    Task.WaitAll(tasks);*/
                    var refCandsF = GetCandidates(treeCandidate.RefNodes);
                    var movCandsF = GetCandidates(treeCandidate.MovNodes);
                    
                    foreach (var rC in refCandsF)
                    {
                        rC.parent = treeCandidate;
                        foreach (var mC in movCandsF)
                        {
                            mC.parent = treeCandidate;
                            localSorted.Add(rC.G + rC.H + mC.G + mC.H, new HashSet<TreeCandidate> {rC, mC});
                        }
                    }

                    // NEW APPROACH: instead of addding them to the all, take other members of the cand, merge them with
                    // every member of the local sortedList and add them to the global list
                    var otherTC = cand.Where(c => c != treeCandidate);
                    var cost = otherTC.Sum(tc => tc.G + tc.H);
                    foreach (var lsl in localSorted)
                    {
                        var merged = lsl.Value;
                        merged.UnionWith(otherTC);
                        merged.UnionWith(temp);
                        SortedStack.Add(cost + lsl.Key, merged);
                    }
                    break;
                    //all.Add(localSorted);
                }
                if (counter == cand.Count)
                {
                    w.Stop();
                    var parentsMet = new HashSet<TreeCandidate>();
                    var goal = true;
                    var finalNodes = new List<SubAssembly>();
                    foreach (var tc in cand)
                    {
                        var tempo = new TreeCandidate(tc);
                        var cnt = true;
                        while (cnt)
                        {
                            if (tempo.parent == null)
                            {
                                finalNodes.Add(tempo.sa);
                                break;
                            }
                            if (tempo.parent.sa.Install.Moving.PartNames.Count == tempo.sa.PartNames.Count &&
                                tempo.parent.sa.Install.Moving.PartNames.All(tempo.sa.PartNames.Contains))
                            {
                                tempo.parent.sa.Install.Moving = tempo.sa;
                                if (parentsMet.Contains(tempo.parent))
                                {
                                    cnt = false;
                                    break;
                                }
                                parentsMet.Add(tempo.parent);
                                tempo = tempo.parent;
                                continue;
                            }

                            if (tempo.parent.sa.Install.Reference.PartNames.Count == tempo.sa.PartNames.Count &&
                                tempo.parent.sa.Install.Reference.PartNames.All(tempo.sa.PartNames.Contains))
                            {
                                tempo.parent.sa.Install.Reference = tempo.sa;
                                if (parentsMet.Contains(tempo.parent))
                                {
                                    cnt = false;
                                    break;
                                }
                                parentsMet.Add(tempo.parent);
                                tempo = tempo.parent;
                                continue;
                            }
                            tempo = tempo.parent;
                        }
                    }
                    tree = finalNodes[0];

                }
                if (tree != null) break;
                //);
                /*while (all.Count > 1)
                {
                    var localSorted2 = new SortedList<double, HashSet<TreeCandidate>>(new CandidateComparer());
                    for (var i = 0; i < 10; i++)
                    {
                        for (var j = 0; j < 10; j++)
                        {
                            var first = all[0].First();
                            var second = all[1].First();
                            var value = first.Value;
                            value.UnionWith(second.Value);
                            localSorted2.Add(first.Key + second.Key, value);
                        }
                    }
                    all[0] = localSorted2;
                    all.RemoveAt(1);
                }
                foreach (var c in all[0])
                    SortedStack.Add(c.Key, c.Value);*/
            }
            //var best = F(out tree, new HashSet<Component>(Graph.nodes.Cast<Component>()));
            return new AssemblySequence {Subassemblies = new List<SubAssembly> {tree}};
        }

        private static void UpdateSortedStackWithBeamWidth(int beamWidth)
        {
            if (SortedStack.Count > beamWidth)
                for (var i = SortedStack.Count - 1; i > beamWidth; i--)
                    SortedStack.RemoveAt(i);
        }

        private static HashSet<TreeCandidate> GetCandidates(HashSet<Component> nodes, double G = 0, bool relaxingSc = false)
        {
            if (MemoCandidates.ContainsKey(nodes)) 
                return MemoCandidates[nodes];
            var gOptions = new Dictionary<option, HashSet<int>>();
            var candidates = new HashSet<TreeCandidate>();
            GenerateOptions(nodes, gOptions, relaxingSc);
            var c = 0;
            foreach (var opt in gOptions.Keys)
            {
                var TC = new TreeCandidate();
                if (AssemblyEvaluator.EvaluateSub(Graph, nodes, opt.nodes.Cast<Component>().ToList(), gOptions[opt],
                    out TC.sa) <= 0)
                {
                    c++;
                    continue;
                }
                TC.RefNodes =
                    new HashSet<Component>(TC.sa.Install.Reference.PartNames.Select(n => (Component) Graph[n]));
                TC.MovNodes =
                    new HashSet<Component>(TC.sa.Install.Moving.PartNames.Select(n => (Component) Graph[n]));
                //if (Math.Min (TC.RefNodes.Count, TC.MovNodes.Count) > 21)	//example constraint
                //	continue;

                var HR = H(TC.RefNodes);
                var HM = H(TC.MovNodes);
                TC.G = G;
                TC.H = TC.sa.Install.Time + Math.Max(HR, HM);
                candidates.Add(TC);
            }
            // if candidates.Count is zero, s.th. is wrong. Meaning that we have a subassembly that cannot be disassembled
            // It means that finite/inf directions suck or secondary connections.
            // since user is checking the removal directions, we can simply assume the problem is only with secondary connections. 
            if (c == gOptions.Count)
            {
                candidates = GetCandidates(nodes, 0, true);
            }
            if (MemoCandidates.ContainsKey(nodes))
                return MemoCandidates[nodes];
            MemoCandidates.Add(nodes, candidates);
            return candidates;
        }

        private static void GenerateOptions(HashSet<Component> A, Dictionary<option, HashSet<int>> gOptions, bool relaxingSc = false)
        {
            foreach (var cndDirInd in DirPool)
            {
                SCCBinary.StronglyConnectedComponents(Graph, A, cndDirInd);
                var blockingDic = DBGBinary.DirectionalBlockingGraph(Graph, cndDirInd, relaxingSc);
                var options = OptionGeneratorProBinary.GenerateOptions(Graph, A, blockingDic, gOptions, cndDirInd);
                foreach (var opt in options)
                    gOptions.Add(opt, new HashSet<int> {cndDirInd});
            }
        }

        protected static double H(HashSet<Component> A)
        {
            if (A.Count <= 1)
                return 0;

            if (Memo.ContainsKey(A))
                return Memo[A].Value;

            var L = Math.Log(A.Count, 2);
            double MinTreeDepth = Math.Ceiling(L);
            var hy = Graph.addHyperArc(A.Cast<node>().ToList());

            List<double> Values = new List<double>();
            foreach (Connection arc in hy.IntraArcs.Where(a => a is Connection))
            {
                HashSet<Component> arcnodes =
                    new HashSet<Component>(new Component[] {(Component) arc.From, (Component) arc.To});
                Values.Add(Memo[arcnodes].Value);
            }
            Graph.removeHyperArc(hy);
            Values.Sort();

            double total = 0;
            for (int x = 0; x < MinTreeDepth; x++)
                total = total + Values[x];

            return Math.Max(total, Values.Last());
        }

        protected static void InitializeMemo()
        {
            foreach (Connection arc in Graph.arcs.Where(a => a is Connection))
            {
                var Asm = new HashSet<Component>(new Component[] {(Component) arc.From, (Component) arc.To});
                var Fr = new List<Component>(new Component[] {(Component) arc.From});
                var dirs = new HashSet<int>(arc.InfiniteDirections);
                SubAssembly sa;
                if (AssemblyEvaluator.EvaluateSub(Graph, Asm, Fr, dirs, out sa) > 0)
                {
                    HashSet<Component> A = new HashSet<Component>(Asm);
                    MemoData D = new MemoData(sa.Install.Time, sa);
                    Memo.Add(A, D);
                }
            }

            foreach (var node in Graph.nodes)
            {
                var component = (Component) node;
                var N = new HashSet<Component>(new[] {component});
                var sa = new SubAssembly(N, EvaluationForBinaryTree.ConvexHullsForParts[component.name], component.Mass,
                    component.Volume, new Vertex(component.CenterOfMass));
                MemoData D = new MemoData(0, sa);
                Memo.Add(N, D);
            }
        }

        private static void ClearEveryThing()
        {
            Memo.Clear();
            MemoCandidates.Clear();
            FrozenSequence.Clear();
        }
    }

    class LeapCandidate : IComparable<LeapCandidate>
    {
        public HashSet<TreeCandidate> subAssems;
        public double G, H; //Total G and H

        public int CompareTo(LeapCandidate other)
        {
            var F = G + H;
            var otherF = other.G + other.H;
            //if (F != otherF)
            return F.CompareTo(otherF); //first try to sort on heuristic values

            //var MaxNodes = Math.Max(RefNodes.Count, MovNodes.Count);
            //var OtherMaxNodes = Math.Max(other.RefNodes.Count, other.MovNodes.Count);
            // MaxNodes.CompareTo(OtherMaxNodes); //if they are even, try to split parts evenly
        }
    }

    class CandidateComparer: IComparer<Double>
    {
        public int Compare(double x, double y)
        {
            if (x >= y)
                return 1;
            return -1;
        }
    }
}

