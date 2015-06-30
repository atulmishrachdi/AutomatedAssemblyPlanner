﻿using System;
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
    class RecursiveOptimizedSearch
    {
        protected static EvaluationForBinaryTree assemblyEvaluator;
		protected static int[] Count = new int[21];
		protected static designGraph Graph;
		protected static List<int> DirPool;
		protected static Dictionary<HashSet<node>, double> Memo = new Dictionary<HashSet<node>, double>(HashSet<node>.CreateSetComparer());
		//var myDictionary = new Dictionary<HashSet<T>, TValue>(HashSet<T>.CreateSetComparer());

        internal static List<AssemblyCandidate> Run(ConvexHullAndBoundingBox inputData, List<int> globalDirPool)
        {
			Graph = inputData.graphAssembly;
			DirPool = globalDirPool;
			Updates.UpdateGlobalDirections(DirPool);

            var solutions = new List<AssemblyCandidate>();
			assemblyEvaluator = new EvaluationForBinaryTree();//inputData.ConvexHullDictionary);

			foreach (arc arc in Graph.arcs)
			{
				List<node> Asm = new List<node>(new node[] {arc.From,arc.To});
				List<node> Fr = new List<node>(new node[] {arc.From});
				SubAssembly sa;
				if (assemblyEvaluator.EvaluateSub (Asm, Fr, out sa)>0)
				{
					HashSet<node> A = new HashSet<node>(new node[] {arc.From,arc.To});
					Memo.Add(A, sa.Install.Time);
				}
			}
			/*
			foreach( KeyValuePair<HashSet<node>, double> kvp in Memo )
			{
				Console.Write("Key = ");
				foreach (node i in kvp.Key)
				{
					Console.Write(" {0}", i.name);
				}
				Console.WriteLine(", Value = {0}", kvp.Value);
			}
*/
			SubAssembly Tree;
			var Best = F(out Tree, Graph.nodes);

			Console.WriteLine("Best assembly time is: " + Best);
			for (int i = 1; i <= 20; i++)
			{
				Console.WriteLine(i+" "+Count[i]);
			}

			AssemblyCandidate goal = null;
			goal.Sequence.Subassemblies.Add(Tree);
            solutions.Add(goal);
            return solutions;
        }

		protected static double F(out SubAssembly Tree, List<node> A)
		{
			if (A.Count <= 1) {
				Count[1]++;
				Tree = null;
				return 0;
			}

			//var options = new List<option>();
			var Candidates = new List<TreeCandidate> ();
			foreach (var cndDirInd in DirPool)
			{
                SCCBinary.StronglyConnectedComponents(Graph, A, cndDirInd);
                var blockingDic = DBGBinary.DirectionalBlockingGraph(Graph, A, cndDirInd);
                //options.AddRange(OptionGeneratorProBinary.GenerateOptions(Graph, A, blockingDic));
				var options = OptionGeneratorProBinary.GenerateOptions (Graph, A, blockingDic);

				foreach (var opt in options)
				{
					TreeCandidate TC = new TreeCandidate();
					if (assemblyEvaluator.EvaluateSub (A, opt.nodes, out TC.sa) > 0)
					{
						TC.RefNodes = TC.sa.Install.Reference.PartNodes.Select (n => (node)Graph [n]).ToList ();
						TC.MovNodes = TC.sa.Install.Moving.PartNodes.Select (n => (node)Graph [n]).ToList ();
						double HR = H(TC.RefNodes);
						double HM = H(TC.MovNodes);
						TC.H = TC.sa.Install.Time + Math.Max(HR,HM);
						Candidates.Add(TC);
					}
				}
			}
			Candidates.Sort();

			double Best = 999999;
			SubAssembly Bestsa = null, BestReference = null, BestMoving = null;
			foreach (var TC in Candidates)
			{
				if (TC.H > Best)
					break;
				SubAssembly Reference, Moving;
				double RefTime = F(out Reference, TC.RefNodes);
				double MovTime = F(out Moving,    TC.MovNodes);
				double MaxT = Math.Max (RefTime, MovTime);
				double Evaluation = TC.sa.Install.Time + MaxT;
				if (Evaluation < Best)
				{
					Best = Evaluation;
					Bestsa = TC.sa;
					BestReference = Reference;
					BestMoving = Moving;
				}
			}

			Tree = Bestsa;
			Tree.Install.Reference = BestReference;
			Tree.Install.Moving = BestMoving;

			Count[A.Count]++;
			return Best;
		}

		//Calculate the heuristic value of a given assembly A
		protected static double H(List<node> A)
		{
			if (A.Count <= 1)
				return 0;

			var L = Math.Log (A.Count, 2);
			double MinTreeDepth = Math.Ceiling(L);
			Graph.addHyperArc(A);
			var hy = Graph.hyperarcs[Graph.hyperarcs.Count - 1];
			List<double> Values = new List<double>();
			foreach(arc arc in hy.IntraArcs)
			{
				HashSet<node> arcnodes = new HashSet<node>(new node[] {arc.From,arc.To});
				Values.Add(Memo[arcnodes]);
			}
			Graph.removeHyperArc(hy);
			Values.Sort();

			double total = 0;
			for(int x=0;x<MinTreeDepth;x++)
			{
				total = total + Values[x];
			}

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

	//Stores information about a candidate option
	class TreeCandidate:IComparable<TreeCandidate>
	{
		public SubAssembly sa;
		public List<node> RefNodes;
		public List<node> MovNodes;
		public double H;	//heuristic value

		public int CompareTo(TreeCandidate other) {  //compare based on heuristic values
			return H.CompareTo(other.H);
		}
	}
}