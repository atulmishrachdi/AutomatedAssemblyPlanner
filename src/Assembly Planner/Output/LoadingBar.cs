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

        public static void start(int width, float completion)
        {
            string bar = "\n[";
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
            Console.Write(bar);
        }

    }
}
