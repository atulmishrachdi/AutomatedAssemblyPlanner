using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth.Representation;
using MIConvexHull;

namespace Assembly_Planner
{
    class EvaluationForBinaryTree
    {
        public double EvaluateSub(List<node> subassemblyNodes, List<node> optNodes, out SubAssembly sub)
        {

            var rest = subassemblyNodes.Where(n => !optNodes.Contains(n)).ToList();
            sub = Update(optNodes, rest);
            var install = new[] { rest, optNodes };
            if (Updates.EitherRefOrMovHasSeperatedSubassemblies(install))
                return -1;
            sub.Install.Time = 10;
            return 1;
        }

        public SubAssembly Update(List<node> opt, List<node> rest)
        {
            Part refAssembly, movingAssembly;
            var movingNodes = opt;
            var newSubAsmNodes = rest;
            if (movingNodes.Count == 1)
            {
                var nodeName = movingNodes[0].name;
                movingAssembly = new Part(nodeName, 0, 0, null, null);
            }
            else
                movingAssembly = new SubAssembly(movingNodes, null, 0, 0, null);

            var referenceHyperArcnodes = new List<node>();
                referenceHyperArcnodes = (List<node>) newSubAsmNodes.Where(a => !movingNodes.Contains(a)).ToList();
            if (referenceHyperArcnodes.Count == 1)
            {
                var nodeName = referenceHyperArcnodes[0].name;
                refAssembly = new Part(nodeName, 0, 0,null, null);
            }
            else
                refAssembly = new SubAssembly(referenceHyperArcnodes, null, 0, 0, null);
            var newSubassembly = new SubAssembly(refAssembly, movingAssembly, null, 0,
                null);
            return newSubassembly;
        }
    }
}
