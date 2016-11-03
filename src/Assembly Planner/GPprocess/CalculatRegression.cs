using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPprocess
{
    class  CalculatRegression
    {
        private static double[] movingweight =
        {
            1.016361135,
            -0.584462571,
            0.209798668,
            1.460358848,
            -1.412666773,
              -1.288718311,
            0.153408805,
            -0.386709758,
            0.276277052,
            0.032513469,
            -0.014051969,
            0.204853189
        };

        private static double[] installweight =
        {
            -9.17139980010201,
            -0.503597236495276,
            3.50695617833512,
            1.78582579930212,
            -6.08057589485667,
            0.344586866037611,
            -5.65953164912241,
            -0.344917027957168,
            0.205949205618400,
            -0.827271699261813,
            1.38694355688234,
            -0.280364072214296,
            1.04935544972630,
            -0.651193478221604,
            -1.21087369996098,
            0.384549255139288,
            0.175246015872497,
            0.645293325166855,
            -0.952383608478953,
            0.0389265928743906,
            0.572979486520538
        };

        private static double[] secureweight =
        {
            15.3332273587178,
            -0.212066628274170,
            10.1090581369420,
            -2.80989477379666,
            2.23734356624945,
            -14.9677942250380,
            0.403134153075178,
            -1.38016917376490,
            -1.32997539916049,
            0.976161173782503,
            0.283276316912916,
            -2.01474098782726,
            9.18820741172387,
            0.344503351211377,
            -3.70708877649565,
            -0.408607675810167,
            -0.976500959152834,
            0.769052655682105,
            2.38319887863486,
        };

        private static double[] rotateweight = new double[12] // rotate
        {
            164.214358286265,
              3.93732466605972,
            -57.3656560033560,
               -5.76726730316074,
            63.9020111342255,
            0.799427427130216,
            2.03992483929465,
            -11.6604128278165,
            -0.835527160390534,
            5.05411832519214,
            -1.15936936610912,
            7.02266106378600
        };


        public static double[,] gety(double[,] MDXobdata, string s)
        {
            var m = new double[MDXobdata.GetLength(0), 1];
            double[] regx;
            double[] weight;
            for (int j = 0; j < MDXobdata.GetLength(0); j++)
            {
                if (s.StartsWith("m"))
                {
                    weight = movingweight;
                    regx = new double[12] //// moving 
                    {
                          1,
                        MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                          MDXobdata[j, 5-1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 3 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 3 - 1]*MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1]*MDXobdata[j, 5 - 1]
                    };
                }
                else if (s.StartsWith("i"))
                {
                    weight = installweight;
                    regx = new double[21] //// install 
                    {
                        1,
                        MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1],
                        MDXobdata[j, 6 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 2 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 3 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 4 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 3 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 5 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 1 - 1],
                        MDXobdata[j, 3 - 1]*MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 6 - 1]*MDXobdata[j, 6 - 1],
                    };
                }
                else if (s.StartsWith("s"))
                {
                    weight = secureweight;
                    regx = new double[19] //// install 
                    {
                        1,   
                        MDXobdata[j, 1- 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1],          
                        MDXobdata[j, 6 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 2 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 3 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 4 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 5 - 1],
                          MDXobdata[j, 3 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 3 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 5 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 2 - 1]
                    };
                }
                else
                {
                    weight = rotateweight;
                    regx = new double[12] //// rotate 
                    {
                        1,
                        MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 3 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 4 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1]*MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 4 - 1],
                    };
                }
                     

                for (int i = 0; i < weight.Length; i++)
                {
                    m[j, 0] = m[j, 0] + (regx[i]*weight[i]);
                }
            }
            return m;
        }

        public static double[,] gety(double[,] MDXobdata, int index, string s)
        {
            var m = 0.0;
            double[] regx;
            double[] weight;
            for (int j = index; j <=index; j++)
            {
                if (s.StartsWith("m"))
                {
                    weight = movingweight;
                    regx = new double[12] //// moving 
                    {
                          1,
                        MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                          MDXobdata[j, 5-1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 3 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 3 - 1]*MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1]*MDXobdata[j, 5 - 1]
                    };
                }
                else if (s.StartsWith("i"))
                {
                    weight = installweight;
                    regx = new double[21] //// install 
                    {
                        1,
                        MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1],
                        MDXobdata[j, 6 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 2 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 3 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 4 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 3 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 5 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 1 - 1],
                        MDXobdata[j, 3 - 1]*MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 6 - 1]*MDXobdata[j, 6 - 1],
                    };
                }
                else if (s.StartsWith("s"))
                {
                    weight = secureweight;
                    regx = new double[19] //// install 
                    {
                        1,   
                        MDXobdata[j, 1- 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1],          
                        MDXobdata[j, 6 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 2 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 3 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 4 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 5 - 1],
                          MDXobdata[j, 3 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 3 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 5 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 5 - 1]*MDXobdata[j, 6 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 2 - 1]
                    };
                }
                else
                {
                    weight = rotateweight;
                    regx = new double[12] //// rotate 
                    {
                        1,
                        MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 3 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 4 - 1],
                        MDXobdata[j, 1 - 1]*MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1]*MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1]*MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1]*MDXobdata[j, 4 - 1],
                    };
                }

                for (int i = 0; i < weight.Length; i++)
                {
                    m = m + (regx[i]*weight[i]);
                }
            }
            double[,] mean = new double[1, 1];
            mean[0, 0] = m;
            return mean;
        }
    }
}
