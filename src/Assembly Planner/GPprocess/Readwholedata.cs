using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;

namespace GPprocess
{
   public static class Readwholedata
    {

       public static void getdata(string alldataaddress, string actionname,out double[,] trainx, out double[] trainy)
       {
           int d;
         var alldata =  readdata.read(alldataaddress, true, out d);
           var trainindex = new List<int>();
           for (int i = 0; i < d; i++)
           {
               trainindex.Add(i);
           }
           trainx = alldata.GetColumns(trainindex);
           trainy = alldata.GetColumn(d);
       }
        public static void getdata(string alldataaddress, string actionname, out double[,] trainx, out double[] trainy, out double[,] testx, out double[] testy, out int dim)
        {
            int numrows;
            var alldata = readdata.read(alldataaddress, true, out numrows);
            var oldindex = new List<int>();
            if (actionname.StartsWith("m"))
            {

                //moving
                oldindex = new List<int>
                {
                    44,
                    19,
                    21,
                    74,
                    73,
                    118,
                    93,
                    95,
                    148,
                    147,
                    192,
                    167,
                    169,
                    222,
                    221,
                    266,
                    241,
                    243,
                    296,
                    295,
                    340,
                    315,
                    317,
                    370,
                    369
                };
            }
            else if (actionname.StartsWith("i"))

                //install
                oldindex = new List<int>

                {

                    10,
                    16,
                    2,
                    32,
                    33,
                    17,
                    48,
                    54,
                    40,
                    70,
                    71,
                    55,
                    86,
                    92,
                    78,
                    108,
                    109,
                    93,
                    124,
                    130,
                    116,
                    146,
                    147,
                    131,
                    162,
                    168,
                    154,
                    184,
                    185,
                    169
                };

            ////rotate
            else if (actionname.StartsWith("r"))
                oldindex = new List<int>
            {
              5,7,12,8,23,33,35,40,36,51,61,63,68,64,79,89,91,96,92,107,117,119,124,120,135
            };
            else
            {
                oldindex = new List<int>
            {
              30,   20, 3,  94, 84, 67, 158,    148,    131 ,222,   212 ,195    ,286,   276 ,259
            };
            }
            var index = new List<int>();
            foreach (var v in oldindex)
            {
                index.Add(v - 1);
            }

            var training = alldata.RemoveRows(index);
            var testing = alldata.GetRows(index);

            var trainingxcolums = new List<int>();
            for (int i = 0; i < training.GetLength(1) - 1; i++)
            {
                trainingxcolums.Add(i);
            }


            trainx = training.GetColumns(trainingxcolums);
            var trainym = training.GetColumns(new List<int> { trainingxcolums.Count });
            testx = testing.GetColumns(trainingxcolums);
            var testym = testing.GetColumns(new List<int> { trainingxcolums.Count });
            trainy = new double[trainym.GetLength(0)];
            testy = new double[testym.GetLength(0)];

            for (int i = 0; i < trainym.GetLength(0); i++)
            {
                trainy[i] = trainym[i, 0];
            }

            for (int i = 0; i < testym.GetLength(0); i++)
            {

                testy[i] = testym[i, 0];
            }
            dim = numrows - 1;

            //ttt
            //    if (alldata.GetLength(1) == 7)
            //    {
            //        trainx = alldata.GetColumns(new List<int> { 0, 1, 2, 3, 4, 5 });
            //        trainy = alldata.GetColumn(alldata.GetLength(1) - 1);
            //    }

            //else
            //    {
            //        trainx = alldata.GetColumns(new List<int> { 0, 1, 2, 3, 4 });
            //        trainy = alldata.GetColumn(alldata.GetLength(1) - 1);
            //    }
        }
    }
}
