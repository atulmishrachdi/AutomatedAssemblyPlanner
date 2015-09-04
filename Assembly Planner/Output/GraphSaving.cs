using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth;
using GraphSynth.Representation;

namespace Assembly_Planner
{
    class GraphSaving
    {
        internal static void SaveTheGraph(designGraph assemblyGraph)
        {
            var outputDirectory = "../../../Test";
            var setting = new GlobalSettings();
            var sa = new BasicFiler(setting.InputDir, setting.OutputDir, setting.RulesDir);
            sa.outputDirectory = outputDirectory;
            sa.Save("bighbigh.gxml", assemblyGraph, false);
        }
    }
}
