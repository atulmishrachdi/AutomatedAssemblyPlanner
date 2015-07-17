using GraphSynth.Representation;
using System;
using System.Collections.Generic;

namespace AssemblyEvaluation
{
    class ReOrientations
    {
        public Boolean Evaluate(List<arc> arcs_old, List<arc> arcs_new)
        {
            var newArc = arcs_new[0];
            var prevArc = arcs_old != null ? arcs_old[0] : arcs_new[0];
            var i = prevArc.localVariables.IndexOf(-1000);
            var j = newArc.localVariables.IndexOf(-1000);
            var insertionAxis1 = new Vector(newArc.localVariables[j + 1],
                                           newArc.localVariables[j + 2],
                                           newArc.localVariables[j + 3]);
            var insertionAxis2 = new Vector(prevArc.localVariables[i + 1],
                                          prevArc.localVariables[i + 2],
                                          prevArc.localVariables[i + 3]);
            return insertionAxis1 != insertionAxis2;

        }
    }
}
