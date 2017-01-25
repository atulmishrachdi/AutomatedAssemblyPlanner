using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assembly_Planner.GraphSynth.BaseClasses;
using GraphSynth;
using GraphSynth.Representation;

namespace Assembly_Planner
{
    class GraphSaving
    {
        internal static void SaveTheGraph(designGraph assemblyGraph)
        {
            var outputDirectory = "";
            var setting = new GlobalSettings();
            var sa = new BasicFiler(setting.InputDir, setting.OutputDir, setting.RulesDir);
            sa.outputDirectory = outputDirectory;
            sa.Save("abbasgholi.gxml", assemblyGraph, false);
        }
        internal static object[] OpenSavedGraph(String fileName)
        {
            var setting = new GlobalSettings();
            var sa = new BasicFiler(setting.InputDir, setting.OutputDir, setting.RulesDir);
            return sa.Open(fileName);
        }

        internal static List<int> RetrieveGlobalDirsFromExistingGraph(designGraph assemblyGraph)
        {
            var gD = new List<int>();
            foreach (Connection connection in assemblyGraph.arcs.Where(a=> a is Connection))
                gD.AddRange(connection.InfiniteDirections.Where(i => !gD.Contains(i)));
            return gD;
        }
    }
}
