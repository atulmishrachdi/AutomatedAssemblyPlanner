using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth;
using GraphSynth.Representation;
using GraphSynth.Search;
using Assembly_Planner.GraphSynth.BaseClasses;
using StarMathLib;
using TVGL;
using Constants = AssemblyEvaluation.Constants;

namespace Assembly_Planner
{
    class LeapSearch
    {
        protected static EvaluationForBinaryTree AssemblyEvaluator;
        protected static designGraph Graph;
        protected static List<int> DirPool;
        protected static int TimeEstm;
        protected static int TimeEstmCounter;
        protected static SortedList<double, HashSet<TreeCandidate>> SortedStack;
        protected static int BeamWidth;

        protected static Dictionary<HashSet<Component>, MemoData> Memo =
            new Dictionary<HashSet<Component>, MemoData>(HashSet<Component>.CreateSetComparer());

        protected static Dictionary<HashSet<Component>, HashSet<TreeCandidate>> MemoCandidates =
            new Dictionary<HashSet<Component>, HashSet<TreeCandidate>>(HashSet<Component>.CreateSetComparer());

        internal AssemblySequence Run(designGraph graph, Dictionary<string, List<TessellatedSolid>> solids,
            List<int> globalDirPool, int beamWidth)
        {
            Graph = graph;
            AssemblyEvaluator = new EvaluationForBinaryTree(solids);
            TimeEstm = 50;
            TimeEstmCounter = 0;
            BeamWidth = beamWidth;
            Constants.Values = new Constants();
            DirPool = globalDirPool;

            var initialMemo = InitializeMemoInitial();
            /*SubAssembly tree = null;
            
            var tokenSource = new CancellationTokenSource();
            var tasks = new[]
            {
                Task.Factory.StartNew(() => InitializeMemoBoosted(initialMemo), tokenSource.Token),
                Task.Factory.StartNew(() =>
                {
                    tree = LeapOptimizationSearch();
                })
            };
            Task.WaitAny(tasks);
            tokenSource.Cancel();
            tokenSource.Dispose();
            InitializeMemo();*/
            var tree = LeapOptimizationSearch();
            return new AssemblySequence { Subassemblies = new List<SubAssembly> { tree } };
        }

        private static SubAssembly LeapOptimizationSearch()
        {
            SubAssembly tree = null;
            SortedStack = new SortedList<double, HashSet<TreeCandidate>>(new CandidateComparer());
            var beamChildern = new SortedList<double, HashSet<TreeCandidate>>(new CandidateComparer());
            var cands = GetCandidates(new HashSet<Component>(Graph.nodes.Cast<Component>()), 0);
            foreach (var c in cands)
                SortedStack.Add(c.G + c.H, new HashSet<TreeCandidate> { c });
            while (SortedStack.Any())
            {
                TimeEstmCounter++;
                TimeEstm++;
                UpdateSortedStackWithBeamWidth(BeamWidth);
                var cand = SortedStack.First().Value;
                SortedStack.RemoveAt(0);
                //var all = new List<SortedList<double, HashSet<TreeCandidate>>>();
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
                        {
                            lock (Memo)
                            {
                                Memo.Add(a, d);
                            }
                        }
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
                        var refCands = GetCandidates(treeCandidate.RefNodes, treeCandidate.G);
                        foreach (var rC in refCands)
                        {
                            rC.parent = treeCandidate;
                            localSorted.Add(rC.G + rC.H, new HashSet<TreeCandidate> { rC });
                        }
                        var otherTC1 = cand.Where(c => c != treeCandidate);
                        var cost1 = otherTC1.Sum(tc => tc.G + tc.H);
                        foreach (var lsl in localSorted)
                        {
                            var merged = lsl.Value;
                            merged.UnionWith(otherTC1);
                            merged.UnionWith(temp);
                            beamChildern.Add(cost1 + lsl.Key, merged);
                        }
                        //all.Add(localSorted);
                        //continue;
                        break;
                    }
                    if (Memo.ContainsKey(treeCandidate.RefNodes))
                    {
                        treeCandidate.sa.Install.Reference = Memo[treeCandidate.RefNodes].sa;
                        var movCands = GetCandidates(treeCandidate.MovNodes, treeCandidate.G);
                        foreach (var mC in movCands)
                        {
                            mC.parent = treeCandidate;
                            localSorted.Add(mC.G + mC.H, new HashSet<TreeCandidate> { mC });
                        }
                        //all.Add(localSorted);
                        var otherTC2 = cand.Where(c => c != treeCandidate);
                        var cost2 = otherTC2.Sum(tc => tc.G + tc.H);
                        foreach (var lsl in localSorted)
                        {
                            var merged = lsl.Value;
                            merged.UnionWith(otherTC2);
                            merged.UnionWith(temp);
                            beamChildern.Add(cost2 + lsl.Key, merged);
                        }
                        //continue;
                        break;
                    }
                    /*HashSet<TreeCandidate> refCandsF = null, movCandsF = null;
                    var tasks = new Task[2];
                    tasks[0] = Task.Factory.StartNew(() => refCandsF = GetCandidates(treeCandidate.RefNodes));
                    tasks[1] = Task.Factory.StartNew(() => movCandsF = GetCandidates(treeCandidate.MovNodes));
                    Task.WaitAll(tasks);*/
                    var refCandsF = GetCandidates(treeCandidate.RefNodes, treeCandidate.G);
                    var movCandsF = GetCandidates(treeCandidate.MovNodes, treeCandidate.G);

                    foreach (var rC in refCandsF)
                    {
                        rC.parent = treeCandidate;
                        foreach (var mC in movCandsF)
                        {
                            mC.parent = treeCandidate;
                            localSorted.Add(rC.G + rC.H + mC.G + mC.H, new HashSet<TreeCandidate> { rC, mC });
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
                        beamChildern.Add(cost + lsl.Key, merged);
                    }
                    break;
                    //all.Add(localSorted);
                }

                if (!SortedStack.Any())
                    foreach (var child in beamChildern)
                        SortedStack.Add(child.Key, child.Value);
                beamChildern.Clear();
                if (counter == cand.Count)
                    tree = CreateTheSequence(cand);
                if (tree != null) break;
            }
            return tree;
        }

        private static SubAssembly CreateTheSequence(HashSet<TreeCandidate> cand)
        {
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
            return finalNodes[0];
        }

        private static void UpdateSortedStackWithBeamWidth(int beamWidth)
        {
            if (SortedStack.Count > beamWidth)
                for (var i = SortedStack.Count - 1; i >= beamWidth; i--)
                    SortedStack.RemoveAt(i);
        }

        private static HashSet<TreeCandidate> GetCandidates(HashSet<Component> nodes, double parentTransitionCost, bool relaxingSc = false)
        {
            var gOptions = new Dictionary<option, HashSet<int>>();
            var candidates = new HashSet<TreeCandidate>();
            if (MemoCandidates.ContainsKey(nodes))
                return MemoCandidates[nodes];
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
                    new HashSet<Component>(TC.sa.Install.Reference.PartNames.Select(n => (Component)Graph[n]));
                TC.MovNodes =
                    new HashSet<Component>(TC.sa.Install.Moving.PartNames.Select(n => (Component)Graph[n]));
                //if (Math.Min (TC.RefNodes.Count, TC.MovNodes.Count) > 21)	//example constraint
                //	continue;

                var HR = H(TC.RefNodes);
                var HM = H(TC.MovNodes);

                var ssss = TC.sa.InternalStabilityInfo.Totalsecore;
                var ss2 = Constants.Innerstabilityweight * TC.sa.InternalStabilityInfo.Totalsecore;
                TC.G = parentTransitionCost + TC.sa.Install.Time + Constants.Innerstabilityweight * TC.sa.InternalStabilityInfo.Totalsecore;
                TC.H = Math.Max(HR, HM);
                candidates.Add(TC);
            }
            // if candidates.Count is zero, s.th. is wrong. Meaning that we have a subassembly that cannot be disassembled
            // It means that finite/inf directions suck or secondary connections.
            // since user is checking the removal directions, we can simply assume the problem is only with secondary connections. 
            if (c == gOptions.Count)
            {
                candidates = GetCandidates(nodes, parentTransitionCost, true);
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
                    gOptions.Add(opt, new HashSet<int> { cndDirInd });
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
                    new HashSet<Component>(new Component[] { (Component)arc.From, (Component)arc.To });
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
                var Asm = new HashSet<Component>(new Component[] { (Component)arc.From, (Component)arc.To });
                var Fr = new List<Component>(new Component[] { (Component)arc.From });
                var dirs = new HashSet<int>(arc.InfiniteDirections);
                SubAssembly sa;
                if (AssemblyEvaluator.EvaluateSub(Graph, Asm, Fr, dirs, out sa) > 0)
                {
                    HashSet<Component> A = new HashSet<Component>(Asm);
                    MemoData D = new MemoData(sa.Install.Time, sa);
                    lock (Memo)
                    {
                        Memo.Add(A, D);
                    }
                }
            }

            foreach (var node in Graph.nodes)
            {
                var component = (Component)node;
                var N = new HashSet<Component>(new[] { component });
                var sa = new SubAssembly(N, EvaluationForBinaryTree.ConvexHullsForParts[component.name], component.Mass,
                    component.Volume, new Vertex(component.CenterOfMass));
                MemoData D = new MemoData(0, sa);
                lock (Memo)
                {
                    Memo.Add(N, D);
                }
            }
        }

        protected void InitializeMemoBoosted(List<HashSet<HashSet<Component>>> newMemo)
        {
            for (var i = 3; i <= Graph.nodes.Count; i++)
            {
                var combinations = CombinationFinder(i);
                var column = new HashSet<HashSet<Component>>();
                foreach (var comb in combinations)
                //Parallel.ForEach(combinations, comb =>
                {
                    foreach (var subAssem1 in newMemo[comb[0] - 1])
                    {
                        foreach (var subAssem2 in newMemo[comb[1] - 1])
                        {
                            if (subAssem1.Any(subAssem2.Contains)) continue;
                            var connections = new HashSet<Connection>();
                            var secConnections = new HashSet<SecondaryConnection>();
                            connections =
                                new HashSet<Connection>(
                                    subAssem1.SelectMany(
                                        n => n.arcs.Where(c => c is Connection).Cast<Connection>().Where(c =>
                                            (subAssem1.Contains(c.From) && subAssem2.Contains(c.To)) ||
                                            (subAssem1.Contains(c.To) && subAssem2.Contains(c.From)))).ToList());
                            secConnections =
                                new HashSet<SecondaryConnection>(
                                    subAssem1.SelectMany(
                                        n =>
                                            n.arcs.Where(a => a is SecondaryConnection)
                                                .Cast<SecondaryConnection>()
                                                .Where(c =>
                                                    (subAssem1.Contains(c.From) && subAssem2.Contains(c.To)) ||
                                                    (subAssem1.Contains(c.To) && subAssem2.Contains(c.From))))
                                        .ToList());
                            if (!connections.Any()) continue;
                            var dirs = ValidDirectionsFinder(subAssem1, subAssem2, connections);
                            ApplySecondaryConnections(dirs, subAssem1, subAssem2, secConnections);
                            if (!dirs.Any()) continue;
                            var asm = new HashSet<Component>(subAssem1);
                            asm.UnionWith(subAssem2);
                            SubAssembly sa;
                            if (AssemblyEvaluator.EvaluateSub(Graph, asm, subAssem1.ToList(), dirs, out sa) > 0)
                            {
                                if (!Memo.ContainsKey(asm))
                                {
                                    if (subAssem1.Count == sa.Install.Moving.PartNames.Count &&
                                        subAssem1.All(s1 => sa.Install.Moving.PartNames.Contains(s1.name)))
                                    {
                                        sa.Install.Moving = Memo[subAssem1].sa;
                                        sa.Install.Reference = Memo[subAssem2].sa;
                                    }
                                    else
                                    {
                                        sa.Install.Moving = Memo[subAssem2].sa;
                                        sa.Install.Reference = Memo[subAssem1].sa;
                                    }
                                    var D = new MemoData(sa.Install.Time, sa);
                                    lock (Memo)
                                        Memo.Add(asm, D);
                                    lock (column)
                                        column.Add(asm);
                                    continue;
                                }
                                if (sa.Install.Time < Memo[asm].Value)
                                    lock (Memo)
                                        Memo[asm] = new MemoData(sa.Install.Time, sa);
                            }

                        }
                    }
                } //);
                newMemo.Add(column);
                if (newMemo.Count == 3) break;

            }
        }

        protected List<HashSet<HashSet<Component>>> InitializeMemoInitial()
        {
            var newMemo = new List<HashSet<HashSet<Component>>>();
            var column = new HashSet<HashSet<Component>>();
            foreach (var node in Graph.nodes)
            {
                var component = (Component)node;
                var N = new HashSet<Component>(new[] { component });
                var sa = new SubAssembly(N, EvaluationForBinaryTree.ConvexHullsForParts[component.name], component.Mass,
                    component.Volume, new Vertex(component.CenterOfMass));
                MemoData D = new MemoData(0, sa);
                column.Add(N);
                lock (Memo)
                {
                    Memo.Add(N, D);
                }
            }
            newMemo.Add(column);
            column = new HashSet<HashSet<Component>>();
            foreach (Connection arc in Graph.arcs.Where(a => a is Connection))
            {
                var Asm = new HashSet<Component>(new Component[] { (Component)arc.From, (Component)arc.To });
                var Fr = new List<Component>(new Component[] { (Component)arc.From });
                var dirs = new HashSet<int>(arc.InfiniteDirections);
                SubAssembly sa;
                if (AssemblyEvaluator.EvaluateSub(Graph, Asm, Fr, dirs, out sa) > 0)
                {
                    var D = new MemoData(sa.Install.Time, sa);
                    lock (Memo)
                    {
                        Memo.Add(Asm, D);
                    }
                    lock (column)
                        column.Add(Asm);
                }
            }
            newMemo.Add(column);
            return newMemo;
        }

        private void ApplySecondaryConnections(HashSet<int> dirs, HashSet<Component> subAssem1,
            HashSet<Component> subAssem2, HashSet<SecondaryConnection> secConnections)
        {
            // subAssem1 is moving
            foreach (var seConn in secConnections)
            {
                if (subAssem1.Contains(seConn.From))
                    dirs.RemoveWhere(d => ContainsADirection(new HashSet<int>(seConn.Directions), d));
                else
                {
                    var opposities =
                        seConn.Directions.Select(d => DisassemblyDirections.DirectionsAndOppositsForGlobalpool[d]);
                    dirs.RemoveWhere(d => ContainsADirection(new HashSet<int>(opposities), d));
                }
            }
        }

        private HashSet<int> ValidDirectionsFinder(HashSet<Component> subAssem1, HashSet<Component> subAssem2,
            HashSet<Connection> connecs)
        {
            // create the directions by assuming subAssem1 is moving
            var union = new HashSet<int>();
            foreach (var connection in connecs)
            {
                if (subAssem1.Contains(connection.From))
                    union.UnionWith(connection.InfiniteDirections.Where(d => !union.Contains(d)));
                else
                {
                    var opposities =
                        connection.InfiniteDirections.Select(inD => DisassemblyDirections.DirectionsAndOppositsForGlobalpool[inD]);
                    union.UnionWith(opposities.Where(d => !union.Contains(d)));
                }
            }
            return union;
        }
        private static bool ContainsADirection(HashSet<int> dirs, int d)
        {
            if (
                dirs.Any(
                    dir =>
                        Math.Abs(1 -
                                 DisassemblyDirections.Directions[dir].dotProduct(DisassemblyDirections.Directions[d])) <
                        OverlappingFuzzification.CheckWithGlobDirsParall2)) return true;
            return false;
        }
        private static List<int[]> CombinationFinder(int num)
        {
            var list = new List<int[]>();
            if (num == 1) return list;
            for (var i = 1; i < num; i++)
                for (var j = i; j < num; j++)
                    if (i + j == num) list.Add(new[] { i, j });
            return list;
        }
        private static void ClearEveryThing()
        {
            Memo.Clear();
            MemoCandidates.Clear();
            EvaluationForBinaryTree.AdjacentParts.Clear();
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

    class CandidateComparer : IComparer<Double>
    {
        public int Compare(double x, double y)
        {
            if (x >= y)
                return 1;
            return -1;
        }
    }
}

