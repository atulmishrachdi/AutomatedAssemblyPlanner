using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using StarMathLib;
using Assembly_Planner.GraphSynth.BaseClasses;

namespace Assembly_Planner
{
    internal class SCCBinary
    {
        public static Dictionary<int, HashSet<int>> ParallelDirections;
        internal static void StronglyConnectedComponents(designGraph graph, HashSet<Component> seperate, int cndDir)
        {
            // The function takes every hyperarc with "seperate" lable and generates its SCCs with respect to 
            // the candidate direction. After generation, a new hyperarc is added to the graph with local lable "SCC".
            var seperateHys =seperate.Select(n => new hyperarc("", new List<node> {n})).ToList();
            seperateHys.AddRange(graph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.gSCC)));

            var stack = new Stack<hyperarc>();
            var visited = new HashSet<hyperarc>();
            var globalVisited = new HashSet<hyperarc>();
            foreach (var nodeHy in seperateHys.Where(n => !globalVisited.Contains(n)))
            {
                stack.Clear();
                visited.Clear();
                stack.Push(nodeHy);
                while (stack.Count > 0)
                {
                    var pNodeHy = stack.Pop();
                    visited.Add(pNodeHy);
                    globalVisited.Add(pNodeHy);
                    var connections = HyperBorderArcs(graph, pNodeHy);
                    foreach (var pHyArc in connections)
                    {
                        if (Removable(pHyArc, cndDir))
                            continue;
                        //var otherNode = (Component)(pNodeArc.From == pNodeHy ? pNodeArc.To : pNodeArc.From);
                        var otherHys = OtherHyperarcFinder(pHyArc, pNodeHy, seperateHys).ToList();
                        if (!otherHys.Any()) continue;
                        if (visited.Contains(otherHys[0]))
                            continue;
                        //if (!seperateHys.Contains(otherHys[0]))
                        //continue;
                        stack.Push(otherHys[0]);
                    }
                }
                var visNodes = visited.SelectMany(v => v.nodes).ToList();
                if (visNodes.Count == seperate.Count) continue;

                var last = graph.addHyperArc(visNodes);
                last.localLabels.Add(DisConstants.SCC);
            }
        }

        public static bool Removable(Connection pNodeArc, int cndDirInd)
        {
            // The function returns "true" if the local variables of the"pNodeArc" 
            // contains a direction that is parralel to the candidate direction. 

            return pNodeArc.InfiniteDirections.Any(dir => ParallelDirections[cndDirInd].Contains(dir));
        }

        private static IList<Connection> HyperBorderArcs(designGraph graph, hyperarc pNodeHy)
        {
            if (pNodeHy.nodes.Count == 1)
                return pNodeHy.nodes[0].arcs.Where(a => a is Connection).Cast<Connection>().ToList();
            return DBGBinary.HyperarcBorderArcsFinder(pNodeHy);
        }

        private static IEnumerable<hyperarc> OtherHyperarcFinder(Connection pHyArc, hyperarc pNodeHy, List<hyperarc> seperateHys)
        {
            if (pNodeHy.nodes.Any(n => n.name == pHyArc.From.name)) return seperateHys.Where(sh => sh.nodes.Any(n => n.name == pHyArc.To.name));
            return seperateHys.Where(sh => sh.nodes.Any(n => n.name == pHyArc.From.name));
        }
    }
}
