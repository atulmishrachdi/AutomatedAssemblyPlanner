using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth.Representation;

namespace Assembly_Planner
{
    interface IEvaluationFunction
    {

        internal bool Evaluate(List<node> s1, List<node> s2, out InstallAction install);
    }
}
