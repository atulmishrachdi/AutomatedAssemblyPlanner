using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using Assembly_Planner.GraphSynth.BaseClasses;

namespace Assembly_Planner
{
    internal class BoostedSCC
    {
        // OptimizedSCC is the modified SCC. In this class, a dictionary of SCCs for each direction is created
        // to take benefit from premade SCC hyperarcs. The dictionary will be updated after each "apply"
        internal static void StronglyConnectedComponents(designGraph assemblyGraph, hyperarc seperateHy, int cndDir)
        {
            
            var preAddedSccs = new List<Component>();
            if (BeamSearch.SccTracker.Keys.Contains(cndDir))
            {
                foreach (var premadeSCC in BeamSearch.SccTracker[cndDir])
                {
                    var c = 0;
                    foreach (var node in premadeSCC)
                        c += seperateHy.nodes.Count(n => n.name == node.name);
                    if (c != premadeSCC.Count) continue;

                    var nodes = new List<node>();
                    foreach (var n in premadeSCC)
                        nodes.AddRange(seperateHy.nodes.Where(a=>a.name == n.name));
                    var last = assemblyGraph.addHyperArc(nodes);
                    last.localLabels.Add(DisConstants.SCC);
                    preAddedSccs.AddRange(premadeSCC);
                }
                //foreach (var premadeSCC in DisassemblyProcess.SccTracker[cndDir].Where(pmSCC => pmSCC.All(n => seperateHy.nodes.Contains(n))))
                //{
                //    var last = assemblyGraph.addHyperArc(premadeSCC);
                //    last.localLabels.Add(DisConstants.SCC);
                //    preAddedSccs.AddRange(premadeSCC);
                //}
            }
            var globalVisited = new List<Component>();
            var stack = new Stack<Component>();
            var visited = new HashSet<Component>();
            var sccTrackerNodes = new List<List<Component>>();
            foreach (Component node in seperateHy.nodes.Where(n =>!globalVisited.Contains(n)))
            {
                if (preAddedSccs.Any(n => n.name == node.name)) 
                    continue;
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
                        if (SCC.Removable(pNodeArc, cndDir))
                            continue;
                        var otherNode =(Component) (pNodeArc.From == pNode ? pNodeArc.To : pNodeArc.From);
                        if (visited.Contains(otherNode))
                            continue;
                        stack.Push(otherNode);
                    }
                }
                assemblyGraph.addHyperArc(visited.Cast<node>().ToList());
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
