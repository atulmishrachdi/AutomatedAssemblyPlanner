using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using StarMathLib;

namespace Assembly_Planner
{
    internal class SCC
    {
        internal static void StronglyConnectedComponents(designGraph assemblyGraph, hyperarc hy, double[] cndDir)
        {
            var stack = new Stack<node>();
            var visited = new List<node>();

            foreach (var node in hy.nodes)
            {
                stack.Push(node);
                while (stack.Count > 0)
                {
                    var pNode = stack.Pop();
                    visited.Add(pNode);

                    foreach (arc pNodeArc in pNode.arcs)
                    {
                        if (Removable(pNodeArc, cndDir))
                            continue;
                        var otherNode = pNodeArc.From == pNode ? pNodeArc.To : pNodeArc.From;
                        if (visited.Contains(otherNode))
                            continue;
                        stack.Push(otherNode);
                    }
                }
                assemblyGraph.addHyperArc(visited);
                assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1].localLabels.Add(DisConstants.SCC);
            }
        }

        private static bool Removable(arc pNodeArc, double[] cndDir)
        {
            var indexL = pNodeArc.localVariables.IndexOf(GraphConstants.DirIndLowerBound);
            var indexU = pNodeArc.localVariables.IndexOf(GraphConstants.DirIndUpperBound);
            for (var i = indexL + 1; i < indexU; i++)
            {
                var arcDisDir = DisassemblyDirections.Directions[(int) pNodeArc.localVariables[i]];
                if (1 - Math.Abs(arcDisDir.dotProduct(cndDir)) < ConstantsPrimitiveOverlap.CheckWithGlobDirsParall)
                    return true;
            }
            return false;
        }
    }
}
