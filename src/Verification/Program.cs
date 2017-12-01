using System;
using Assembly_Planner;

namespace Verification
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			string dir = "";
			if (args.Length > 0) {
				dir = args [0];
			}
			return Program.doVerification (dir);
		}
	}
}
