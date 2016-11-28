using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth;
using GraphSynth.Representation;
//using GraphSynth.Search;
using Assembly_Planner.GraphSynth.BaseClasses;
using TVGL;
using Constants = AssemblyEvaluation.Constants;

namespace Assembly_Planner
{
    class RecursiveOptimizedSearch
    {
        protected static EvaluationForBinaryTree AssemblyEvaluator;
        protected static int[] Count = new int[100];
        protected static designGraph Graph;
        protected static List<int> DirPool;

        protected static Dictionary<HashSet<Component>, MemoData> Memo =
            new Dictionary<HashSet<Component>, MemoData>(HashSet<Component>.CreateSetComparer());
        protected static Dictionary<HashSet<Component>, List<TreeCandidate>> MemoCandidates =
            new Dictionary<HashSet<Component>, List<TreeCandidate>>(HashSet<Component>.CreateSetComparer());
        internal static HashSet<HashSet<Component>> FrozenSequence = new HashSet<HashSet<Component>>();
        protected static Dictionary<string, int> Node2Int = new Dictionary<string, int>();
        protected static bool Estimate;
        protected static int TimeEstm = 50;
        protected static int TimeEstmCounter = 0;

        internal static void PrintAssemblyNodes(SubAssembly Tree)
        {
            var R = Tree.Install.Reference.PartNames;
            var M = Tree.Install.Moving.PartNames;
            var A = R.Concat(M);

            Console.Write("[" + Node2Int[A.First()]);
            foreach (var component in A.Skip(1))
                Console.Write("," + Node2Int[component]);
            Console.Write("] (" + A.Count() + ")");
        }

        internal static void PrintTree(SubAssembly Tree, int Level = 0)
        {
            if (Tree.Install == null)
                return;

            Console.Write(String.Concat(Enumerable.Repeat("    ", Level)));
            PrintAssemblyNodes(Tree);
            Console.WriteLine("");

            if (Tree.Install.Reference.PartNames.Count > 1)
                PrintTree((SubAssembly)Tree.Install.Reference, Level + 1);
            if (Tree.Install.Moving.PartNames.Count > 1)
                PrintTree((SubAssembly)Tree.Install.Moving, Level + 1);
        }

        internal static AssemblySequence Run(designGraph graph, Dictionary<string, List<TessellatedSolid>> solids,
            List<int> globalDirPool, bool DoEstimate = false)
        {
            Constants.Values = new Constants();
            //if (Bridge.RestartBoolean) ClearEveryThing();
            Graph = graph;
            AssemblyEvaluator = new EvaluationForBinaryTree(solids);
            DirPool = globalDirPool;
            //Updates.UpdateGlobalDirections(DirPool);
            Estimate = DoEstimate;
            TimeEstmCounter++;
            TimeEstm++;

            SubAssembly tree = null;
            var watch = new Stopwatch();
            watch.Start();
            /*
                        InitializeMemoBoosted();
                        var best = F(out tree, new HashSet<Component>(Graph.nodes.Cast<Component>()));
            */
#if SerialDebug
            InitializeMemo();
            var best = F(out tree, new HashSet<Component>(Graph.nodes.Cast<Component>()));
            
#else
            Task.Factory.StartNew(() => InitializeMemoBoosted());
            Task.Factory.StartNew(() =>
            {
                var best = F(out tree, new HashSet<Component>(Graph.nodes.Cast<Component>()));
            });
            Task.WaitAll();
#endif
            watch.Stop();
            //var index = 0;
            //foreach (var component in Graph.nodes.Cast<Component>())
            //{
            //    Node2Int[component.name] = index++; //for use with PrintTree
            //}
            //redo:


            /*var frozen = new HashSet<Component>();
            foreach (var n in tree.Install.Moving.PartNames)
                frozen.Add((Component) Graph.nodes.First(nod => nod.name == n));
            FrozenSequence.Add(frozen);

            foreach (var seq in FrozenSequence)
            {
                var last = Graph.addHyperArc(seq.Cast<node>().ToList());
                last.localLabels.Add(DisConstants.gSCC);
            }*/
            // when the code is running again with frozen subassemblies, keep the memo of each frozen sub,
            // the ones with and 2 nodeas and and remove the rest.
            //UpdateTheMemo();
            //goto redo;
            //Console.WriteLine("Best assembly time found: " + Best);
            //for (int i = 1; i <= Graph.nodes.Count; i++)
            //	Console.WriteLine(i+" "+Count[i]);
            //PrintTree(Tree);
            return new AssemblySequence { Subassemblies = new List<SubAssembly> { tree } };
            //return Tree;
        }

        protected static double F(out SubAssembly tree, HashSet<Component> A)
        {
            Count[A.Count]++;
            TimeEstmCounter++;
            TimeEstm++;

            if (Memo.ContainsKey(A) /*||
                Memo.Keys.Any(k => k.Count == A.Count && k.All(kk => A.Any(a => a.name == kk.name)))*/)
            {
                tree = Memo[A].sa;
                return Memo[A].Value;
            }

            var candidates = GetCandidates(A);
            candidates.Sort();

            var best = double.PositiveInfinity;
            SubAssembly bestsa = null, bestReference = null, bestMoving = null;
            foreach (var tc in candidates)
            {
                if (tc.H >= best)
                    break;
                SubAssembly reference, moving;
                var refTime = F(out reference, tc.RefNodes);
                var movTime = F(out moving, tc.MovNodes);
                var maxT = Math.Max(refTime, movTime);
                var evaluation = tc.sa.Install.Time + maxT;
                if (evaluation < best)
                {
                    best = evaluation;
                    bestsa = tc.sa;
                    bestReference = reference;
                    bestMoving = moving;
                }
                if (Estimate)
                    break;
            }

            tree = bestsa;
            tree.Install.Reference = bestReference;
            tree.Install.Moving = bestMoving;

            if (!Estimate)
            {
                var d = new MemoData(best, bestsa);
                Memo.Add(A, d);
            }
            return best;
        }

        protected static List<TreeCandidate> GetCandidates(HashSet<Component> A, double G = 0)
        {
            var gOptions = new Dictionary<option, HashSet<int>>();
            var candidates = new List<TreeCandidate>();
            if (FrozenSequence.Count > 0)
            {
                // each of these frozen sequences must be considered as a subassembly
                //HashSet<Component> subAssem;
                if (MemoCandidates.ContainsKey(A))
                {
                    // If the memoOption has the key, then the options will be chosen which the components 
                    //    of the frozen subassemblies are either all in the option.nodes or non of them are
                    //    in the option.nodes (meaning that it's in the rest)
                    candidates.AddRange(MemoCandidates[A].Where(SubAssemsAreInOptNodesOrRestNodes));
                    return candidates;
                }
            }
            GenerateOptions(A, gOptions);
            //MemoOptions.Add(A, gOptions);

            foreach (var opt in gOptions.Keys)
            {
                var TC = new TreeCandidate();
                if (AssemblyEvaluator.EvaluateSub(Graph, A, opt.Nodes.Cast<Component>().ToList(), gOptions[opt], out TC.sa) > 0)
                {
                    TC.RefNodes = new HashSet<Component>(TC.sa.Install.Reference.PartNames.Select(n => (Component)Graph[n]));
                    TC.MovNodes = new HashSet<Component>(TC.sa.Install.Moving.PartNames.Select(n => (Component)Graph[n]));
                    //if (Math.Min (TC.RefNodes.Count, TC.MovNodes.Count) > 21)	//example constraint
                    //	continue;

                    var HR = H(TC.RefNodes);
                    var HM = H(TC.MovNodes);
                    TC.G = G;
                    TC.H = TC.sa.Install.Time + Math.Max(HR, HM);
                    candidates.Add(TC);
                }
            }
            MemoCandidates.Add(A, candidates);
            return candidates;
        }

        private static bool MemoOptionHasTheSubAssem(HashSet<Component> A, out HashSet<Component> subAssem)
        {
            subAssem = null;
            foreach (var key in MemoCandidates.Keys)
            {
                if (key.Count != A.Count) continue;
                if (A.All(a => key.Any(k => k.name == a.name)) && key.All(k => A.Any(a => a.name == k.name)))
                {
                    subAssem = key;
                    return true;
                }
            }
            return false;
        }

        private static bool SubAssemsAreInOptNodesOrRestNodes(TreeCandidate option)
        {
            foreach (var seq in FrozenSequence)
            {
                if (seq.All(option.MovNodes.Contains) || seq.All(option.RefNodes.Contains))
                    return true;
                if (!seq.Any(option.MovNodes.Contains) && !seq.Any(option.RefNodes.Contains))
                    return true;
            }
            return false;
        }

        private static void GenerateOptions(HashSet<Component> A, Dictionary<option, HashSet<int>> gOptions)
        {
            foreach (var cndDirInd in DirPool)
            {
                SCCBinary.StronglyConnectedComponents(Graph, A, cndDirInd);
                var blockingDic = DBGBinary.DirectionalBlockingGraph(Graph, cndDirInd);
                var options = OptionGeneratorProBinary.GenerateOptions(Graph, A, blockingDic, gOptions, cndDirInd);
                foreach (var opt in options)
                    gOptions.Add(opt, new HashSet<int> { cndDirInd });
            }
        }

        //initialize memoization with 2-Component (i.e., arc) subassemblies so heuristic works
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
                    Memo.Add(A, D);
                }
            }

            foreach (var node in Graph.nodes)
            {
                var component = (Component)node;
                var N = new HashSet<Component>(new[] { component });
                var sa = new SubAssembly(N, EvaluationForBinaryTree.ConvexHullsForParts[component.name], component.Mass,
                    component.Volume, new Vertex(component.CenterOfMass));
                MemoData D = new MemoData(0, sa);
                Memo.Add(N, D);
            }
        }

        //Calculate the heuristic value of a given assembly A
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
                HashSet<Component> arcnodes = new HashSet<Component>(new Component[] { (Component)arc.From, (Component)arc.To });
                Values.Add(Memo[arcnodes].Value);
            }
            Graph.removeHyperArc(hy);
            Values.Sort();

            double total = 0;
            for (int x = 0; x < MinTreeDepth; x++)
                total = total + Values[x];

            return Math.Max(total, Values.Last());
            /*
                        var intraArcs = new List<arc>();
                        foreach (var Component in subassemblyNodes)
                        {
                            foreach (arc arc in Component.arcs)
                            {
                                if (Component == arc.From)
                                {
                                    if (subassemblyNodes.Contains(arc.To))
                                        intraArcs.Add(arc);
                                }
                                else
                                    if (subassemblyNodes.Contains(arc.From))
                                        intraArcs.Add(arc);
                            }
                        }
            */
        }

        private static void UpdateTheMemo()
        {
            for (var i = 0; i < Memo.Count; i++)
            {
                var memo = Memo.Keys.ToList()[i];
                if (memo.Count == 1 || memo.Count == 2)
                    continue;
                if (!FrozenSequence.Any(fS => (memo.Count == fS.Count) && memo.All(c => fS.Any(fs => fs.name == c.name))))
                {
                    Memo.Remove(memo);
                    i--;
                }
            }
        }

        protected static void InitializeMemoBoosted()
        {
            var newMemo = new List<HashSet<HashSet<Component>>>();
            for (var i = 1; i <= Graph.nodes.Count; i++)
            {
                var combinations = CombinationFinder(i);
                var column = new HashSet<HashSet<Component>>();
                if (!combinations.Any())
                {
                    foreach (var node in Graph.nodes)
                    {
                        var component = (Component)node;
                        var N = new HashSet<Component>(new[] { component });
                        var sa = new SubAssembly(N, EvaluationForBinaryTree.ConvexHullsForParts[component.name],
                            component.Mass,
                            component.Volume, new Vertex(component.CenterOfMass));
                        var D = new MemoData(0, sa);
                        column.Add(N);
                        Memo.Add(N, D);
                    }
                    newMemo.Add(column);
                }
                else
                {
                    Parallel.ForEach(combinations, comb =>
                    {
                        foreach (var subAssem1 in newMemo[comb[0] - 1])
                        {
                            foreach (var subAssem2 in newMemo[comb[1] - 1])
                            {
                                if (subAssem1.Any(subAssem2.Contains)) continue;
                                var connections =
                                    new HashSet<Connection>(
                                        subAssem1.SelectMany(
                                            n => n.arcs.Where(c => c is Connection).Cast<Connection>().Where(c =>
                                                (subAssem1.Contains(c.From) && subAssem2.Contains(c.To)) ||
                                                (subAssem1.Contains(c.To) && subAssem2.Contains(c.From)))));
                                var secConnections =
                                    new HashSet<SecondaryConnection>(
                                        subAssem1.SelectMany(
                                            n =>
                                                n.arcs.Where(a => a is SecondaryConnection)
                                                    .Cast<SecondaryConnection>()
                                                    .Where(c =>
                                                        (subAssem1.Contains(c.From) && subAssem2.Contains(c.To)) ||
                                                        (subAssem1.Contains(c.To) && subAssem2.Contains(c.From)))));

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
                    });
                    newMemo.Add(column);
                    if (newMemo.Count == 3) break;
                }
            }
        }

        private static void ApplySecondaryConnections(HashSet<int> dirs, HashSet<Component> subAssem1,
            HashSet<Component> subAssem2, HashSet<SecondaryConnection> secConnections)
        {
            // subAssem1 is moving
            foreach (var seConn in secConnections)
            {
                if (subAssem1.Contains(seConn.From))
                    dirs.RemoveWhere(seConn.Directions.Contains);
                else
                {
                    var opposities =
                        seConn.Directions.Select(d => DisassemblyDirections.DirectionsAndOpposits[d]);
                    dirs.RemoveWhere(opposities.Contains);
                }
            }
        }

        private static HashSet<int> ValidDirectionsFinder(HashSet<Component> subAssem1, HashSet<Component> subAssem2,
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
                        connection.InfiniteDirections.Select(inD => DisassemblyDirections.DirectionsAndOpposits[inD]);
                    union.UnionWith(opposities.Where(d => !union.Contains(d)));
                }
            }
            return union;
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
            FrozenSequence.Clear();
        }

    }

    //stores memoization information
    class MemoData
    {
        public double Value;
        public SubAssembly sa;

        public MemoData(double V, SubAssembly S)
        {
            Value = V;
            sa = S;
        }
    }

    //Stores information about a candidate option
    class TreeCandidate : IComparable<TreeCandidate>
    {
        public TreeCandidate parent;
        public SubAssembly sa;
        public HashSet<Component> RefNodes, MovNodes;
        public double G, H; //G-score and heuristic value
        public Dictionary<int, HashSet<HashSet<Component>>> Options; 
        public int CompareTo(TreeCandidate other)
        {
            var F = G + H;
            var otherF = other.G + other.H;
            if (F != otherF)
                return F.CompareTo(otherF);     //first try to sort on heuristic values

            var MaxNodes = Math.Max(RefNodes.Count, MovNodes.Count);
            var OtherMaxNodes = Math.Max(other.RefNodes.Count, other.MovNodes.Count);
            return MaxNodes.CompareTo(OtherMaxNodes); //if they are even, try to split parts evenly
        }

        internal TreeCandidate()
        {
        }
        internal TreeCandidate(TreeCandidate tc)
        {
            parent = tc.parent;
            sa = tc.sa;
            RefNodes = tc.RefNodes;
            MovNodes = tc.MovNodes;
            G = tc.G;
            H = tc.H;
        }
    }
}
