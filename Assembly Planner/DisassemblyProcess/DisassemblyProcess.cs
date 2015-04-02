using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;

namespace Assembly_Planner
{
    internal class DisassemblyProcess
    {
        internal static void Run(designGraph assemblyGraph, List<int> globalDirPool)
        {
            // This is a surrogate graph-based approach for "disassembly"
            // take a direction from the pool
            //   find the SCCs
            //   create the DBG
            //   generate the options
            
            assemblyGraph.addHyperArc(assemblyGraph.nodes, "ini");
            var iniHy = assemblyGraph.hyperarcs[assemblyGraph.hyperarcs.Count - 1];
            iniHy.localLabels.Add(DisConstants.SeperateHyperarcs);
            foreach (var i in globalDirPool)
            {
                var cndDir = DisassemblyDirections.Directions[i];
                foreach (var hy in assemblyGraph.hyperarcs.Where(h=>h.localLabels.Contains(DisConstants.SeperateHyperarcs)))
                {
                    SCC.StronglyConnectedComponents(assemblyGraph, hy, cndDir);
                    var blockingDic = DBG.DirectionalBlockingGraph(assemblyGraph, hy, cndDir);
                    OptionGenerator.GenerateOptions(assemblyGraph, hy, cndDir, blockingDic);
                }
            }

            // Now all the options are generated and it is the time to do the search
            // After apply, we can erase everything and start making the SCCs and DBGs from the beginning, 
            //    But majarity of the later options had beed found before, so I need to find a way to store them
            //    and use them again.

            // After apply, add the "seperate" lable to the chosen Hyperarc
        }
    }
}
