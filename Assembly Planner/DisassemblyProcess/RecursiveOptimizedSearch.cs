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
    class RecursiveOptimizedSearch
    {
        protected static EvaluationForBinaryTree assemblyEvaluator;
		protected static int[] Count = new int[21];

        internal static List<AssemblyCandidate> Run(ConvexHullAndBoundingBox inputData, List<int> globalDirPool)
        {
            var assemblyGraph = inputData.graphAssembly;
            var solutions = new List<AssemblyCandidate>();
			assemblyEvaluator = new EvaluationForBinaryTree();//inputData.ConvexHullDictionary);

            Updates.UpdateGlobalDirections(globalDirPool);

			SubAssembly Tree;
			var Best = F(out Tree, assemblyGraph, assemblyGraph.nodes, globalDirPool);

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

		protected static double F(out SubAssembly Tree, designGraph Graph, List<node> A, List<int> globalDirPool)
		{
			if (A.Count <= 1) {
				Count[1]++;
				Tree = null;
				return 0;
			}

			var options = new List<option>();
			foreach (var cndDirInd in globalDirPool)
			{
                SCCBinary.StronglyConnectedComponents(Graph, A, cndDirInd);
                var blockingDic = DBGBinary.DirectionalBlockingGraph(Graph, A, cndDirInd);
                options.AddRange(OptionGeneratorProBinary.GenerateOptions(Graph, A, blockingDic));
			}

			double Best = 999999;
			SubAssembly Bestsa = null, BestReference = null, BestMoving = null;
			foreach (var opt in options)
			{/*
				var child = (AssemblyCandidate)current.copy();
				SearchProcess.transferLmappingToChild(child.graph, Graph, opt);
				var rest = Updates.AddSecondHyperToOption(child,opt);
				Updates.ApplyChild(child, opt);
*/
				SubAssembly sa, Reference, Moving;
				if (assemblyEvaluator.EvaluateSub(A, opt.nodes, out sa) > 0)
				{
					List<node> RefNodes = sa.Install.Reference.PartNodes.Select(n => (node)Graph[n]).ToList();
					List<node> MovNodes = sa.Install.Moving.PartNodes.Select(n => (node)Graph[n]).ToList();
					double RefTime = F(out Reference, Graph, RefNodes, globalDirPool);
					double MovTime = F(out Moving,    Graph, MovNodes, globalDirPool);
					double MaxT = Math.Max (RefTime, MovTime);
					double Evaluation = sa.Install.Time + MaxT;
					if (Evaluation < Best)
					{
						Best = Evaluation;
						Bestsa = sa;
						BestReference = Reference;
						BestMoving = Moving;
					}
				}
			}

			Tree = Bestsa;
			Tree.Install.Reference = BestReference;
			Tree.Install.Moving = BestMoving;

			Count[A.Count]++;
			return Best;
		}
    }
}
