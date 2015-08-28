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

        internal static List<int> ConvertCrossToSign(List<double[]> crossP)
        {
            var signs = new List<int> { 1 };
            var mainCross = crossP[0];
            for (var i = 1; i < crossP.Count; i++)
            {
                var cross2 = crossP[i];
                if ((Math.Sign(mainCross[0]) != Math.Sign(cross2[0]) ||
                     (Math.Sign(mainCross[0]) == 0 && Math.Sign(cross2[0]) == 0)) &&
                    (Math.Sign(mainCross[1]) != Math.Sign(cross2[1]) ||
                     (Math.Sign(mainCross[1]) == 0 && Math.Sign(cross2[1]) == 0)) &&
                    (Math.Sign(mainCross[2]) != Math.Sign(cross2[2]) ||
                     (Math.Sign(mainCross[2]) == 0 && Math.Sign(cross2[2]) == 0)))
                    signs.Add(-1);
                else
                    signs.Add(1);
            }
            return signs;
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
