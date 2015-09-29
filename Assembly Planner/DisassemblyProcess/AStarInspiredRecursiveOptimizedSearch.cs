using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth.Representation;

namespace Assembly_Planner.DisassemblyProcess
{
    class AStarInspiredRecursiveOptimizedSearch
    {
        protected static EvaluationForBinaryTree assemblyEvaluator;
        protected static int[] Count = new int[100];
        protected static designGraph Graph;
        protected static List<int> DirPool;
        protected static Dictionary<HashSet<node>, MemoData> Memo = new Dictionary<HashSet<node>, MemoData>(HashSet<node>.CreateSetComparer());
        //protected static MinHeap<TreeCandidate> OpenSet;
        //protected static Dictionary<HashSet<node>, TreeCandidate> TreeCandidates = new Dictionary<HashSet<node>, TreeCandidate>(HashSet<node>.CreateSetComparer());
        protected static Dictionary<HashSet<node>, Dictionary<HashSet<node>, TreeCandidate>> TreeCandidates =
            new Dictionary<HashSet<node>,
            Dictionary<HashSet<node>, TreeCandidate>>
            (HashSet<node>.CreateSetComparer());
        protected static SortedDictionary<TreeCandidate, int> OpenSet = new SortedDictionary<TreeCandidate, int>();//new sortTreeCandidates());
        protected static Dictionary<HashSet<node>, MemoData> Guesses = new Dictionary<HashSet<node>, MemoData>(HashSet<node>.CreateSetComparer());
        protected static Dictionary<string, int> Node2Int = new Dictionary<string, int>();
        protected static bool Estimate;

        internal static SubAssembly RunAnytime(ConvexHullAndBoundingBox inputData, List<int> globalDirPool)
        {
            Graph = inputData.graphAssembly;
            DirPool = globalDirPool;
            Updates.UpdateGlobalDirections(DirPool);
            assemblyEvaluator = new EvaluationForBinaryTree();//inputData.ConvexHullDictionary);

            InitializeMemo();

            var index = 0;
            foreach (var node in Graph.nodes)
                Node2Int[node.name] = index++;
            /*
                        var minHeap = new MinHeap<int>(new[] {9, 8, 4, 1, 6, 2, 7, 4, 1, 2});
                        Console.WriteLine("heap size: " + minHeap.Count());
                        Console.WriteLine("min: " + minHeap.GetMin());
                        Console.WriteLine("min " + minHeap.ExtractDominating());
                        Console.WriteLine("heap size: " + minHeap.Count());
            */
            var Candidates = GetCandidates(Graph.nodes, 0);
            //OpenSet = new MinHeap<TreeCandidate>(Candidates);
            AddToOpenSet(Candidates);
            /*
                        var A = new HashSet<node>(Graph.nodes);

                        TreeCandidate Best = OpenSet.GetMin();
                        MemoData D = new MemoData(Best.H, Best.sa);
                        D.Options = Candidates;
                        Guesses[A] = D;
            /*
                        foreach(var TC in Candidates)
                        {
                            MemoData D = new MemoData(TC.H, TC.sa);
                            Guesses[] = D;
                        }

                        SubAssembly sa;
                        assemblyEvaluator.EvaluateSub(Graph.nodes, Candidates[0].RefNodes.ToList(), out sa);
                        Guesses[A] = new MemoData(H(A), sa);
            */
            //PrintOpenSet ();

            while (OpenSet.Count() > 0)
            {
                var current = OpenSet.Keys.First();
                //TreeCandidate current = TreeCandidates[A];

                //TreeCandidate current = OpenSet.First();
                Console.Write("Considering: ");
                PrintSet(current.RefNodes);
                Console.Write(" and ");
                PrintSet(current.MovNodes);
                Console.WriteLine(" with G: " + current.G + " and H: " + current.H + ".");

                var A = new HashSet<node>(current.RefNodes.Union(current.MovNodes));
                MemoData D;
                if (Memo.ContainsKey(A))
                {
                    D = Memo[A];
                    if (current.H >= D.Value)
                    {
                        OpenSet.Remove(current);
                        //OpenSet.ExtractDominating();
                        continue;
                    }
                }
                else
                    D = new MemoData(double.PositiveInfinity, current.sa);

                double G = current.G + current.sa.Install.Time;

                SubAssembly Tree1, Tree2;
                var V1 = GetValue(out Tree1, current, current.RefNodes, G);

                if (V1 >= 0)
                {
                    var V2 = GetValue(out Tree2, current, current.MovNodes, G);

                    if (V2 >= 0)
                    {
                        var V = Math.Max(V1, V2) + current.sa.Install.Time;
                        if (V < D.Value)
                        {
                            D.Value = V;
                            D.sa.Install.Reference = Tree1;
                            D.sa.Install.Moving = Tree2;
                            Memo[A] = D;
                            Guesses.Remove(A);
                            Console.Write("Obtained new value of " + V + " for ");
                            PrintSet(A);
                            Console.WriteLine(".");
                            UpdateParents(current);
                            //PrintTree(D.sa);
                        }
                        OpenSet.Remove(current);
                        //OpenSet.ExtractDominating();
                    }
                }
            }

            SubAssembly Tree = Memo[new HashSet<node>(Graph.nodes)].sa;
            PrintTree(Tree);
            return Tree;
        }


        //This takes O(n log n), but this is acceptable given the indexing capabilities it gives us compared to using a heap
        internal static void AddToOpenSet(IEnumerable<TreeCandidate> Elements)
        {
            foreach (var TC in Elements)
            {
                AddToOpenSet(TC);
            }
        }

        internal static void AddToOpenSet(TreeCandidate TC)
        {
            OpenSet.Add(TC, 0);
            if (TreeCandidates.ContainsKey(TC.RefNodes))
            {
                if (TreeCandidates[TC.RefNodes].ContainsKey(TC.MovNodes))
                    return;
            }
            else
                TreeCandidates[TC.RefNodes] = new Dictionary<HashSet<node>, TreeCandidate>(HashSet<node>.CreateSetComparer());

            TreeCandidates[TC.RefNodes][TC.MovNodes] = TC;
        }

        internal static void UpdateParents(TreeCandidate TC)
        {
            foreach (var Parent in TC.Parents)
            {
                /*
				double Min = double.PositiveInfinity;
				foreach (var Option in Parent.Options)
					Min = Math.Min(Min, Option.G + Option.H);
				var NewValue = Min + Parent.sa.Install.Time;
				if(NewValue > Parent.Value)
				{
					Parent.Value = NewValue;
					UpdateParents(Parent);
				}
				*/
            }
        }

        internal static double GetValue(out SubAssembly Tree, TreeCandidate Parent, HashSet<node> A, double G)
        {
            //			if (A.Count <= 1) {
            //				Tree = null;
            //				return 0;
            //			}

            if (Memo.ContainsKey(A))
            {
                Tree = Memo[A].sa;
                return Memo[A].Value;
            }

            //			if (ClosedSet.ContainsKey (A)) {
            //				Tree =  ClosedSet[A].sa;
            //				return ClosedSet[A].Value;
            //			}

            //ClosedSet [A] = D;
            var Candidates = GetCandidates(A.ToList(), G);
            //double BestF = double.PositiveInfinity;
            //TreeCandidate Best = null;
            foreach (var TC in Candidates)
            {
                TC.Parents.Add(Parent);
                AddToOpenSet(TC);
                //TreeCandidates[A] = TC;
                //OpenSet.Add(A, 0);
                /*
				if(BestF > TC.G + TC.H) {
					BestF = TC.G + TC.H;
					Best = TC;
				}
*/
                Console.Write("Added: ");
                PrintSet(TC.RefNodes);
                Console.Write(" and ");
                PrintSet(TC.MovNodes);
                Console.WriteLine(" with G: " + TC.G + " and H: " + TC.H + ".");
            }
            PrintOpenSet();
            /*
                        if(Guesses.ContainsKey(A)) {
                            Guesses[A].Parents.Add(Parent);

                            Console.Write("Adding parent: ");
                            PrintAssemblyNodes(Parent.sa);
                            Console.Write(" to guess: ");
                            PrintSet(A);
                            Console.WriteLine(".");
                        } else {
                            MemoData D = new MemoData(Best.G + Best.H, Best.sa);
                            D.Parents.Add(Parent);
                            D.Options = Candidates;
                            Guesses[A] = D;

                            Console.Write("Adding guess: ");
                            PrintSet(A);
                            Console.Write(" with value: "+D.Value+" ("+Best.G+"+"+Best.H+") and parent: ");
                            PrintAssemblyNodes(Parent.sa);
                            Console.WriteLine(".");
                        }
            */
            Tree = null;
            return -1;
        }

        internal static void PrintOpenSet(int Max = 10)
        {
            int Count = 0;
            foreach (var TC in OpenSet.Keys)
            {
                //TreeCandidate TC = TreeCandidates[A];
                Console.Write("T: " + TC.sa.Install.Time + " R: ");
                PrintSet(TC.RefNodes);
                Console.Write(" M: ");
                PrintSet(TC.MovNodes);
                Console.WriteLine(" G: " + TC.G + " H: " + TC.H);
                Count++;
                if (Count >= Max)
                    break;
            }
            Console.WriteLine("");
        }

        internal static void PrintAssemblyNodes(SubAssembly Tree)
        {
            var R = Tree.Install.Reference.PartNodes;
            var M = Tree.Install.Moving.PartNodes;
            var A = R.Concat(M);

            Console.Write("[" + Node2Int[A.First()]);
            foreach (var node in A.Skip(1))
                Console.Write("," + Node2Int[node]);
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

        internal static void PrintSet(IEnumerable<node> A)
        {
            Console.Write("[" + Node2Int[A.First().name]);
            foreach (var node in A.Skip(1))
                Console.Write("," + Node2Int[node.name]);
            Console.Write("] (" + A.Count() + ")");
        }

        internal static SubAssembly Run(ConvexHullAndBoundingBox inputData, List<int> globalDirPool, bool DoEstimate = false)
        { // same as REcursiveOptimizedSearch
            Graph = inputData.graphAssembly;
            DirPool = globalDirPool;
            Updates.UpdateGlobalDirections(DirPool);
            assemblyEvaluator = new EvaluationForBinaryTree();//inputData.ConvexHullDictionary);
            Estimate = DoEstimate;

            InitializeMemo();

            var index = 0;
            foreach (var node in Graph.nodes)
                Node2Int[node.name] = index++;

            SubAssembly Tree;
            var Best = F(out Tree, new HashSet<node>(Graph.nodes));

            Console.WriteLine("Best assembly time found: " + Best);
            //for (int i = 1; i <= Graph.nodes.Count; i++)
            //	Console.WriteLine(i+" "+Count[i]);

            PrintTree(Tree);
            return Tree;
        }

        protected static double F(out SubAssembly Tree, HashSet<node> A)
        // same as RecursiveOptimizedSearch.cs 
        {
            Count[A.Count]++;

            //if (A.Count <= 1) {
            //	Tree = null;
            //	return 0;
            //}

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

            MemoData D = new MemoData(Best, Bestsa);
            Memo.Add(A, D);

            return Best;
        }

        protected static List<TreeCandidate> GetCandidates(List<node> A, double G = 0)
        // same as RecursiveOptimizedSearch.cs 
        {
            var Candidates = new List<TreeCandidate>();
            foreach (var cndDirInd in DirPool)
            {
                SCCBinary.StronglyConnectedComponents(Graph, A, cndDirInd);
                var blockingDic = DBGBinary.DirectionalBlockingGraph(Graph, A, cndDirInd);
                //options.AddRange(OptionGeneratorProBinary.GenerateOptions(Graph, A, blockingDic));
                var options = OptionGeneratorProBinary.GenerateOptions(Graph, A, blockingDic);

                foreach (var opt in options)
                {
                    TreeCandidate TC = new TreeCandidate();
                    if (assemblyEvaluator.EvaluateSub(A, opt.nodes, out TC.sa) > 0)
                    {
                        //TC.RefNodes = TC.sa.Install.Reference.PartNodes.Select (n => (node)Graph [n]).ToList ();
                        //TC.MovNodes = TC.sa.Install.Moving.PartNodes.Select (n => (node)Graph [n]).ToList ();
                        TC.RefNodes = new HashSet<node>(TC.sa.Install.Reference.PartNodes.Select(n => (node)Graph[n]));
                        TC.MovNodes = new HashSet<node>(TC.sa.Install.Moving.PartNodes.Select(n => (node)Graph[n]));
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

        //initialize memoization with 2-node (i.e., arc) subassemblies so heuristic works
        protected static void InitializeMemo()
        // same as RecursiveOptimizedSearch.cs 
        {
            foreach (arc arc in Graph.arcs)
            {
                List<node> Asm = new List<node>(new node[] { arc.From, arc.To });
                List<node> Fr = new List<node>(new node[] { arc.From });
                SubAssembly sa;
                if (assemblyEvaluator.EvaluateSub(Asm, Fr, out sa) > 0)
                {
                    HashSet<node> A = new HashSet<node>(Asm);
                    MemoData D = new MemoData(sa.Install.Time, sa);
                    Memo.Add(A, D);
                }
            }

            foreach (var node in Graph.nodes)
            {
                List<node> N = new List<node>(new node[] { node });
                HashSet<node> A = new HashSet<node>(N);
                //var P = new Part(node.name, 0, 0,null, null);
                var sa = new SubAssembly(N, null, 0, 0, null);
                MemoData D = new MemoData(0, sa);
                Memo.Add(A, D);
            }
        }

        //Calculate the heuristic value of a given assembly A
        protected static double H(HashSet<node> A)
        // same as RecursiveOptimizedSearch.cs 
        {
            if (A.Count <= 1)
                return 0;

            if (Memo.ContainsKey(A))
                return Memo[A].Value;

            var L = Math.Log(A.Count, 2);
            double MinTreeDepth = Math.Ceiling(L);
            Graph.addHyperArc(A.ToList());
            var hy = Graph.hyperarcs[Graph.hyperarcs.Count - 1];
            List<double> Values = new List<double>();
            foreach (arc arc in hy.IntraArcs)
            {
                HashSet<node> arcnodes = new HashSet<node>(new node[] { arc.From, arc.To });
                Values.Add(Memo[arcnodes].Value);
            }
            Graph.removeHyperArc(hy);
            Values.Sort();

            double total = 0;
            for (int x = 0; x < MinTreeDepth; x++)
                total = total + Values[x];

            return total;
            /*
                        var intraArcs = new List<arc>();
                        foreach (var node in subassemblyNodes)
                        {
                            foreach (arc arc in node.arcs)
                            {
                                if (node == arc.From)
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
    // same as RecursiveOptimizedSearch.cs except for Parents and Children comment
    class MemoData
    {
        public double Value;
        public SubAssembly sa;

        public MemoData(double V, SubAssembly S)
        {
            Value = V;
            sa = S;
            //Parents = new List<MemoData>();
            //Children = new List<MemoData>();
        }
    }

    //Stores information about a candidate option
    class TreeCandidate : IComparable<TreeCandidate>
    {
        public SubAssembly sa;
        public HashSet<node> RefNodes, MovNodes;
        public double G, H; //G-score and heuristic value
        public List<TreeCandidate> Parents = new List<TreeCandidate>();
        public List<TreeCandidate> Options = new List<TreeCandidate>();

        public int CompareTo(TreeCandidate other)
        {
            var F = G + H;
            var otherF = other.G + other.H;
            if (F != otherF)
                return F.CompareTo(otherF);     //first try to sort on heuristic values

            var MaxNodes = Math.Max(RefNodes.Count, MovNodes.Count);
            var OtherMaxNodes = Math.Max(other.RefNodes.Count, other.MovNodes.Count);
            if (MaxNodes != OtherMaxNodes)
                return MaxNodes.CompareTo(OtherMaxNodes); //if they are even, try to split parts evenly

            if (RefNodes != other.RefNodes)
                return RefNodes.GetHashCode().CompareTo(other.RefNodes.GetHashCode());

            return MovNodes.GetHashCode().CompareTo(other.MovNodes.GetHashCode());
        }
    }
    /*
	class sortTreeCandidates:IComparer<TreeCandidate>
	{
		//public Dictionary<HashSet<node>, TreeCandidate> TreeCandidates;

		public sortTreeCandidates(Dictionary<HashSet<node>, TreeCandidate> TCs)
		{
			TreeCandidates = TCs;
		}

		public int Compare(TreeCandidate a, TreeCandidate b)
		{
			return a.CompareTo(TreeCandidates[b]);
		}
	}
*/
}

}
