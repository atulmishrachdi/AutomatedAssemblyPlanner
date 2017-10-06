using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Timers;

namespace Assembly_Planner
{
    class LoadingBar
    {

		private static void updateProg(float completion){
			int progVal = (int) (completion * 100);

			bool done = false;
			StreamWriter progStream;
			while (!done) {
				try{
					progStream = new System.IO.StreamWriter("./prog.txt");
					progStream.Write(progVal.ToString());
					progStream.Close();
					done = true;
				}
				catch (Exception){
				}
			}

		}

        public static void start(int width, float completion)
        {
            string bar = "[";
            int pos = 0;
            int loadCutOff = (int) (completion * width);
            while (pos < loadCutOff)
            {
                bar = bar + "|";
                pos++;
            }
            while (pos < width)
            {
                bar = bar + " ";
                pos++;
            }
            bar = bar + "]";
            Console.Write(bar);
        }

        public static void refresh(int width, float completion)
        {
            string bar = "\r[";
            int pos = 0;
            int loadCutOff = (int)(completion * width);
            while (pos < loadCutOff)
            {
                bar = bar + "|";
                pos++;
            }
            while (pos < width)
            {
                bar = bar + " ";
                pos++;
            }
            bar = bar + "]";
			updateProg (completion);
            Console.Write(bar);
        }

    }
}
