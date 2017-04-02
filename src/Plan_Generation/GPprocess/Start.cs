//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Linq;
//using System.Management.Instrumentation;
//using System.Text;
//using System.Threading.Tasks;
//using RandomGen;
//using StarMathLib;
//using MicrosoftResearch.Infer.Models;
//using MicrosoftResearch.Infer.Maths;
//using MicrosoftResearch.Infer.Distributions;
//using MicrosoftResearch.Infer.Distributions.Kernels;
//using MicrosoftResearch.Infer;
//using AwokeKnowing.GnuplotCSharp;
//using MicrosoftResearch.Infer.Factors;

//namespace GPprocess
//{
//    public class Start
//    {
//        static void Main(string[] args)
//        {
//#region input
//            var range = 7;
//            var num = 5;
//            var testnum = 100;
//            double[] obdataX = GetRandomObData(-range, range, num);
//            double[] obdataY = GetRandomObData(-1, 1, num);
//            //double[] obdataX = {};
//            //double[] obdatay = {};
//            bool[] outputs = { true, true, false, true, false, false };
//            double[] testpoints = CreatTestPoints(-range, range, testnum);
//            Array.Sort(testpoints);
//            Vector[] Xobdatavector = GetXVector(obdataX);
//            Vector[] testpoinvector = CreatTestPointsinArry(testpoints);
//           // Makeplot(obdataX, obdatay);
//#endregion

//            GaussianProcess gp = new GaussianProcess(new ConstantFunction(0), new SquaredExponential(0));
//            Variable<SparseGP> prior = Variable.New<SparseGP>().Named("prior");
//            Variable<SparseGP> post = Variable.New<SparseGP>().Named("post");
//            prior.ObservedValue = new SparseGP(new SparseGPFixed(gp, testpoinvector));
//            post.ObservedValue = new SparseGP(new SparseGPFixed(gp, Xobdatavector));
//            var MeansAndVars = GatMeanAndVar( post, Xobdatavector, testpoinvector, obdataY);
//           // GnuPlot.Plot("cos(2*x)", "with points pt 5");
//            MakeBigPlot(testpoints, MeansAndVars, obdataX, obdataY);
//            Console.ReadKey();
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

//        private static Vector[] CreatTestPointsinArry( double[] testpoints)
//        {
//            Vector[] XtestVectors = new Vector[testpoints.Count()];
            
//            for (var i = 0; i < testpoints.Count(); i++) 
//            {
//                XtestVectors[i] = Vector.FromArray(testpoints[i]);
//            }
//            return XtestVectors;
//        }
   
//        private static Vector[] GetXVector(double[] obdata)
//        {
//            Vector[] XVectors = new Vector[obdata.Count()];
//            for (var i = 0; i < obdata.Count(); i++)
//            {
//                XVectors[i] = Vector.FromArray(obdata[i]);
//            }
//            return XVectors;
//        }

//        private static double[] GetRandomObData(double a , double b, int p)
//        {
//            var obdata = new double[p];
//            for (var i = 0; i < p; i++)
//            {
//                    obdata[i] = Gen.Random.Numbers.Doubles(a, b)();
//            }
//            return obdata;
//        }

//        private static Vector[] AddnewPoint(Vector[] Xobdata, Vector[] testpoints,int index)
//        {
//            Vector[] XobAndTestData = new Vector[Xobdata.Count() + 1];
//            for (var i = 0; i < XobAndTestData.Count(); i++)
//            {
//                if (i < XobAndTestData.Count() - 1)
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

//        private static List<double> GatMeanAndVar( Variable<SparseGP> post, Vector[] Xobdata,Vector[] testpoints,double[] obdatay)
//        {
//            var mean = GetfVactor(obdatay);
//            var cov = GetCovM(post);
//            var MeanAndVar = new List<double>();
//            //cov[0, 1] = Math.Round(cov[0, 14], 8);
//            for (var i = 0; i < testpoints.Count()-1; i++)
//            {
//                GaussianProcess gp = new GaussianProcess(new ConstantFunction(0), new SquaredExponential());
//                var XobAndTestData = AddnewPoint(Xobdata, testpoints, i);
//                Variable<SparseGP> GPXobAndTestData = Variable.New<SparseGP>().Named("post");
//                GPXobAndTestData.ObservedValue = new SparseGP(new SparseGPFixed(gp, XobAndTestData));
//                var newcov = GetCovM(GPXobAndTestData);
//                var kstart = new double[GPXobAndTestData.ObservedValue.Var_B_B.Rows-1, 1];
//                for (var j = 0; j < GPXobAndTestData.ObservedValue.Var_B_B.Rows-1; j++)
//                {
//                    kstart[j, 0] = newcov[j, GPXobAndTestData.ObservedValue.Var_B_B.Cols-1];
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
//            var mean = new double[obdatay.Count(),1];
//            for (var i = 0; i < obdatay.Count(); i++)
//            {
//                mean[i, 0] = obdatay[i];
//            }
//            return mean;
//        }

//        private static double[,] Getmean(Variable<SparseGP> prior)
//        {
//            var mean = new double[prior.ObservedValue.Mean_B.Count,1];
//            var s = prior.ObservedValue.Mean_B[0];
//            for (var i = 0; i < prior.ObservedValue.Mean_B.Count; i++)
//            {
//                mean[i, 0] = prior.ObservedValue.Mean_B[i];
//            }
//            return mean;
//        }

//        private static double[,] GetCovM(Variable<SparseGP> prior)
//        {
//            var arry = prior.ObservedValue.Var_B_B.SourceArray;
//            var M = new double[prior.ObservedValue.Var_B_B.Rows, prior.ObservedValue.Var_B_B.Cols];
//            int pointer = 0;
//            for (var i = 0; i < prior.ObservedValue.Var_B_B.Rows; i++)
//            {
//                for (var j = 0; j < prior.ObservedValue.Var_B_B.Rows; j++)
//                {
//                    M[i, j] = arry[pointer];
//                    pointer++;
//                }
//            }
//            return M;
//        }
//        private static void Makeplot(double[] inputsx,double[] inputsy)
//        {

//            GnuPlot.Plot(inputsx, inputsy);
//        }


//    }
//}


