using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using StarMathLib;

namespace Assembly_Planner
{
    internal class DBG
    {
        static List<hyperarc> Preceedings = new List<hyperarc>();
        private static int co;

        internal static Dictionary<hyperarc, List<hyperarc>> DirectionalBlockingGraph(designGraph assemblyGraph, hyperarc seperate, int cndDirInd)
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
            var connectedButUnblocked= new Dictionary<hyperarc, List<hyperarc>>();
            foreach (var sccHy in assemblyGraph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SCC)))
            {
                var hyperarcBorderArcs = HyperarcBorderArcsFinder(sccHy);
                var blockedWith = new List<hyperarc>();
                var notBlockedWith = new List<hyperarc>();
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
                            notBlockedWith.Add(blocking);
                            continue;
                        }
                        if (!blockedWith.Contains(blocking))
                            blockedWith.Add(blocking);
                    }
                    else // contains  "To"
                    {
                        if (Parallel(borderArc, cndDirInd) != 1)
                        {
                            notBlockedWith.Add(blocking);
                            continue;
                        }
                        if (!blockedWith.Contains(blocking))
                            blockedWith.Add(blocking);
                    }
                }
                dbgDictionary.Add(sccHy,blockedWith);
                connectedButUnblocked.Add(sccHy,notBlockedWith);
            }
            dbgDictionary = UnconnectedBlockingDetermination.Run(dbgDictionary, connectedButUnblocked, cndDirInd);
            dbgDictionary = UpdateBlockingDic(dbgDictionary);
            return dbgDictionary;
        }

        private static int Parallel(arc borderArc, int cndDirInd)
        {
            // 1: parallel and same direction
            // -1: parallel but opposite direction
            // 0 not parallel. 
            var cndDir = DisassemblyDirections.Directions[cndDirInd];
            var indexL = borderArc.localVariables.IndexOf(GraphConstants.DirIndLowerBound);
            var indexU = borderArc.localVariables.IndexOf(GraphConstants.DirIndUpperBound);
            var paralAndSame = false;
            var paralButOppose = false;
            for (var i = indexL + 1; i < indexU; i++)
            {
                var arcDisDir = DisassemblyDirections.Directions[(int)borderArc.localVariables[i]];
                if (Math.Abs(1 - arcDisDir.dotProduct(cndDir)) < ConstantsPrimitiveOverlap.CheckWithGlobDirsParall)
                    paralAndSame = true;
                if (Math.Abs(1 + arcDisDir.dotProduct(cndDir)) < ConstantsPrimitiveOverlap.CheckWithGlobDirsParall)
                    paralButOppose = true;
            }
            if (paralAndSame && paralButOppose) return 0;
            if (paralAndSame) return 1;
            if (paralButOppose) return -1;
            return 0;
        }

        private static hyperarc BlockingSccFinder(designGraph graph, hyperarc sccHy, arc arc)
        {
            if (sccHy.nodes.Contains(arc.From))
            {
                foreach (var hy in graph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SCC)))
                {
                    if (hy.nodes.Contains(arc.To))
                        return hy;
                }
            }
            foreach (var hy in graph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SCC)))
            {
                if (hy.nodes.Contains(arc.From))
                    return hy;
            }
            return null;
        }

        private static List<arc> HyperarcBorderArcsFinder(hyperarc sccHy)
        {
            var borders = new List<arc>();
            foreach (node node in sccHy.nodes)
            {
                foreach (arc arc in node.arcs.Where(a => a.GetType() == typeof(arc)))
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
            foreach (var value in blockingDic[sccHy])
                PreceedingFinder(value, blockingDic);
        }
    }
}
