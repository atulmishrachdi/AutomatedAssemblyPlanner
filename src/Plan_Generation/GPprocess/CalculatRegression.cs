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
            //1.016361135,
            //-0.584462571,
            //0.209798668,
            //1.460358848,
            //-1.412666773,
            //  -1.288718311,
            //0.153408805,
            //-0.386709758,
            //0.276277052,
            //0.032513469,
            //-0.014051969,
            //0.204853189
            -2.67827401987612,
0.0532473558699870,
0.196837025430431,
-0.0292396553296314,
-0.243161786009405,
0.468387465128049
        };

        private static double[] installweight =
        {
            //-9.17139980010201,
            //-0.503597236495276,
            //3.50695617833512,
            //1.78582579930212,
            //-6.08057589485667,
            //0.344586866037611,
            //-5.65953164912241,
            //-0.344917027957168,
            //0.205949205618400,
            //-0.827271699261813,
            //1.38694355688234,
            //-0.280364072214296,
            //1.04935544972630,
            //-0.651193478221604,
            //-1.21087369996098,
            //0.384549255139288,
            //0.175246015872497,
            //0.645293325166855,
            //-0.952383608478953,
            //0.0389265928743906,
            //0.572979486520538
            0.686954840965199,
0.115325824735747,
-0.254893087364820,
0.0810138464710337,
0.280387350477842,
0.0802199700491647,
0.420735911460501
        };

        private static double[] secureweight =
        {
            //15.3332273587178,
            //-0.212066628274170,
            //10.1090581369420,
            //-2.80989477379666,
            //2.23734356624945,
            //-14.9677942250380,
            //0.403134153075178,
            //-1.38016917376490,
            //-1.32997539916049,
            //0.976161173782503,
            //0.283276316912916,
            //-2.01474098782726,
            //9.18820741172387,
            //0.344503351211377,
            //-3.70708877649565,
            //-0.408607675810167,
            //-0.976500959152834,
            //0.769052655682105,
            //2.38319887863486,
            2.97679236329902,
0.194924638186007,
0.100839142314783,
0.121011213721419,
-0.155668915056301,
0.114015945254631,
-0.538194795017441
        };

        private static double[] rotateweight = new double[6] // rotate
        {
          0.347830236,
-0.310631973,
-0.051929319,
-0.361529413,
1.151870782,
0.511446286
        };
        internal static double[] gety(double[,] mDXobdata, double[] weight)
        {
            var m = 0.0;

            var y = new double[mDXobdata.GetLength(0)];

            for (int i = 0; i < mDXobdata.GetLength(0); i++)
            {
                m = 0.0;
                for (int j = 0; j < weight.Length; j++)
                {
                    if (j != weight.Length - 1)
                    {
                        m = m + (mDXobdata[i, j] * weight[j]);
                    }
                    else
                    {
                        m = m + weight[j];
                    }

                }
                y[i] = m;
            }
            return y;
        }

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
                    regx = new double[6] //// moving 
                    {
                          1,
                        MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                          MDXobdata[j, 5-1],
                        //MDXobdata[j, 1 - 1]*MDXobdata[j, 5 - 1],
                        //MDXobdata[j, 3 - 1]*MDXobdata[j, 5 - 1],
                        //MDXobdata[j, 4 - 1]*MDXobdata[j, 5 - 1],
                        //MDXobdata[j, 3 - 1]*MDXobdata[j, 3 - 1],
                        //MDXobdata[j, 4 - 1]*MDXobdata[j, 4 - 1],
                        //MDXobdata[j, 5 - 1]*MDXobdata[j, 5 - 1]
                    };
                }
                else if (s.StartsWith("i"))
                {
                    weight = installweight;
                    regx = new double[7] //// install 
                    {
                        1,
                        MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1],
                        MDXobdata[j, 6 - 1],
                        //MDXobdata[j, 1 - 1]*MDXobdata[j, 2 - 1],
                        //MDXobdata[j, 1 - 1]*MDXobdata[j, 5 - 1],
                        //MDXobdata[j, 2 - 1]*MDXobdata[j, 3 - 1],
                        //MDXobdata[j, 2 - 1]*MDXobdata[j, 4 - 1],
                        //MDXobdata[j, 2 - 1]*MDXobdata[j, 5 - 1],
                        //MDXobdata[j, 2 - 1]*MDXobdata[j, 6 - 1],
                        //MDXobdata[j, 3 - 1]*MDXobdata[j, 6 - 1],
                        //MDXobdata[j, 4 - 1]*MDXobdata[j, 6 - 1],
                        //MDXobdata[j, 5 - 1]*MDXobdata[j, 6 - 1],
                        //MDXobdata[j, 1 - 1]*MDXobdata[j, 1 - 1],
                        //MDXobdata[j, 3 - 1]*MDXobdata[j, 3 - 1],
                        //MDXobdata[j, 4 - 1]*MDXobdata[j, 4 - 1],
                        //MDXobdata[j, 5 - 1]*MDXobdata[j, 5 - 1],
                        //MDXobdata[j, 6 - 1]*MDXobdata[j, 6 - 1],
                    };
                }
                else if (s.StartsWith("s"))
                {
                    weight = secureweight;
                    regx = new double[7] //// install 
                    {
                        1,   
                        MDXobdata[j, 1- 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1],          
                        MDXobdata[j, 6 - 1],
                        //MDXobdata[j, 1 - 1]*MDXobdata[j, 2 - 1],
                        //MDXobdata[j, 1 - 1]*MDXobdata[j, 3 - 1],
                        //MDXobdata[j, 1 - 1]*MDXobdata[j, 4 - 1],
                        //MDXobdata[j, 1 - 1]*MDXobdata[j, 6 - 1],
                        //MDXobdata[j, 2 - 1]*MDXobdata[j, 5 - 1],
                        //  MDXobdata[j, 3 - 1]*MDXobdata[j, 5 - 1],
                        //MDXobdata[j, 3 - 1]*MDXobdata[j, 6 - 1],
                        //MDXobdata[j, 4 - 1]*MDXobdata[j, 5 - 1],
                        //MDXobdata[j, 4 - 1]*MDXobdata[j, 6 - 1],
                        //MDXobdata[j, 5 - 1]*MDXobdata[j, 6 - 1],
                        //MDXobdata[j, 1 - 1]*MDXobdata[j, 1 - 1],
                        //MDXobdata[j, 2 - 1]*MDXobdata[j, 2 - 1]
                    };
                }
                else
                {
                    weight = rotateweight;
                    regx = new double[6] //// rotate 
                    {
                        1,
                        MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1],
                       
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
                    regx = new double[6] //// moving 
                   {
                          1,
                        MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                          MDXobdata[j, 5-1],
                       //MDXobdata[j, 1 - 1]*MDXobdata[j, 5 - 1],
                       //MDXobdata[j, 3 - 1]*MDXobdata[j, 5 - 1],
                       //MDXobdata[j, 4 - 1]*MDXobdata[j, 5 - 1],
                       //MDXobdata[j, 3 - 1]*MDXobdata[j, 3 - 1],
                       //MDXobdata[j, 4 - 1]*MDXobdata[j, 4 - 1],
                       //MDXobdata[j, 5 - 1]*MDXobdata[j, 5 - 1]
                   };
                }
                else if (s.StartsWith("i"))
                {
                    weight = installweight;
                    regx = new double[7] //// install 
                   {
                        1,
                        MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1],
                        MDXobdata[j, 6 - 1],
                       //MDXobdata[j, 1 - 1]*MDXobdata[j, 2 - 1],
                       //MDXobdata[j, 1 - 1]*MDXobdata[j, 5 - 1],
                       //MDXobdata[j, 2 - 1]*MDXobdata[j, 3 - 1],
                       //MDXobdata[j, 2 - 1]*MDXobdata[j, 4 - 1],
                       //MDXobdata[j, 2 - 1]*MDXobdata[j, 5 - 1],
                       //MDXobdata[j, 2 - 1]*MDXobdata[j, 6 - 1],
                       //MDXobdata[j, 3 - 1]*MDXobdata[j, 6 - 1],
                       //MDXobdata[j, 4 - 1]*MDXobdata[j, 6 - 1],
                       //MDXobdata[j, 5 - 1]*MDXobdata[j, 6 - 1],
                       //MDXobdata[j, 1 - 1]*MDXobdata[j, 1 - 1],
                       //MDXobdata[j, 3 - 1]*MDXobdata[j, 3 - 1],
                       //MDXobdata[j, 4 - 1]*MDXobdata[j, 4 - 1],
                       //MDXobdata[j, 5 - 1]*MDXobdata[j, 5 - 1],
                       //MDXobdata[j, 6 - 1]*MDXobdata[j, 6 - 1],
                   };
                }
                else if (s.StartsWith("s"))
                {
                    weight = secureweight;
                    regx = new double[7] //// install 
                         {
                        1,
                        MDXobdata[j, 1- 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1],
                        MDXobdata[j, 6 - 1],
                             //MDXobdata[j, 1 - 1]*MDXobdata[j, 2 - 1],
                             //MDXobdata[j, 1 - 1]*MDXobdata[j, 3 - 1],
                             //MDXobdata[j, 1 - 1]*MDXobdata[j, 4 - 1],
                             //MDXobdata[j, 1 - 1]*MDXobdata[j, 6 - 1],
                             //MDXobdata[j, 2 - 1]*MDXobdata[j, 5 - 1],
                             //  MDXobdata[j, 3 - 1]*MDXobdata[j, 5 - 1],
                             //MDXobdata[j, 3 - 1]*MDXobdata[j, 6 - 1],
                             //MDXobdata[j, 4 - 1]*MDXobdata[j, 5 - 1],
                             //MDXobdata[j, 4 - 1]*MDXobdata[j, 6 - 1],
                             //MDXobdata[j, 5 - 1]*MDXobdata[j, 6 - 1],
                             //MDXobdata[j, 1 - 1]*MDXobdata[j, 1 - 1],
                             //MDXobdata[j, 2 - 1]*MDXobdata[j, 2 - 1]
                         };
                }
                else
                {
                    weight = rotateweight;
                    regx = new double[6] //// rotate 
                    {
                        1,
                        MDXobdata[j, 1 - 1],
                        MDXobdata[j, 2 - 1],
                        MDXobdata[j, 3 - 1],
                        MDXobdata[j, 4 - 1],
                        MDXobdata[j, 5 - 1],
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
