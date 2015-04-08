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
        internal static void StronglyConnectedComponents(designGraph assemblyGraph, hyperarc seperate, int cndDir)
        {
            // The function takes every hyperarc with "seperate" lable and generates its SCCs with respect to 
            // the candidate direction. After generation, a new hyperarc is added to the graph with local lable "SCC".
            
            var stack = new Stack<node>();
            var visited = new List<node>();

            foreach (var node in seperate.nodes.Where(n=>!visited.Contains(n)))
            {
                stack.Clear();
                visited.Clear();
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
                if (visited.Count == seperate.nodes.Count) continue;
                assemblyGraph.addHyperArc(visited);
                assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1].localLabels.Add(DisConstants.SCC);
            }
        }

        private static bool Removable(arc pNodeArc, int cndDirInd)
        {
            // The function returns "true" if the local variables of the"pNodeArc" 
            // contains a direction that is parralel to the candidate direction. 

            var indexL = pNodeArc.localVariables.IndexOf(GraphConstants.DirIndLowerBound);
            var indexU = pNodeArc.localVariables.IndexOf(GraphConstants.DirIndUpperBound);
            for (var i = indexL + 1; i < indexU; i++)
            {
                var arcDisDir = DisassemblyDirections.Directions[(int)pNodeArc.localVariables[i]];
                if (Math.Abs(1 - Math.Abs(arcDisDir.dotProduct(DisassemblyDirections.Directions[cndDirInd]))) <
                    ConstantsPrimitiveOverlap.CheckWithGlobDirsParall ||
                    Math.Abs(1 + Math.Abs(arcDisDir.dotProduct(DisassemblyDirections.Directions[cndDirInd]))) <
                    ConstantsPrimitiveOverlap.CheckWithGlobDirsParall)
                    //var dirInd = pNodeArc.localVariables[i];
                    //if (dirInd == cndDirInd)
                    return true;
            }
            return false;
        }
    }
}
