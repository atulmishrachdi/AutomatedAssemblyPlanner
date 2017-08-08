using System;
using Assembly_Planner;

namespace Verification
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			string dir = "";
			if (args.Length > 0) {
				dir = args [0];
			}
			Program.doVerification (dir);
		}
	}
}
