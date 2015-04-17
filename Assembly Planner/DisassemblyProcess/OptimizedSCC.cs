using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;

namespace Assembly_Planner
{
    internal class OptimizedSCC
    {
        // OptimizedSCC is the modified SCC. In this class, a dictionary of SCCs for each direction is created
        // to take benefit from premade SCC hyperarcs. The dictionary will be updated after each "apply"
        internal static void StronglyConnectedComponents(designGraph assemblyGraph, hyperarc seperateHy, int cndDir)
        {
            
            var preAddedSccs = new List<node>();
            if (BeamSearch.SccTracker.Keys.Contains(cndDir))
            {
                foreach (var premadeSCC in BeamSearch.SccTracker[cndDir])
                {
                    var c = 0;
                    foreach (var node in premadeSCC)
                    {
                        c += seperateHy.nodes.Count(n => n.name == node.name);
                    }
                    if (c != premadeSCC.Count) continue;
                    assemblyGraph.addHyperArc(premadeSCC);
                    assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1].localLabels.Add(DisConstants.SCC);
                    preAddedSccs.AddRange(premadeSCC);
                }
                //foreach (var premadeSCC in DisassemblyProcess.SccTracker[cndDir].Where(pmSCC => pmSCC.All(n => seperateHy.nodes.Contains(n))))
                //{
                //    assemblyGraph.addHyperArc(premadeSCC);
                //    assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1].localLabels.Add(DisConstants.SCC);
                //    preAddedSccs.AddRange(premadeSCC);
                //}
            }
            var globalVisited = new List<node>();
            var stack = new Stack<node>();
            var visited = new HashSet<node>();
            var sccTrackerNodes = new List<List<node>>();
            foreach (var node in seperateHy.nodes.Where(n => !preAddedSccs.Contains(n) && !globalVisited.Contains(n)))
            {
                stack.Clear();
                visited.Clear();
                stack.Push(node);
                while (stack.Count > 0)
                {
                    var pNode = stack.Pop();
                    visited.Add(pNode);
                    globalVisited.Add(pNode);

                    foreach (arc pNodeArc in pNode.arcs.Where(a => a.GetType() == typeof(arc)))
                    {
                        if (SCC.Removable(pNodeArc, cndDir))
                            continue;
                        var otherNode = pNodeArc.From == pNode ? pNodeArc.To : pNodeArc.From;
                        if (visited.Contains(otherNode))
                            continue;
                        stack.Push(otherNode);
                    }
                }
                assemblyGraph.addHyperArc(visited.ToList());
                assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1].localLabels.Add(DisConstants.SCC);
                sccTrackerNodes.Add(visited.ToList());
            }
            if (BeamSearch.SccTracker.Keys.Contains(cndDir))
                BeamSearch.SccTracker[cndDir] = sccTrackerNodes;
            else
                BeamSearch.SccTracker.Add(cndDir, sccTrackerNodes);
        }

    }
}
