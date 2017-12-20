using GraphSynth.Representation;
using System.Collections.Generic;

namespace Assembly_Planner
{
    interface IEvaluationFunction
    {

        bool Evaluate(List<node> s1, List<node> s2, out InstallAction install);
    }
}
