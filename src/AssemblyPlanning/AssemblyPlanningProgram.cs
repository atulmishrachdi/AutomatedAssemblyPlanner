using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assembly_Planner;

namespace AssemblyPlanning
{
    class AssemblyPlanningProgram
    {
        static void Main(string[] args)
        {

			string dir = "";
			if (args.Length > 0) {
				dir = args [0];
			}
            Program.doAssemblyPlanning(dir);

        }
    }
}
