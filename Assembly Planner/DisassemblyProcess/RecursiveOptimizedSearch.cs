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
using TVGL;

namespace Assembly_Planner
{

    /// <summary>
    /// Class RecursiveOptimizedSearch - is not currently instantiated (with a constructor). Instead the
    /// global fields are all static and the "Run" function acts to instantiate many of these. In the future,
    /// we may want to make multiple searches. It seems that each "Search" object would need its own Memo.
    /// </summary>
    class RecursiveOptimizedSearch
    {
        protected static EvaluationForBinaryTree assemblyEvaluator;
        /// <summary>
        /// The count is used how many times a particular subassembly with a number of parts is visited
        /// </summary>
        protected static int[] Count = new int[100];
        protected static designGraph Graph;
        protected static List<int> DirPool;
        /// <summary>
        /// The memoization is stored in a Dictionary where the Key is the Hashset of Components
        /// the value is the MemoData class
        /// </summary>
        protected static Dictionary<HashSet<Component>, MemoData> Memo = new Dictionary<HashSet<Component>, MemoData>(HashSet<Component>.CreateSetComparer());
        /// <summary>
        /// The node2int - just used to print the nodenames as integers
        /// </summary>
        protected static Dictionary<string, int> Node2Int = new Dictionary<string, int>();
        /// <summary>
        /// The Estimate flag is used to accelerate the search to get one answer quickly
        /// </summary>
        protected static bool Estimate;

        /// <summary>
        /// Prints the assembly nodes.
        /// </summary>
        /// <param name="Tree">The tree.</param>
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

        /// <summary>
        /// Prints the entire tree - this is a cute recursive function used to provide visual validation 
        /// of the treequence
        /// </summary>
        /// <param name="Tree">The tree.</param>
        /// <param name="Level">The level.</param>
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

        /// <summary>
        /// The function called from the main loop
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <param name="globalDirPool">The global dir pool.</param>
        /// <param name="DoEstimate">if set to <c>true</c> [do estimate].</param>
        /// <returns>SubAssembly.</returns>
        internal static SubAssembly Run(ConvexHullAndBoundingBox inputData, List<int> globalDirPool, bool DoEstimate = false)
        {
            #region Initializing (could be in a future constructor)
            Graph = inputData.graphAssembly;
            DirPool = globalDirPool;
            //Updates.UpdateGlobalDirections(DirPool);
            assemblyEvaluator = new EvaluationForBinaryTree(inputData.ConvexHullDictionary);//inputData.ConvexHullDictionary);
            Estimate = DoEstimate;

            InitializeMemo();
            var index = 0;
            foreach (Component Component in Graph.nodes)
                Node2Int[Component.name] = index++; //for use with PrintTree
            #endregion

            SubAssembly Tree;
            var Best = F(out Tree, new HashSet<Component>(Graph.nodes.Cast<Component>()));

            Console.WriteLine("Best assembly time found: " + Best);
            //for (int i = 1; i <= Graph.nodes.Count; i++)
            //	Console.WriteLine(i+" "+Count[i]);

            PrintTree(Tree);
            return Tree;
        }

        /// <summary>
        /// Technically returns the objective function for the specified tree. Really though,
        /// this is the entire search process where subassemblies are recursively searched to
        /// find their best sequence of assembling. As such, is outputs the Tree as well.
        /// Initially, the hash, A, is the entire set of components in the graph. In order to
        /// simplify the arguments (and reduce the size of the recursive stack) this is used as
        /// a hash - and reference data in the static Graph.
        /// </summary>
        /// <param name="Tree">The tree.</param>
        /// <param name="A">a.</param>
        /// <returns>System.Double.</returns>
        protected static double F(out SubAssembly Tree, HashSet<Component> A)
        {
            // log the number of visits
            Count[A.Count]++;

            // if A has been seen before, simply return the memo data
            if (Memo.ContainsKey(A))
            {
                Tree = Memo[A].sa;
                return Memo[A].Value;
            }

            // this GetCandidates encapsulates ...todo
            var Candidates = GetCandidates(A.ToList());  // note conversion from hash to list (todo: avoid using lists in 
            // sort the candidates by the comparer which is baked into the TreeCandidate class. in this way, the following
            // foreach will look at the children in order of best to worst.
            Candidates.Sort();

            double Best = double.PositiveInfinity;
            SubAssembly Bestsa = null, BestReference = null, BestMoving = null;
            foreach (var TC in Candidates)
            {
                if (TC.H >= Best)
                    break; //any child that has a heuristic worse than the actual cost of a previously explored
                // child is not worth exploring (given the admissability of H).
                SubAssembly Reference, Moving;
                // for a given child, we then recurse down on the two resulting subassemblies.
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
                if (Estimate) //if the Estimate flag is on, we don't let the process check 2nd best, 3rd best children
                    // instead just take the best and return up out of the stack.
                    break;
            }

            Tree = Bestsa;
            Tree.Install.Reference = BestReference;
            Tree.Install.Moving = BestMoving;

            if (!Estimate)
            {   // if the Estimate flag is true, we don't want to store memos, because these memos
                // would be estimates - and not reliably predictions. If the search were restarted,
                // we would like to have reliable memos s.t. we can pick up where we left off.
                MemoData D = new MemoData(Best, Bestsa);
                Memo.Add(A, D);
            }
            return Best;
        }

        /// <summary>
        /// Gets the candidates.
        /// </summary>
        /// <param name="A">All the parts in the subassembly. </param>
        /// <param name="G">The transition cost so far - currently not used - in the future
        /// it could be used in an A* like way. </param>
        /// <returns>List&lt;TreeCandidate&gt;.</returns>
        protected static List<TreeCandidate> GetCandidates(List<Component> A, double G = 0)
        {
            // the Candidates the is list to be returned
            var Candidates = new List<TreeCandidate>();
            var gOptions = new List<option>();
            // foreach direction, find the options
            foreach (var cndDirInd in DirPool)
            {
                // these four lines of code generate the valid options for the given direction, cndDirInd.
                SCCBinary.StronglyConnectedComponents(Graph, A, cndDirInd);
                Dictionary<hyperarc, List<hyperarc>> blockingDic = DBGBinary.DirectionalBlockingGraph(Graph, cndDirInd);
                var options = OptionGeneratorProBinary.GenerateOptions(Graph, A, blockingDic, gOptions);
                gOptions.AddRange(options);

                foreach (var opt in options)
                {
                    // make a TreeCandidate for the option, and evaluate it.
                    TreeCandidate TC = new TreeCandidate();
                    if (assemblyEvaluator.EvaluateSub(A, opt.nodes.Cast<Component>().ToList(), out TC.sa) > 0)
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
            #region make all memos for "prepping" a single part
            foreach (var Component in Graph.nodes)
            {
                List<Component> N = new List<Component>(new Component[] { (Component)Component });
                HashSet<Component> A = new HashSet<Component>(N);
                var sa = new SubAssembly(N, null, 0, 0, null);
                MemoData D = new MemoData(0, sa);
                Memo.Add(A, D);
            }
            #endregion
            #region make all memos for any pair of connected parts
            foreach (Connection arc in Graph.arcs.Where(a => a is Connection))
            {
                // todo: simplify EvaluateSub inputs to ILists s.t. these can be more compact.
                List<Component> Asm = new List<Component>(new Component[] { (Component)arc.From, (Component)arc.To });
                List<Component> Fr = new List<Component>(new Component[] { (Component)arc.From });
                SubAssembly sa;
                if (assemblyEvaluator.EvaluateSub(Asm, Fr, out sa) > 0)
                {  
                    HashSet<Component> A = new HashSet<Component>(Asm);
                    MemoData D = new MemoData(sa.Install.Time, sa);
                    //todo: D = new MemoData(sa.Install.Time + Math.Max(...), sa);
                    // where the ... is the times to prep for the above single part memos.
                    Memo.Add(A, D);
                }
            }
            #endregion

        }

        //Calculate the heuristic value of a given assembly A
        protected static double H(HashSet<Component> A)
        {
            if (A.Count <= 1)
                return 0;

            if (Memo.ContainsKey(A))
                return Memo[A].Value;

            // best case scenario: the subassmebly continues to split in (somewhat) equal parts.
            // this binary notion allows us to figure out the minimum number of steps as log_2(numParts).
            var L = Math.Log(A.Count, 2);
            double MinTreeDepth = Math.Ceiling(L);

            // now that the number of operations is known what is the lower bound estimate for the time
            // the hyperarc is used just to get to the "intra-arcs" between these nodes. then the times from the
            // paired memos for those arcs are extracted....
            var hy = Graph.addHyperArc(A.Cast<node>().ToList());
            List<double> Values = new List<double>();
            foreach (Connection arc in hy.IntraArcs.Where(a => a is Connection))
            {
                HashSet<Component> arcnodes = new HashSet<Component>(new Component[] { (Component)arc.From, (Component)arc.To });
                Values.Add(Memo[arcnodes].Value);
            }
            Graph.removeHyperArc(hy);
            Values.Sort();
            //... then the best MinTreeDepth times are added together.
            double total = 0;
            for (int x = 0; x < MinTreeDepth; x++)
                total = total + Values[x];

            // but all the times are on the treequence - even though they happen concurrently.
            // so it has to be at least as big at the last time. e.g. imagine Values - [1 1 1 1 1 100].
            // with a mintreeDepth of 4. the answer can't just be 4 - it has to be 100.
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


    /// <summary>
    /// Class MemoData - stores memoization information.
    /// </summary>
    class MemoData
    {
        /// <summary>
        /// The value of Subassembly, sa, this is basically the objective function or time 
        /// </summary>
        public double Value;
        public SubAssembly sa;

        public MemoData(double V, SubAssembly S)
        {
            Value = V;
            sa = S;
        }
    }

    //Stores information about a candidate option
    /// <summary>
    /// Class TreeCandidate. 
    /// </summary>
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
