using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;
using BaseClasses;
using BaseClasses.Representation;
using Geometric_Reasoning;

namespace Plan_Generation
{
    internal class SCC
    {
        internal static void StronglyConnectedComponents(designGraph assemblyGraph, hyperarc seperate, int cndDir)
        {
            // The function takes every hyperarc with "seperate" lable and generates its SCCs with respect to 
            // the candidate direction. After generation, a new hyperarc is added to the graph with local lable "SCC".

            var stack = new Stack<Component>();
            var visited = new HashSet<Component>();
            var globalVisited = new HashSet<Component>();
            foreach (Component node in seperate.nodes.Where(n => !globalVisited.Contains(n)))
            {
                stack.Clear();
                visited.Clear();
                stack.Push(node);
                while (stack.Count > 0)
                {
                    var pNode = stack.Pop();
                    visited.Add(pNode);
                    globalVisited.Add(pNode);

                    foreach (Connection pNodeArc in pNode.arcs.Where(a => a.GetType() == typeof(Connection)))
                    {
                        if (Removable(pNodeArc, cndDir))
                            continue;
                        var otherNode = (Component)(pNodeArc.From == pNode ? pNodeArc.To : pNodeArc.From);
                        if (visited.Contains(otherNode))
                            continue;
                        stack.Push(otherNode);
                    }
                }
                if (visited.Count == seperate.nodes.Count) continue;

                var last = assemblyGraph.addHyperArc(visited.Cast<node>().ToList());
                last.localLabels.Add(DisConstants.SCC);
            }

        }

        public static bool Removable(Connection pNodeArc, int cndDirInd)
        {
            // The function returns "true" if the local variables of the"pNodeArc" 
            // contains a direction that is parralel to the candidate direction. 

            foreach (var rd in pNodeArc.InfiniteDirections)
            {
                var arcDisDir = StartProcess.Directions[rd];
                if (Math.Abs(1 - Math.Abs(arcDisDir.dotProduct(StartProcess.Directions[cndDirInd]))) <
                    OverlappingFuzzification.CheckWithGlobDirsParall ||
                    Math.Abs(1 + Math.Abs(arcDisDir.dotProduct(StartProcess.Directions[cndDirInd]))) <
                    OverlappingFuzzification.CheckWithGlobDirsParall)
                    //var dirInd = pNodeArc.localVariables[i];
                    //if (dirInd == cndDirInd)
                    return true;
            }
            return false;
        }
    }
}
