using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;

namespace Assembly_Planner
{
    class BoltAndGearUpdateFunctions
    {
        internal static List<int> CrossUpdate(List<int> crossSign)
        {
            var updatedCrossSign = new List<int>(crossSign);
            var count1 = 0;
            var count2 = 0;
            var currentSign = updatedCrossSign[0];
            if (currentSign == 1) count1++;
            else count2++;
            var endOfStream = false;
            var i = 0;
            while (endOfStream == false)
            {
                int startInd, endInd;
                if (i == 0)
                    startInd = i;
                else startInd = i + 1;
                while (OtherSignCountIsZero(currentSign, count1, count2))
                {
                    i++;
                    if (i == updatedCrossSign.Count)
                    {
                        endOfStream = true;
                        break;
                    }
                    if (updatedCrossSign[i] == 1)
                        count1++;
                    else
                        count2++;
                }
                i--;
                endInd = i;
                if (CountCurrentSign(currentSign, count1, count2) > 2)
                    // keep only two
                    for (var j = startInd + 2; j < endInd + 1; j++)
                    {
                        updatedCrossSign.RemoveAt(j);
                        j--;
                        endInd--;
                        i--;
                    }
                currentSign = SwitchTheCurrent(currentSign);
                count1 = 0;
                count2 = 0;
            }
            return updatedCrossSign;
        }


        private static int SwitchTheCurrent(int currentSign)
        {
            return currentSign == 1 ? -1 : 1;
        }

        private static int CountCurrentSign(int currentSign, int count1, int count2)
        {
            return currentSign == 1 ? count1 : count2;
        }

        private static bool OtherSignCountIsZero(int currentSign, int count1, int count2)
        {
            if (currentSign == 1)
            {
                if (count2 == 0) 
                    return true;
            }
            else
            {
                if (count1 == 0) 
                    return true;
            }
            return false;
        }

    }
}
