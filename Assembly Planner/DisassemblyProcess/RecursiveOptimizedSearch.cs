using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth;
using GraphSynth.Representation;
using GraphSynth.Search;
using Assembly_Planner.GraphSynth.BaseClasses;

namespace Assembly_Planner
{
    class RecursiveOptimizedSearch
    {
        protected static EvaluationForBinaryTree assemblyEvaluator;
        protected static int[] Count = new int[100];
        protected static designGraph Graph;
        protected static List<int> DirPool;
        protected static Dictionary<HashSet<Component>, MemoData> Memo = new Dictionary<HashSet<Component>, MemoData>(HashSet<Component>.CreateSetComparer());
        protected static Dictionary<string, int> Node2Int = new Dictionary<string, int>();
        protected static bool Estimate;

        internal static void PrintAssemblyNodes(SubAssembly Tree)
        {
            var R = Tree.Install.Reference.PartNodes;
            var M = Tree.Install.Moving.PartNodes;
            var A = R.Concat(M);

            Console.Write("[" + Node2Int[A.First()]);
            foreach (var Component in A.Skip(1))
                Console.Write("," + Node2Int[Component]);
            Console.Write("] (" + A.Count() + ")");
        }

        internal static void PrintTree(SubAssembly Tree, int Level = 0)
        {
            if (Tree.Install == null)
                return;

            Console.Write(String.Concat(Enumerable.Repeat("    ", Level)));
            PrintAssemblyNodes(Tree);
            Console.WriteLine("");

            if (Tree.Install.Reference.PartNodes.Count > 1)
                PrintTree((SubAssembly)Tree.Install.Reference, Level + 1);
            if (Tree.Install.Moving.PartNodes.Count > 1)
                PrintTree((SubAssembly)Tree.Install.Moving, Level + 1);
        }

        internal static SubAssembly Run(ConvexHullAndBoundingBox inputData, List<int> globalDirPool, bool DoEstimate = false)
        {
            Graph = inputData.graphAssembly;
            DirPool = globalDirPool;
            //Updates.UpdateGlobalDirections(DirPool);
            assemblyEvaluator = new EvaluationForBinaryTree(inputData.ConvexHullDictionary);
            AssemblyEvaluator.convexHullForPartsA = assemblyEvaluator.convexHullForParts;
            Estimate = DoEstimate;

            InitializeMemo();

            var index = 0;
            foreach (Component Component in Graph.nodes)
                Node2Int[Component.name] = index++; //for use with PrintTree

            SubAssembly Tree;
            var Best = F(out Tree, new HashSet<Component>(Graph.nodes.Cast<Component>()));

            Console.WriteLine("Best assembly time found: " + Best);
            //for (int i = 1; i <= Graph.nodes.Count; i++)
            //	Console.WriteLine(i+" "+Count[i]);

            PrintTree(Tree);
            return Tree;
        }

        protected static double F(out SubAssembly Tree, HashSet<Component> A)
        {
            Count[A.Count]++;

            if (Memo.ContainsKey(A))
            {
                Tree = Memo[A].sa;
                return Memo[A].Value;
            }

            var Candidates = GetCandidates(A.ToList());
            Candidates.Sort();

            double Best = double.PositiveInfinity;
            SubAssembly Bestsa = null, BestReference = null, BestMoving = null;
            foreach (var TC in Candidates)
            {
                if (TC.H >= Best)
                    break;
                SubAssembly Reference, Moving;
                double RefTime = F(out Reference, TC.RefNodes);
                double MovTime = F(out Moving, TC.MovNodes);
                double MaxT = Math.Max(RefTime, MovTime);
                double Evaluation = TC.sa.Install.Time + MaxT;
                if (Evaluation < Best)
                {
                    Best = Evaluation;
                    Bestsa = TC.sa;
                    BestReference = Reference;
                    BestMoving = Moving;
                }
                if (Estimate)
                    break;
            }

            Tree = Bestsa;
            Tree.Install.Reference = BestReference;
            Tree.Install.Moving = BestMoving;

            if (!Estimate)
            {
                MemoData D = new MemoData(Best, Bestsa);
                Memo.Add(A, D);
            }
            return Best;
        }

        protected static List<TreeCandidate> GetCandidates(List<Component> A, double G = 0)
        {
            var Candidates = new List<TreeCandidate>();
            var gOptions = new List<option>();
            foreach (var cndDirInd in DirPool)
            {
                SCCBinary.StronglyConnectedComponents(Graph, A, cndDirInd);
                var blockingDic = DBGBinary.DirectionalBlockingGraph(Graph, cndDirInd);
                var options = OptionGeneratorProBinary.GenerateOptions(Graph, A, blockingDic, gOptions);
                gOptions.AddRange(options);

                foreach (var opt in options)
                {
                    TreeCandidate TC = new TreeCandidate();
                    if (assemblyEvaluator.EvaluateSub(Graph, A, opt.nodes.Cast<Component>().ToList(), out TC.sa) > 0)
                    {
                        TC.RefNodes = new HashSet<Component>(TC.sa.Install.Reference.PartNodes.Select(n => (Component)Graph[n]));
                        TC.MovNodes = new HashSet<Component>(TC.sa.Install.Moving.PartNodes.Select(n => (Component)Graph[n]));
                        //if (Math.Min (TC.RefNodes.Count, TC.MovNodes.Count) > 21)	//example constraint
                        //	continue;

                        double HR = H(TC.RefNodes);
                        double HM = H(TC.MovNodes);
                        TC.G = G;
                        TC.H = TC.sa.Install.Time + Math.Max(HR, HM);
                        Candidates.Add(TC);
                    }
                }
            }
            return Candidates;
        }

        //initialize memoization with 2-Component (i.e., arc) subassemblies so heuristic works
        protected static void InitializeMemo()
        {
            foreach (Connection arc in Graph.arcs.Where(a=> a is Connection))
            {
                List<Component> Asm = new List<Component>(new Component[] { (Component)arc.From, (Component)arc.To });
                List<Component> Fr = new List<Component>(new Component[] { (Component)arc.From });
                SubAssembly sa;
                if (assemblyEvaluator.EvaluateSub(Graph,Asm, Fr, out sa) > 0)
                {
                    HashSet<Component> A = new HashSet<Component>(Asm);
                    MemoData D = new MemoData(sa.Install.Time, sa);
                    Memo.Add(A, D);
                }
            }

            foreach (var Component in Graph.nodes)
            {
                List<Component> N = new List<Component>(new Component[] { (Component)Component });
                HashSet<Component> A = new HashSet<Component>(N);
                var sa = new SubAssembly(N, null, 0, 0, null);
                MemoData D = new MemoData(0, sa);
                Memo.Add(A, D);
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
            foreach (Connection arc in hy.IntraArcs.Where(a=> a is Connection))
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
        public SubAssembly sa;
        public HashSet<Component> RefNodes, MovNodes;
        public double G, H; //G-score and heuristic value

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
    }
}
