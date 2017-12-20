using Assembly_Planner;

namespace AssemblyPlanning
{
    class AssemblyPlanSearchProgram
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
