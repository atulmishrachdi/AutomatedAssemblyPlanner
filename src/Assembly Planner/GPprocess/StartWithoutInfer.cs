//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Management.Instrumentation;
//using System.Text;
//using System.Threading.Tasks;
//using RandomGen;
//using StarMathLib;
//using AwokeKnowing.GnuplotCSharp;
//using OptimizationToolbox;

//namespace GPprocess
//{
//    public class StartWithoutInfer
//    {
//        static void Main(string[] args)
//        {
//            #region input

//            var range1 = 100;
//            var range2 = 100;
//            var num = 10;
//            var num2 = 10;
//            var testnum = 1;
//            double[] obdataX = GetRandomObData(0, range1, num);
//            double[] obdataY = GetRandomObData(0, 1, num2);
//            double[] obdataY = FunctionOfX(obdataX);
//            double[,] TwoDobdataX = GetTwoDobdataX(0, range1, 0, range2, num2);

//            Makeplot(obdataX, obdataY);
//            MakeThreeDplot(TwoDobdataX, obdataY);

//            bool[] outputs = { true, true, false, true, false, false };
//            double[] testpoints = CreatTestPoints(0, range1, testnum);
//            Array.Sort(testpoints);
//            double L = 1.5;
//            double sef = 1.05;
//            #endregion
//            var MeansAndVars = GatMeanAndVar(obdataX, obdataY, testpoints, L, sef);
//            var opty = new HillClimbing();

//            MakeBigPlot(testpoints, MeansAndVars, obdataX, obdataY);
//            Console.ReadKey();
//        }

//        private static void MakeThreeDplot(double[,] TwoDobdataX, double[] obdataY)
//        {
//            var a = TwoDobdataX.GetLength(1);
//            var x1 = new double[a];
//            var x2 = new double[a];
//            for (int i = 0; i < a; i++)
//            {
//                x1[i] = TwoDobdataX[0, i];
//                x2[i] = TwoDobdataX[1, i];
//            }

//            GnuPlot.Set("dgrid3d 40,40,2");
//            GnuPlot.Set("Contour");
//            GnuPlot.SPlot(x1, x2, obdataY, "with points pt 2");
//            GnuPlot.SPlot(x1, x2, obdataY, "with pm3d");




//        }

//        private static double[,] GetTwoDobdataX(int p1, int range1, int p2, int range2, int num2)
//        {
//            double[,] twodx = new double[2, num2];
//            for (int j = 0; j < 2; j++)
//            {
//                double[] x = GetRandomObData(0, range1, num2);
//                for (int i = 0; i < x.Count(); i++)
//                {
//                    twodx[j, i] = x[i];
//                }
//            }
//            return twodx;
//        }

//        private static double[] FunctionOfX(double[] obdataX)
//        {
//            var y = new double[obdataX.Count()];
//            for (int i = 0; i < obdataX.Count(); i++)
//            {
//                y[i] = obdataX[i] * Math.Cos(obdataX[i]);
//            }
//            return y;
//        }

//        private static double[,] GetCovMatrix(double[] obdataX, double L, double sef)
//        {
//            var CovM = SEKernel(obdataX, L, sef);
//            return CovM;
//        }

//        private static double[,] SEKernel(double[] obdataX, double L, double sef)
//        {
//            var CovM = new double[obdataX.Count(), obdataX.Count()];
//            for (var i = 0; i < obdataX.Count(); i++)
//            {
//                for (var j = 0; j < obdataX.Count(); j++)
//                {
//                    var a = -((Math.Pow((obdataX[i] - obdataX[j]), 2)) / (2 * Math.Pow(L, 2)));
//                    CovM[i, j] = Math.Pow(sef, 2)
//                    * Math.Pow(Math.E, -((Math.Pow((obdataX[i] - obdataX[j]), 2)) / (2 * Math.Pow(L, 2))));
//                }
//            }
//            return CovM;
//        }

//        private static void MakeBigPlot(double[] testpoints, List<double> MeansAndVars, double[] obdataX, double[] obdataY)
//        {
//            var XandUper = new double[testpoints.Count()];
//            var counterx = 0;
//            for (var i = 0; i < MeansAndVars.Count(); i++)
//            {
//                if (i % 2 == 0)
//                {
//                    continue;
//                }
//                else
//                {
//                    var a = MeansAndVars[i - 1] + MeansAndVars[i];
//                    XandUper[counterx] = a;
//                    counterx++;
//                }
//            }
//            var XandLower = new double[testpoints.Count()];
//            var counterxt = 0;
//            for (var i = 0; i < MeansAndVars.Count(); i++)
//            {
//                if (i % 2 == 0)
//                {
//                    continue;
//                }
//                else
//                {
//                    var a = MeansAndVars[i - 1] - MeansAndVars[i];
//                    XandLower[counterxt] = a;
//                    counterxt++;
//                }
//            }
//            var mean = new double[testpoints.Count()];
//            var countermean = 0;
//            for (var i = 0; i < MeansAndVars.Count(); i++)
//            {
//                if (i % 2 == 1)
//                {
//                    continue;
//                }
//                else
//                {
//                    mean[countermean] = MeansAndVars[i];
//                    countermean++;
//                }
//            }
//            GnuPlot.HoldOn();
//            GnuPlot.Plot(obdataX, obdataY, "title'ObsData'with points pt 2");
//            GnuPlot.Plot(testpoints, XandUper, "title'Upper'with linespoints pt " + (int)PointStyles.Dot);
//            GnuPlot.Plot(testpoints, XandLower, "title'Lower'with linespoints pt " + (int)PointStyles.Dot);
//            GnuPlot.Plot(testpoints, mean, "title'Mean'with linespoints pt " + (int)PointStyles.Dot);
//        }

//        private static double[] GetRandomObData(double a, double b, int p)
//        {
//            var obdata = new double[p];
//            for (var i = 0; i < p; i++)
//            {
//                obdata[i] = Gen.Random.Numbers.Doubles(a, b)();
//            }
//            return obdata;
//        }

//        private static double[] AddnewPoint(double[] Xobdata, double[] testpoints, int index)
//        {
//            double[] XobAndTestData = new double[Xobdata.Count() + 1];
//            for (var i = 0; i < Xobdata.Count() + 1; i++)
//            {
//                if (i < Xobdata.Count())
//                    XobAndTestData[i] = Xobdata[i];
//                else
//                {
//                    XobAndTestData[i] = testpoints[index];
//                }
//            }
//            return XobAndTestData;
//        }

//        private static double[] CreatTestPoints(int start, int end, int Num)
//        {

//            var Xpoints = new double[Num];
//            for (var i = 0; i < Num; i++)
//            {
//                Xpoints[i] = Gen.Random.Numbers.Doubles(start, end)();
//            }
//            return Xpoints;
//        }

//        private static List<double> GatMeanAndVar(double[] Xobdata, double[] obdatay, double[] testpoints, double L, double sef)
//        {
//            var mean = GetfVactor(obdatay);
//            var cov = GetCovMatrix(Xobdata, L, sef);
//            var MeanAndVar = new List<double>();
//            cov[0, 1] = Math.Round(cov[0, 14], 8);
//            for (var i = 0; i < testpoints.Count() - 1; i++)
//            {
//                var XobAndTestData = AddnewPoint(Xobdata, testpoints, i);
//                var newcov = GetCovMatrix(XobAndTestData, L, sef);
//                var kstart = new double[newcov.GetLength(0) - 1, 1];
//                for (var j = 0; j < newcov.GetLength(0) - 1; j++)
//                {
//                    kstart[j, 0] = newcov[j, newcov.GetLength(0) - 1];
//                }
//                var Cholcov = StarMath.CholeskyDecomposition(cov);
//                var kstartT = kstart.transpose();
//                var kInv = cov.inverse();
//                var MeanStart = (kstartT.multiply(kInv)).multiply(mean);
//                var VarStart = (((kstartT.multiply(kInv)).multiply(kstart)).multiply(-1));
//                MeanAndVar.Add(MeanStart[0, 0]);
//                MeanAndVar.Add(VarStart[0, 0] + 1);
//            }
//            return MeanAndVar;
//        }

//        private static double[,] GetfVactor(double[] obdatay)
//        {
//            var mean = new double[obdatay.Count(), 1];
//            for (var i = 0; i < obdatay.Count(); i++)
//            {
//                mean[i, 0] = obdatay[i];
//            }
//            return mean;
//        }

//        private static void Makeplot(double[] inputsx, double[] inputsy)
//        {
//            Array.Sort(inputsx);
//            Array.Sort(inputsy);
//            GnuPlot.Plot(inputsx, inputsy, "with linespoints pt" + (int)PointStyles.Dot);
//        }


//    }
//}


