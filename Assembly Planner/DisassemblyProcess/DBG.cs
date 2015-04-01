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
        internal static void DirectionalBlockingGraph(designGraph assemblyGraph, hyperarc hy, double[] cndDir)
        {
            // So, I am trying to make the DBG for for each seperate hyperarc. 
            // This hyperarc includes small hyperarcs with the lable  "SCC"
            // Each element of the DBG is one SCC hyperarc
            //  I was thinking instead of having a graph for DBG, simly create a dictionary
            //      the key is the SCC hyperarc and the value is a list of hyperarcs which are blocking the key

            var dbgDictionary = new Dictionary<hyperarc, List<hyperarc>>();

            foreach (var sccHy in assemblyGraph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SCC) 
                     && !h.localLabels.Contains(DisConstants.Removable)))
            {
                var hyperarcBorderArcs = HyperarcBorderArcsFinder(sccHy);
                var blockedWith = new List<hyperarc>();
                foreach (var borderArc in hyperarcBorderArcs)
                {
                    // if (From in sccHy)
                    //      blocked if: has a direction which is parallel but reverse
                    // if (To in sccHy)
                    //      blocked if: has a direction which is parallel and same direction

                    if (sccHy.nodes.Contains(borderArc.From))
                    {
                        if (Parallel(borderArc, cndDir) != -1) continue;
                        var blocking = BlockingSccFinder(assemblyGraph, sccHy, borderArc);
                        if (!blockedWith.Contains(blocking))
                            blockedWith.Add(blocking);
                    }
                    else // contains  "To"
                    {
                        if (Parallel(borderArc, cndDir) != 1) continue;
                        var blocking = BlockingSccFinder(assemblyGraph, sccHy, borderArc);
                        if (!blockedWith.Contains(blocking))
                            blockedWith.Add(blocking);
                    }
                }
                dbgDictionary.Add(sccHy,blockedWith);
            }
        }

        private static int Parallel(arc borderArc, double[] cndDir)
        {
            var indexL = borderArc.localVariables.IndexOf(GraphConstants.DirIndLowerBound);
            var indexU = borderArc.localVariables.IndexOf(GraphConstants.DirIndUpperBound);
            for (var i = indexL + 1; i < indexU; i++)
            {
                var arcDisDir = DisassemblyDirections.Directions[(int)borderArc.localVariables[i]];
                if (1 - arcDisDir.dotProduct(cndDir) < ConstantsPrimitiveOverlap.CheckWithGlobDirsParall)
                    return 1;
                if (1 + arcDisDir.dotProduct(cndDir) < ConstantsPrimitiveOverlap.CheckWithGlobDirsParall)
                    return -1;
            }
            return 0;
        }

        private static hyperarc BlockingSccFinder(designGraph graph, hyperarc sccHy, arc arc)
        {
            if (sccHy.nodes.Contains(arc.From))
            {
                foreach (var hy in graph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SCC) 
                         && !h.localLabels.Contains(DisConstants.Removable)))
                {
                    if (hy.nodes.Contains(arc.To))
                        return hy;
                }
            }
            foreach (var hy in graph.hyperarcs.Where(h => h.localLabels.Contains(DisConstants.SCC)
                     && !h.localLabels.Contains(DisConstants.Removable)))
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
                foreach (arc arc in node.arcs)
                {
                    var otherNode = arc.From == node ? arc.To : arc.From;
                    if (sccHy.nodes.Contains(otherNode)) continue;
                    borders.Add(arc);
                }
            }
            return borders;
        }
    }
}
