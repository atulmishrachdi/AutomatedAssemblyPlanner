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
    internal class DBGBinary
    {
        static List<hyperarc> Preceedings = new List<hyperarc>();
        private static int co;
        private static List<hyperarc> visited = new List<hyperarc>();
        internal static Dictionary<hyperarc, List<hyperarc>> DirectionalBlockingGraph(designGraph assemblyGraph, int cndDirInd)
        {
            // So, I am trying to make the DBG for for each seperate hyperarc. 
            // This hyperarc includes small hyperarcs with the lable  "SCC"
            // Each element of the DBG is one SCC hyperarc
            //  I was thinking instead of having a graph for DBG, simly create a dictionary
            //      the key is the SCC hyperarc and the value is a list of hyperarcs which are blocking the key

            // 6/17/2015: One important thing I need to add to the DBG is the blocking between parts which are not 
            //            connected (touched). For instance in my PumpAssembly, ring is not connected to the shaft
            //            but it is blocked by that. Also shaft is not touching the lid, but it is blocked by that. 

            var dbgDictionary = new Dictionary<hyperarc, List<hyperarc>>();
            //var connectedButUnblocked= new Dictionary<hyperarc, List<hyperarc>>();
            foreach (var sccHy in assemblyGraph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SCC)))
            {
                var hyperarcBorderArcs = HyperarcBorderArcsFinder(sccHy);
                var blockedWith = new List<hyperarc>();
                //var notBlockedWith = new List<hyperarc>();
                foreach (var borderArc in hyperarcBorderArcs)
                {
                    // if (From in sccHy)
                    //      blocked if: has a direction which is parallel but reverse
                    // if (To in sccHy)
                    //      blocked if: has a direction which is parallel and same direction
                    var blocking = BlockingSccFinder(assemblyGraph, sccHy, borderArc);
                    if (sccHy.nodes.Contains(borderArc.From))
                    {
                        if (Parallel(borderArc, cndDirInd) != -1)
                        {
                            //notBlockedWith.Add(blocking);
                            continue;
                        }
                        if (!blockedWith.Contains(blocking))
                            blockedWith.Add(blocking);
                    }
                    else // contains  "To"
                    {
                        if (Parallel(borderArc, cndDirInd) != 1)
                        {
                            //notBlockedWith.Add(blocking);
                            continue;
                        }
                        if (!blockedWith.Contains(blocking))
                            blockedWith.Add(blocking);
                    }
                }
                dbgDictionary.Add(sccHy, blockedWith);
                //connectedButUnblocked.Add(sccHy,notBlockedWith);
            }
            //dbgDictionary = CombineWithNonAdjacentBlockings2(dbgDictionary, cndDirInd);
            dbgDictionary = CombineWithNonAdjacentBlockingsUsingSecondaryConnections(dbgDictionary, assemblyGraph, cndDirInd);
            dbgDictionary = SolveMutualBlocking(assemblyGraph, dbgDictionary);
            dbgDictionary = UpdateBlockingDic(dbgDictionary);
            //if (MutualBlocking(assemblyGraph, dbgDictionary))
            //    dbgDictionary = DirectionalBlockingGraph(assemblyGraph, seperate, cndDirInd); // This is expensive. Get rid of it.
            dbgDictionary = SolveMutualBlocking(assemblyGraph, dbgDictionary);
            if (MutualBlocking2(assemblyGraph, dbgDictionary))
            {
                var df = 2;
            }
            return dbgDictionary;
        }

        private static Dictionary<hyperarc, List<hyperarc>> CombineWithNonAdjacentBlockingsUsingSecondaryConnections(
     Dictionary<hyperarc, List<hyperarc>> dbgDictionary, designGraph assemblyGraph, int cndDirInd)
        {
            var direction = DisassemblyDirections.Directions[cndDirInd];
            var dirs = (from gDir in DisassemblyDirections.Directions
                        where 1 - Math.Abs(gDir.dotProduct(direction)) < OverlappingFuzzification.CheckWithGlobDirsParall
                        select DisassemblyDirections.Directions.IndexOf(gDir)).ToList();
            var oppositeDir = dirs.Where(d => d != cndDirInd).ToList();
            foreach (SecondaryConnection SC in assemblyGraph.arcs.Where(a => a is SecondaryConnection))
            {
                var blockedScc = dbgDictionary.Keys.ToList().Where(scc => scc.nodes.Contains(SC.From));
                var blockingScc = dbgDictionary.Keys.ToList().Where(scc => scc.nodes.Contains(SC.To));
                if (!blockedScc.Any() || !blockingScc.Any())
                    continue;
                if (oppositeDir.Any())
                {
                    if (SC.Directions.Contains(cndDirInd) && SC.Directions.Contains(oppositeDir[0]))
                    {
                        foreach (var blocked in blockedScc)
                        {
                            foreach (var blocking in blockingScc.Where(b => b != blocked))
                            {
                                if (!dbgDictionary[blocked].Contains(blocking))
                                    dbgDictionary[blocked].Add(blocking);
                                if (!dbgDictionary[blocking].Contains(blocked))
                                    dbgDictionary[blocking].Add(blocked);
                            }
                        }
                    }
                    else if (SC.Directions.Contains(cndDirInd))
                    {
                        foreach (var blocked in blockedScc)
                        {
                            foreach (var blocking in blockingScc.Where(b => b != blocked))
                            {
                                if (!dbgDictionary[blocked].Contains(blocking))
                                    dbgDictionary[blocked].Add(blocking);
                            }
                        }
                    }
                    else if (SC.Directions.Contains(oppositeDir[0]))
                    {
                        foreach (var blocked in blockedScc)
                        {
                            foreach (var blocking in blockingScc.Where(b => b != blocked))
                            {
                                if (!dbgDictionary[blocking].Contains(blocked))
                                    dbgDictionary[blocking].Add(blocked);
                            }
                        }
                    }
                }
                else
                {
                    if (!SC.Directions.Contains(cndDirInd)) continue;
                    foreach (var blocked in blockedScc)
                    {
                        foreach (var blocking in blockingScc.Where(b => b != blocked))
                        {
                            if (!dbgDictionary[blocked].Contains(blocking))
                                dbgDictionary[blocked].Add(blocking);
                        }
                    }
                }
            }
            return dbgDictionary;
        }

        internal static Dictionary<hyperarc, List<hyperarc>> SolveMutualBlocking(designGraph assemblyGraph, Dictionary<hyperarc, List<hyperarc>> dbgDictionary)
        {
            for (var i = 0; i < dbgDictionary.Count - 1; i++)
            {
                var iKey = dbgDictionary.Keys.ToList()[i];
                for (var j = i + 1; j < dbgDictionary.Count; j++)
                {
                    var jKey = dbgDictionary.Keys.ToList()[j];
                    if (dbgDictionary[iKey].Contains(jKey) && dbgDictionary[jKey].Contains(iKey))
                    {
                        var nodes = new List<node>();
                        nodes.AddRange(iKey.nodes);
                        nodes.AddRange(jKey.nodes);
                        var nodes2 = new List<node>(nodes);
                        var last = assemblyGraph.addHyperArc(nodes2);
                        last.localLabels.Add(DisConstants.SCC);
                        var updatedBlocking = new List<hyperarc>();
                        updatedBlocking.AddRange(dbgDictionary[iKey].Where(hy => hy != jKey));
                        updatedBlocking.AddRange(dbgDictionary[jKey].Where(hy => hy != iKey && !updatedBlocking.Contains(hy)));
                        var updatedBlocking2 = new List<hyperarc>(updatedBlocking);
                        dbgDictionary.Remove(iKey);
                        dbgDictionary.Remove(jKey);
                        foreach (var key in dbgDictionary.Keys.ToList())
                        {
                            if (dbgDictionary[key].Contains(iKey) || dbgDictionary[key].Contains(jKey))
                            {
                                dbgDictionary[key].Add(last);
                                dbgDictionary[key].Remove(iKey);
                                dbgDictionary[key].Remove(jKey);
                            }
                            if (dbgDictionary[key].Any(a => a == null))
                            {
                                var a = 2;
                            }
                        }
                        
                        assemblyGraph.removeHyperArc(iKey);
                        assemblyGraph.removeHyperArc(jKey);
                        dbgDictionary.Add(last, updatedBlocking2);
                        i--;
                        break;
                    }
                }
            }
            return dbgDictionary;
        }

        private static bool MutualBlocking(designGraph assemblyGraph, Dictionary<hyperarc, List<hyperarc>> dbgDictionary)
        {
            for (var i = 0; i < dbgDictionary.Count - 1; i++)
            {
                var iKey = dbgDictionary.Keys.ToList()[i];
                for (var j = i + 1; j < dbgDictionary.Count; j++)
                {
                    var jKey = dbgDictionary.Keys.ToList()[j];
                    if (dbgDictionary[iKey].Contains(jKey) && dbgDictionary[jKey].Contains(iKey))
                    {
                        var nodes = new List<node>();
                        nodes.AddRange(iKey.nodes);
                        nodes.AddRange(jKey.nodes);
                        assemblyGraph.removeHyperArc(iKey);
                        assemblyGraph.removeHyperArc(jKey);
                        var last = assemblyGraph.addHyperArc(nodes);
                        last.localLabels.Add(DisConstants.SCC);
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool MutualBlocking2(designGraph assemblyGraph, Dictionary<hyperarc, List<hyperarc>> dbgDictionary)
        {
            for (var i = 0; i < dbgDictionary.Count - 1; i++)
            {
                var iKey = dbgDictionary.Keys.ToList()[i];
                for (var j = i + 1; j < dbgDictionary.Count; j++)
                {
                    var jKey = dbgDictionary.Keys.ToList()[j];
                    if (dbgDictionary[iKey].Contains(jKey) && dbgDictionary[jKey].Contains(iKey))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static Dictionary<hyperarc, List<hyperarc>> CombineWithNonAdjacentBlockings2(Dictionary<hyperarc, List<hyperarc>> dbgDictionary, int cndDirInd)
        {
            var direction = DisassemblyDirections.Directions[cndDirInd];
            var dirs = (from gDir in DisassemblyDirections.Directions
                        where 1 - Math.Abs(gDir.dotProduct(direction)) < OverlappingFuzzification.CheckWithGlobDirsParall
                        select DisassemblyDirections.Directions.IndexOf(gDir)).ToList();
            if (NonadjacentBlockingDetermination.NonAdjacentBlocking.Count == 0) return dbgDictionary;
            foreach (var dir in dirs)
            {
                if (dir == cndDirInd)
                {
                    foreach (var nonAdjBlo in NonadjacentBlockingDetermination.NonAdjacentBlocking[dir])
                    {
                        foreach (
                            var scc1 in
                                dbgDictionary.Keys.Where(scc1 => scc1.nodes.Any(n => n.name == nonAdjBlo.blockingSolids[0].Name))
                                    .ToList())
                        {
                            foreach (
                                var scc2 in
                                    dbgDictionary.Keys.Where(
                                        scc2 => scc2 != scc1 && scc2.nodes.Any(n => n.name == nonAdjBlo.blockingSolids[1].Name)))
                            {
                                if (!dbgDictionary[scc1].Contains(scc2))
                                    dbgDictionary[scc1].Add(scc2);
                                break;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var nonAdjBlo in NonadjacentBlockingDetermination.NonAdjacentBlocking[dir])
                    {
                        foreach (
                            var scc1 in
                                dbgDictionary.Keys.Where(scc1 => scc1.nodes.Any(n => n.name == nonAdjBlo.blockingSolids[1].Name))
                                    .ToList())
                        {
                            foreach (
                                var scc2 in
                                    dbgDictionary.Keys.Where(
                                        scc2 => scc2 != scc1 && scc2.nodes.Any(n => n.name == nonAdjBlo.blockingSolids[0].Name)))
                            {
                                if (!dbgDictionary[scc1].Contains(scc2))
                                    dbgDictionary[scc1].Add(scc2);

                                break;
                            }
                            break;
                        }
                    }
                }
            }
            return dbgDictionary;
        }

        internal static int Parallel(Connection borderArc, int cndDirInd)
        {
            //  1: parallel and same direction
            // -1: parallel but opposite direction
            //  0: not parallel. 
            //  2: parralel same direction and opposite direction
            var cndDir = DisassemblyDirections.Directions[cndDirInd];
            var paralAndSame = false;
            var paralButOppose = false;
            foreach (var dirInd in borderArc.InfiniteDirections)
            {
                var arcDisDir = DisassemblyDirections.Directions[dirInd];
                if (Math.Abs(1 - arcDisDir.dotProduct(cndDir)) < OverlappingFuzzification.CheckWithGlobDirsParall)
                    paralAndSame = true;
                if (Math.Abs(1 + arcDisDir.dotProduct(cndDir)) < OverlappingFuzzification.CheckWithGlobDirsParall)
                    paralButOppose = true;
            }
            if (paralAndSame && paralButOppose) return 2;
            if (paralAndSame) return 1;
            if (paralButOppose) return -1;
            return 0;
        }

        private static hyperarc BlockingSccFinder(designGraph graph, hyperarc sccHy, Connection arc)
        {
            if (sccHy.nodes.Contains(arc.From))
            {
                foreach (var hy in graph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SCC)))
                {
                    if (hy.nodes.Contains(arc.To))
                        return hy;
                }
                return null;
            }
            foreach (var hy in graph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SCC)))
            {
                if (hy.nodes.Contains(arc.From))
                    return hy;
            }
            return null;
        }

        internal static List<Connection> HyperarcBorderArcsFinder(hyperarc sccHy)
        {
            var borders = new List<Connection>();
            foreach (Component node in sccHy.nodes)
            {
                foreach (Connection arc in node.arcs.Where(a => a.GetType() == typeof(Connection)))
                {
                    var otherNode = arc.From == node ? arc.To : arc.From;
                    if (sccHy.nodes.Contains(otherNode)) continue;
                    borders.Add(arc);
                }
            }
            return borders;
        }
        private static Dictionary<hyperarc, List<hyperarc>> UpdateBlockingDic(Dictionary<hyperarc, List<hyperarc>> blockingDic)
        {
            var newBlocking = new Dictionary<hyperarc, List<hyperarc>>();
            foreach (var sccHy in blockingDic.Keys)
            {
                Preceedings.Clear();
                co = 0;
                PreceedingFinder(sccHy, blockingDic);
                Preceedings = Updates.UpdatePreceedings(Preceedings);
                visited.Clear();
                var cpy = new List<hyperarc>(Preceedings);
                newBlocking.Add(sccHy, cpy);
            }
            return newBlocking;
        }
        private static void PreceedingFinder(hyperarc sccHy, Dictionary<hyperarc, List<hyperarc>> blockingDic)
        {
            co++;
            if (co != 1)
                Preceedings.Add(sccHy);
            foreach (var value in blockingDic[sccHy].Where(v=> v != null))
            {
                if (visited.Contains(value)) continue;
                visited.Add(value);
                PreceedingFinder(value, blockingDic);
            }
        }
    }
}
