using Assembly_Planner.GraphSynth.BaseClasses;
using GraphSynth.Representation;
using System.Collections.Generic;
using System.Linq;

namespace Assembly_Planner
{
    class FeasibilityEvaluator
    {                     
        internal double EvaluateOrder(List<Component> movingNodes, List<Component> refNodes)
        {
            var movingNodeScores = (from n in movingNodes
                                    let i = n.localVariables.IndexOf(-8000)
                                    select n.localVariables[i + 1]);


            var refNodeScores = (from n in refNodes
                                 let i = n.localVariables.IndexOf(-8000)
                                 select n.localVariables[i + 1]);

            return movingNodeScores.Average() + refNodeScores.Average();
        }
    }
}
