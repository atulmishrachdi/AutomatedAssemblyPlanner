using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth.Representation;
using MIConvexHull;

namespace Assembly_Planner
{
    class EvaluationForBinaryTree
    {
        public double EvaluateSub(List<node> subassemblyNodes, List<node> optNodes, out SubAssembly sub)
        {

            var rest = subassemblyNodes.Where(n => !optNodes.Contains(n)).ToList();
            sub = Update(optNodes, rest);
            var install = new[] { rest, optNodes };
            if (EitherRefOrMovHasSeperatedSubassemblies(install, subassemblyNodes))
                return -1;
            sub.Install.Time = 10;
            return 1;
        }

        public SubAssembly Update(List<node> opt, List<node> rest)
        {
            Part refAssembly, movingAssembly;
            var movingNodes = opt;
            var newSubAsmNodes = rest;
            if (movingNodes.Count == 1)
            {
                var nodeName = movingNodes[0].name;
                movingAssembly = new Part(nodeName, 0, 0, null, null);
            }
            else
                movingAssembly = new SubAssembly(movingNodes, null, 0, 0, null);

            var referenceHyperArcnodes = new List<node>();
                referenceHyperArcnodes = (List<node>) newSubAsmNodes.Where(a => !movingNodes.Contains(a)).ToList();
            if (referenceHyperArcnodes.Count == 1)
            {
                var nodeName = referenceHyperArcnodes[0].name;
                refAssembly = new Part(nodeName, 0, 0,null, null);
            }
            else
                refAssembly = new SubAssembly(referenceHyperArcnodes, null, 0, 0, null);
            var newSubassembly = new SubAssembly(refAssembly, movingAssembly, null, 0,
                null);
            return newSubassembly;
        }

        internal static bool EitherRefOrMovHasSeperatedSubassemblies(List<node>[] install, 
            List<node> A)
        {
            foreach (var subAsm in install)
            {
                var stack = new Stack<node>();
                var visited = new HashSet<node>();
                var globalVisited = new HashSet<node>();
                foreach (var node in subAsm.Where(n => !globalVisited.Contains(n)))
                {
                    stack.Clear();
                    visited.Clear();
                    stack.Push(node);
                    while (stack.Count > 0)
                    {
                        var pNode = stack.Pop();
                        visited.Add(pNode);
                        globalVisited.Add(pNode);

                        foreach (Connection arc in pNode.arcs.Where(a => a.GetType() == typeof (Connection)))
                        {
                            if (!A.Contains(arc.From) || !A.Contains(arc.To) ||
                                !subAsm.Contains(arc.From) || !subAsm.Contains(arc.To)) continue;
                            var otherNode = arc.From == pNode ? arc.To : arc.From;
                            if (visited.Contains(otherNode))
                                continue;
                            stack.Push(otherNode);
                        }
                    }
                    if (visited.Count < subAsm.Count)
                        return true;
                }
            }
            return false;
        }
    }
}
