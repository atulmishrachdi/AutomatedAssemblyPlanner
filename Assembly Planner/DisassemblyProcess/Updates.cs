using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth.Representation;
using StarMathLib;

namespace Assembly_Planner
{
    internal class Updates
    {
        internal static void ApplyChild(AssemblyCandidate child, option opt)
        {
            // The function removes hyperarcs with "SCC" or "Seperate" lables
            for (var i = 0; i < child.graph.hyperarcs.Count; i++)
            {
                var hy = child.graph.hyperarcs[i];
                if (hy.localLabels.Contains(DisConstants.SeperateHyperarcs) && hy.nodes.Any(n => opt.nodes.Contains(n)))
                {
                    child.graph.removeHyperArc(hy);
                    i--;
                    continue;
                }
                if ((hy.localLabels.Contains(DisConstants.SeperateHyperarcs) &&
                     !hy.nodes.Any(n => opt.nodes.Contains(n))) || hy.localLabels.Contains(DisConstants.SingleNode))
                    continue;

                if (!hy.localLabels.Contains(DisConstants.Removable)) // Maybe all of them contain "Removable"
                {
                    child.graph.removeHyperArc(hy);
                    i--;
                }
                else
                {
                    if (hy.localLabels.Contains(DisConstants.SCC))
                        hy.localLabels.Remove(DisConstants.SCC);
                }
            }
        }

        internal static void UpdateGlobalDirections(List<int> globalDirPool)
        {
            // The function removes one of each parallel direction pair.
            for (var i = 0; i < globalDirPool.Count - 1; i++)
            {
                var dir1 = DisassemblyDirections.Directions[globalDirPool[i]];
                for (var j = i + 1; j < globalDirPool.Count; j++)
                {
                    var dir2 = DisassemblyDirections.Directions[globalDirPool[j]];
                    if (Math.Abs(1 + dir1.dotProduct(dir2)) > 1e-3) continue;
                    globalDirPool.RemoveAt(j);
                    j--;
                }
            }
        }

        internal static List<hyperarc> UpdatePreceedings(List<hyperarc> Preceedings)
        {
            var list = new List<hyperarc>();
            foreach (var preceeding in Preceedings.Where(p => !list.Contains(p)))
                list.Add(preceeding);
            return list;
        }

        internal static List<node> AddSecondHyperToOption(AssemblyCandidate child, option opt)
        {
            foreach (
                var sepHy in
                    child.graph.hyperarcs.Where(
                        a =>
                            a.localLabels.Contains(DisConstants.SeperateHyperarcs) &&
                            opt.nodes.All(n => a.nodes.Contains(n)))) //
                return sepHy.nodes.Where(n => !opt.nodes.Contains(n)).ToList();
            return null;
        }

        internal static void AddPartsProperties(string inputDir, designGraph assemblyGraph)
        {
            var reader = new StreamReader(File.OpenRead(@inputDir + "/information.csv"));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                var i = 1;
                var node = assemblyGraph.nodes.Where(n => n.name == Convert.ToString(values[0])).ToList()[0];
                while (i < 9)
                {
                    node.localVariables.Add(Convert.ToDouble(values[i]));
                    i++;
                }
            }
        }

        internal static void UpdateChildGraph(AssemblyCandidate c, List<node>[] install)
        {
            // This is happening in Evaluation
            for (var j = 0; j < c.graph.hyperarcs.Count; j++)
            {
                var hy = c.graph.hyperarcs[j];
                if (hy.localLabels.Contains(DisConstants.SeperateHyperarcs) ||
                    hy.localLabels.Contains(DisConstants.SingleNode)) continue;
                c.graph.removeHyperArc(c.graph.hyperarcs[j]);
                j--;
            }

            foreach (var list in install)
            {
                if (list.Count == 1)
                {
                    c.graph.addHyperArc(list);
                    c.graph.hyperarcs[c.graph.hyperarcs.Count - 1].localLabels.Add(DisConstants.SingleNode);
                }
                else
                {
                    c.graph.addHyperArc(list);
                    c.graph.hyperarcs[c.graph.hyperarcs.Count - 1].localLabels.Add(DisConstants.SeperateHyperarcs);
                }
            }
        }


        internal static bool EitherRefOrMovHasSeperatedSubassemblies(List<node>[] install)
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

                        foreach (arc pNodeArc in pNode.arcs.Where(a => a.GetType() == typeof (arc)))
                        {
                            var otherNode = pNodeArc.From == pNode ? pNodeArc.To : pNodeArc.From;
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

        internal static void RemoveRepeatedFastenersMain(arc a, designGraph graph)
        {
            if (!a.localVariables.Contains(DisConstants.IndexOfNodesLockedByFastenerL)) return;
            var counter = a.localVariables.IndexOf(DisConstants.DirIndUpperBound) + 1;
            var boltsInformation = new List<List<double>>();
            while (counter < a.localVariables.Count)
            {
                var oneBoltInfo = new List<double>();
                for (var i = counter; i < a.localVariables.Count; i++)
                {
                    oneBoltInfo.Add(a.localVariables[i]);
                    if (i != a.localVariables.Count - 1 &&
                        a.localVariables[i + 1] != DisConstants.BoltDirectionOfFreedom) continue;
                    boltsInformation.Add(oneBoltInfo);
                    counter += oneBoltInfo.Count;
                    break;
                }
            }
            foreach (var oneBoltInfo in boltsInformation)
            {
                if (!oneBoltInfo.Contains(DisConstants.IndexOfNodesLockedByFastenerL)) continue;
                var l = oneBoltInfo.IndexOf(DisConstants.IndexOfNodesLockedByFastenerL);
                var u = oneBoltInfo.IndexOf(DisConstants.IndexOfNodesLockedByFastenerU); 
                var nodes = new List<node>();
                for (var i = l + 1; i < u; i++)
                    nodes.Add(graph.nodes[(int)oneBoltInfo[i]]);
                foreach (
                    arc arc in
                        graph.arcs.Where(
                            arc => arc is arc && arc != a && (nodes.Contains(arc.From) && nodes.Contains(arc.To))))
                {
                    var counter2 = arc.localVariables.IndexOf(DisConstants.DirIndUpperBound) + 1;
                    while (counter2 < arc.localVariables.Count)
                    {
                        var list = new List<double>();
                        var removed = false;
                        for (var i = 0; i < oneBoltInfo.Count; i++)
                        {
                            list.Add(arc.localVariables[i+counter2]);
                            if (i + counter2 != arc.localVariables.Count - 1 &&
                                arc.localVariables[i + counter2 + 1] != DisConstants.BoltDirectionOfFreedom) continue;
                            if (oneBoltInfo.SequenceEqual(list))
                            {
                                arc.localVariables.RemoveRange(counter2, list.Count);
                                removed = true;
                                break;
                            }
                            counter2 += list.Count;
                            break;
                        }
                        if (removed) break;
                    }
                }
            }
        }
    }
}
