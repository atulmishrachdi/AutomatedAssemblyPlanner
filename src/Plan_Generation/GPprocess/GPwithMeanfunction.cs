using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;
using RandomGen;
using StarMathLib;
using OptimizationToolbox;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Assembly_Planner;


namespace GPprocess
{
    public class GPwithMeanfunction
    {
        #region 1d

        //static void Main(string[] args)
        //{

        //    //   double[,] t = readdata.read("D:\\Desktop\\testdata\\xforassembly.csv", 2);
        //    //    double[] befornorobdataY = readdata.read("D:\\Desktop\\testdata\\y.txt");//20 data

        //    var dis = 10000;
        //    var ranges = new double[] 
        //    {
        //    0,5,0,5,0,10
        //    };// range = # of dim + 2
        //    double range1 = -2;
        //    double range2 = 12;
        //    var testnum = 200;

        //    ////////////////////


        //    double[] obdataX = readdata.read("../../../../x2.csv");
        //    var obdataY = readdata.read("../../../../y2.csv");
        //    // double[] obdataX = GetRandomObData(0, 2, 5);
        //    //double[] obdataY = FunctionOfX(obdataX, true, 0.2, false, 1, 0.1);


        //    double[] btestpoints = CreatTestPoints(range1, range2, testnum);
        //    Array.Sort(btestpoints);
        //    double[] testpointMeandSD = GetMeanandSD(btestpoints);
        //    double[] testpoints = CreatTestPoints(range1, range2, testnum);
        //    Array.Sort(testpoints);





        //    var samples = GetLHCsamples(ranges, dis);
        //    //var OptimPara = new double[ranges.Count() / 2];
        //    var OptimPara = new double[3] { 5, 2, 1 };
        //    var para = new double[4];// Order: sixf,sixl,noise
        //    var fv = 1000000000000.0;
        //    //for (var i = 0; i < dis; i++)
        //    //{
        //    //    para = samples.GetRow(i);
        //    //    //    para = new double[3] { 100000, 0.2840, -5.8818 };//matlab test
        //    //    //var CandidatePara = Opt(obdataX, obdataY, para,);
        //    //    var CandidatePara = Opt(obdataX, obdataY, para);
        //    //    if (CandidatePara[ranges.Count() / 2] < fv)
        //    //    {
        //    //        fv = CandidatePara[CandidatePara.GetLength(0) - 1];
        //    //        OptimPara[0] = CandidatePara[0];
        //    //        OptimPara[1] = CandidatePara[1];
        //    //        OptimPara[2] = CandidatePara[2];
        //    //        Console.WriteLine("f={0}", fv);
        //    //        for (var j = 0; j < OptimPara.GetLength(0); j++)
        //    //            Console.WriteLine("n={0}", OptimPara[j]);
        //    //    }
        //    //}
        //    // var OptimPara = new double[3] { 3, 0.2, -1 }; ;
        //    //var MeansAndVars = NewOneDGetMeanAndVar(obdataX, obdataY, testpoints, OptimPara, 0.1);

        //    var OtherMeansAndVars = OneDGetMeanAndVar(obdataX, obdataY, testpoints, OptimPara);//it turns out they are the same

        //    var mnv = new double[testnum * 2];
        //    for (var kk = 0; kk != testnum * 2; kk++)
        //    {
        //        mnv[kk] = OtherMeansAndVars[kk];
        //    }


        //    // var OptimPara = new double[2] {1.19, 0.28};
        //    // var MeansAndVars = GetMeanAndVar(obdataX, obdataY, testpoints, OptimPara, noise);
        //    //     //var Ytruenormal = Normalize(Ytrue);
        //    //     var comparelist = Compareresult(Ytrue, MeansAndVars);
        //    //     var meanerror = getmeanerror(comparelist);
        //    // var predicMean = Getmean(MeansAndVars);
        //    //     var predicVar = Getvar(MeansAndVars);
        //    //     var matlab = new MLApp.MLApp();
        //    MakeBigPlot(testpoints, mnv, obdataX, obdataY
        //        //  , numofoffset, obdataY
        //            );
        //    Console.ReadKey();
        //}



        #endregion
        #region Mult D

        public static string actionname;
        public static int iterations = 0;
        //static void mMain(string[] args)
        //{
        //    DateTime beforDT = System.DateTime.Now;
        //    string trainx = "../../../../installdata/dataset2/TrainX.csv";
        //    string trainy = "../../../../installdata/dataset2/TrainY.csv";
        //    string testx = "../../../../installdata/dataset2/TestX.csv";
        //    string testy = "../../../../installdata/dataset2/TestY.csv";
        //    //string trainx = "../../../../rotatedata/dataset1/TrainX.csv";
        //    //string trainy = "../../../../rotatedata/dataset1/TrainY.csv";
        //    //string testx = "../../../../rotatedata/dataset1/TestX.csv";
        //    //string testy = "../../../../rotatedata/dataset1/TestY.csv";
        //    bool isrotatedata = false;
        //    bool allstop = false;
        //    if (testy == "../../../../rotatedata/dataset1/TestY.csv")
        //    {
        //        isrotatedata = true;
        //    }
        //    int dim = 0;
        //    //  double[,] obdataX = readdata.read(trainx, true, out dim);
        //    // var obdataY = readdata.read(trainy);
        //    // var testpoints = readdata.read(testx, true, out dim);
        //    // var Ytrueb = readdata.read(testy);

        //    double[,] obdataX;
        //    double[,] testpoints;

        //    double[] obdataY;
        //    double[] Ytrueb;
        //    //var address = "../../../../GPdata/JMDinstall7log - Copy.csv";
        //    //actionname = "instal";

        //    //var address = "../../../../GPdata/JMDmoving6log.csv";
        //    //actionname = "moving";

        //    //var address = "../../../../GPdata/JMDsecurereorder.csv";
        //    //actionname = "secure";

        //    var address = "../../../../GPdata/JMDrotatelog.csv";
        //    actionname = "rotate";

        //    readdata.getdata(address, actionname, out obdataX, out obdataY, out testpoints, out Ytrueb, out dim);
        //    dim++;//beta
        //    if (testy == "../../../../rotatedata/dataset1/TestY.csv")
        //    {
        //        dim = dim - 5;
        //        isrotatedata = true;
        //    }
        //    var lowrange = 0;
        //    var uprange = 1;

        //    var ranges = new double[dim * 2 + 4];
        //    for (var i = 0; i <= dim * 2 + 2; i = i + 2)
        //    {
        //        ranges[i] = lowrange;
        //        ranges[i + 1] = uprange;
        //    }

        //    var improve = true;
        //    var foundbestranges = false;
        //    ////
        //    var uperbound = 1.0;
        //    var bestf = double.PositiveInfinity;
        //    var error = double.PositiveInfinity;
        //    var fv = double.PositiveInfinity;
        //    var fvini = double.PositiveInfinity;
        //    var bests = new double[ranges.Count() / 2];
        //    var LHCsize = 50;
        //    var learnrate = 1.1;
        //    var defaulttrytime = 2;
        //    var totaltry = 0;
        //    var endcount = 0;
        //    while (foundbestranges == false)
        //    {
        //        var minf = double.PositiveInfinity;
        //        if (improve == false)
        //        {
        //            ranges = bests;
        //        }

        //        endcount = endcount + 1;
        //        do
        //        {
        //            if (isrotatedata)
        //            {
        //                obdataX = readdata.read(trainx, true, out dim);
        //                var beforedim = dim;
        //                dim = dim - 5;
        //                var beforeangleindxs = new List<int> { 4, 5, 6 };
        //                var afterangleindxs = new List<int> { 7, 8, 9 };
        //                var beforeangles = obdataX.GetColumns(beforeangleindxs);
        //                var afterangles = obdataX.GetColumns(afterangleindxs);
        //                var angles = new double[obdataX.GetLength(0), 1];


        //                var anglebetween = new double[obdataX.GetLength(0)];

        //                for (var i = 0; i < obdataX.GetLength(0); i++)
        //                {
        //                    var beforeangle = beforeangles.GetRow(i);
        //                    var afterangle = afterangles.GetRow(i);
        //                    var a = beforeangle.dotProduct(afterangle);
        //                    var nbef = StarMath.norm1(beforeangle);
        //                    var naf = StarMath.norm1(beforeangle);
        //                    angles[i, 0] = a / (nbef * naf);
        //                }
        //                var angleindxs = new List<int> { 4, 5, 6, 7, 8, 9 };
        //                obdataX = obdataX.RemoveColumns(angleindxs);
        //                //   obdataX.JoinMatrixColumnsIntoVector
        //            }
        //            if (improve == false)
        //            {
        //                testpoints = readdata.read(testx, true, out dim);

        //                obdataX = readdata.read(trainx, true, out dim);
        //                dim++;
        //                obdataY = readdata.read(trainy);
        //            }
        //            if (improve == false)
        //            {
        //                LHCsize = 1000;
        //            }
        //            // ranges[ranges.Length - 3] = 3;
        //            var samples = GetLHCsamples(ranges, LHCsize);
        //            var OptimPara = new double[2 + dim];
        //            var para = new double[2 + dim];// Order: sixf,sixl,noise
        //            for (var i = 0; i < LHCsize; i++)
        //            {
        //                para = samples.GetRow(i);
        //                var CandidatePara = Opt(obdataX, obdataY, para);
        //                if (CandidatePara[dim + 2] < fv)
        //                {
        //                    fv = CandidatePara[CandidatePara.GetLength(0) - 1];
        //                    for (int k = 0; k != dim + 2; k++)
        //                    {
        //                        OptimPara[k] = CandidatePara[k];
        //                    }

        //                    foreach (var c in OptimPara)
        //                    {
        //                        var d = Convert.ToDouble(c).ToString("0.00");
        //                        Console.Write("{0},", d);
        //                    }
        //                    Console.WriteLine("f={0:F3}", fv);
        //                }
        //                Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b");
        //                Console.Write(" {0}%", 100 * (i + 1) / LHCsize);
        //            }
        //            DateTime afterDT = System.DateTime.Now;
        //            TimeSpan ts = afterDT.Subtract(beforDT);
        //            Console.WriteLine("DateTime总共花费{0}s.", ts.TotalMilliseconds / 1000);
        //            Console.ReadKey();
        //            var newm = newMDGetMeanAndVar(obdataX, obdataY, testpoints, OptimPara, actionname);
        //            var predicMean = Getmean(newm);
        //            var predicVarlog = GetSD(newm);
        //            var err = 0.0;
        //            for (int i = 0; i < predicMean.Count(); i++)
        //            {
        //                err = err + Math.Abs(Ytrueb[i] - predicMean[i]) * Math.Abs(Ytrueb[i] - predicMean[i]);
        //            }
        //            err = err / predicMean.Count();
        //            Console.Beep(500, 5000);
        //            Console.WriteLine();
        //            Console.WriteLine("meanErr={0}", err);
        //            if (improve == false)
        //            {
        //                foundbestranges = true;
        //                break;
        //            }
        //            if (fv <= minf)
        //            {
        //                minf = fv;
        //                totaltry = 0;
        //                bests = ranges;
        //                bestf = fv;
        //                fv = fvini;
        //                ranges = ranges.multiply(learnrate);
        //                continue;
        //            }
        //            else
        //            {
        //                if (totaltry < defaulttrytime)
        //                {
        //                    totaltry++;
        //                    continue;
        //                }
        //                else
        //                {
        //                    fv = double.PositiveInfinity;
        //                    improve = false;
        //                    allstop = true;
        //                }

        //            }
        //        }
        //        while (improve == true);
        //    }
        //    foundbestranges = true;
        //}


        #endregion
        private static void GetKfoldandRestdata(double[,] restfoldobdataX, double[] restfoldobdataY, double[,] obdataX, double[] obdataY, int numfold, out double[,] oobx, out double[] ooby, out double[,] rx, out double[] ry)
        {
            var index = GetRandomInt(0, obdataY.Count() - 1, numfold);



            restfoldobdataX = obdataX.GetRows(index);

            int cc = 0;
            foreach (var i in index)
            {

                restfoldobdataY[cc] = obdataY[i];
                cc++;
            }

            obdataX = obdataX.RemoveRows(index);
            obdataY = obdataY.RemoveVectorCells(index);
            oobx = obdataX;
            ooby = obdataY;
            rx = restfoldobdataX;
            ry = restfoldobdataY;
        }
        private static double[] Opt(double[,] obdataX, double[] obdataY, double[] para)
        {
            var optpara = new Optimization().Run(obdataX, obdataY, para);
            return optpara;
        }
        public static List<double> newMDGetMeanAndVar(double[,] Xobdata, double[] obdatay, double[,] testpoints, double[] para, string actionname)
        {
            double[,] MeanStart;
            var mean = GetfVactor(obdatay);
            var cov = GetCovMatrix(Xobdata, para, true);
            var MeanAndVar = new List<double>();
            var a = testpoints.GetLength(0);
            double variance;
            var h = CalculatRegression.gety(Xobdata, actionname).transpose();
            var hstarts = CalculatRegression.gety(testpoints, actionname);
            int c = Xobdata.GetLength(0) - 1;
            for (var i = 0; i <= a - 1; i++)
            {
                var XobAndTestData = AddnewPoint(Xobdata, testpoints, i);
                var newcov = GetCovMatrix(XobAndTestData, para, true);
                var kstart = new double[newcov.GetLength(0) - 1, 1];
                for (var j = 0; j < newcov.GetLength(0) - 1; j++)
                {
                    kstart[j, 0] = newcov[j, newcov.GetLength(0) - 1];
                }
                var kstartT = kstart.transpose();
                var kInv = cov.inverse();
                //  MeanStart = (kstartT.multiply(kInv)).multiply(mean);
                if (para.Length - Xobdata.GetLength(1) == 2)
                {
                    MeanStart = (kstartT.multiply(kInv)).multiply(mean);
                    var VarStart = (((kstartT.multiply(kInv)).multiply(kstart)).multiply(-1));
                    variance = VarStart[0, 0] + cov[c, c];
                }
                else //with mean fuc
                {
                    var regy = CalculatRegression.gety(testpoints, i, actionname)[0, 0] * para[para.Length - 1];
                    var regym = new double[1, 1];
                    regym[0, 0] = regy;
                    var fb = CalculatRegression.gety(Xobdata, actionname).multiply(para[para.Length - 1]);
                    MeanStart = regym.add((kstartT.multiply(kInv)).multiply(mean.subtract(fb)));
                    var VarStart = (((kstartT.multiply(kInv)).multiply(kstart)).multiply(-1));
                    var variancestar = VarStart[0, 0] + cov[c, c];
                    var hstart = new double[1, 1];
                    hstart[0, 0] = hstarts[i, 0];
                    var r = hstart.subtract((h.multiply(kInv)).multiply(kstart));
                    var hkk = ((h.multiply(kInv)).multiply(h.transpose())).inverse();
                    variance = variancestar + (r.transpose().multiply(hkk))[0, 0];
                }

                MeanAndVar.Add(MeanStart[0, 0]);
                //



                //double sdpow = Math.Pow(sd, 2);
                double sdabsolute = Math.Sqrt(Math.Abs(variance));
                MeanAndVar.Add(sdabsolute);
                //   MeanAndVar.Add(VarStart[0, 0] + 1);
            }
            return MeanAndVar;

        }

        private static double[] NewOneDGetMeanAndVar(double[] obdataX, double[] obdataY, double[] testpoints, double[] OptimPara, double noise)
        {
            var ell = Math.Exp(OptimPara[1]);
            var sf2 = Math.Exp(2 * OptimPara[0]);

            var cov = SEKernel(obdataX, OptimPara, false);
            var liknoise = Math.Exp(2 * OptimPara[2]);
            //var liknoise = Math.Exp(2 * -2.302585092994043);
            var l = Cholesky(cov.divide(liknoise).add(CreatImatrix(cov.GetLength(0))));
            //var l = Cholesky(k.divide(liknoise).add(CreatImatrix(MDXobdata.GetLength(0))));//MD
            // l = l.transpose();
            var y = l.inverse();
            var l1 = y.multiply(obdataY);
            l = l.transpose();
            l = l.inverse();

            var alpha = l.multiply(l1).divide(liknoise);

            var k = new double[obdataX.Count(), testpoints.Count()];
            for (var i = 0; i < obdataX.Count(); i++)
            {
                for (var j = 0; j < testpoints.Count(); j++)
                {
                    var k1 = Math.Pow((obdataX[i] / ell - testpoints[j] / ell), 2);

                    k[i, j] = sf2 * Math.Exp(-k1 / 2);
                }
            }


            k = k.transpose();

            var mean = new double[100, 1];
            //////f=K*alpha
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    for (int kk = 0; kk < 6; kk++)
                    {
                        mean[i, j] += k[i, kk] * alpha[kk];
                    }
                }
            }

            ////////////variance
            l = Cholesky(cov.divide(liknoise).add(CreatImatrix(cov.GetLength(0))));
            var ks = k.transpose();
            var sw = 1 / Math.Sqrt(liknoise);
            var haha = ks.multiply(sw);
            //leftdevie
            var h = l.transpose();
            h = h.inverse();
            h = h.transpose();
            var v = h.multiply(haha);
            //leftdevie
            var vt = StarMath.EltMultiply(v, v);
            var sum = new double[vt.GetLength(1)];
            for (var i = 0; i < vt.GetLength(1); i++)
            {

                sum[i] = StarMath.SumColumn(vt, i);
            }
            var variance = new double[testpoints.Count()];
            var kss = new double[sum.Count()];
            for (var i = 0; i < kss.Count(); i++)
            {
                kss[i] = sf2;
                variance[i] = kss[i] - sum[i];
                variance[i] = Math.Sqrt(variance[i]);
            }

            var meanAndvariances = new double[testpoints.Count() * 2];
            for (var i = 0; i < testpoints.Count(); i++)
            {
                meanAndvariances[i * 2] = mean[i, 0];
                meanAndvariances[i * 2 + 1] = variance[i];
            }
            return meanAndvariances;
        }
        private static double[] NormalizePredY(double[] Ytrueb, double[] YoriginalMeanandSD)
        {
            var Ytrue = new double[Ytrueb.Count()];
            for (int i = 0; i < Ytrueb.Count(); i++)
            {
                Ytrue[i] = (Ytrueb[i] - YoriginalMeanandSD[0]) / YoriginalMeanandSD[1];
            }
            return Ytrue;

        }
        private static double[,] NormalizeNewcommingX(double[,] Ptestpoints, double[,] Xmeanandsd)
        {
            var r = Ptestpoints.GetLength(0);
            var c = Ptestpoints.GetLength(1);
            var Testtrue = new double[r, c];
            for (int j = 0; j < c; j++)
            {
                for (int i = 0; i < r; i++)
                {
                    Testtrue[i, j] = (Ptestpoints[i, j] - Xmeanandsd[0, j]) / Xmeanandsd[1, j];
                }
            }
            return Testtrue;
        }
        private static double[] NormalizeNewcommingX(double[] Ptestpoints, double[] Xmeanandsd)
        {
            var r = Ptestpoints.GetLength(0);
            var Testtrue = new double[r];
            for (int j = 0; j < r; j++)
            {
                Testtrue[j] = (Ptestpoints[j] - Xmeanandsd[0]) / Xmeanandsd[1];
            }
            return Testtrue;
        }

        private static double[,] GetMeanandSD(double[,] bobdataX)
        {
            int l = bobdataX.GetLength(0);
            int dim = bobdataX.GetLength(1);
            var obdataX = bobdataX;
            var arrlist = new double[l];
            var diff = new double[l];
            var MeanAndSD = new double[2, dim];

            for (int d = 0; d < dim; d++)
            {
                for (int i = 0; i < l; i++)
                {
                    arrlist[i] = bobdataX[i, d];
                }
                var sss = arrlist.Sum();
                var mean = arrlist.Sum() / l;
                for (int i = 0; i < l; i++)
                {
                    diff[i] = Math.Pow((arrlist[i] - mean), 2);
                }
                var sd = Math.Sqrt(diff.Sum(x => Convert.ToDouble(x)) / (l - 1));
                MeanAndSD[0, d] = mean;
                MeanAndSD[1, d] = sd;
            }
            return MeanAndSD;
        }
        private static double[] GetMeanandSD(double[] bobdataY)
        {
            int l = bobdataY.Count();
            var arrlist = new double[l];
            var diff = new double[l];
            var MeanAndSD = new double[2];


            for (int i = 0; i < l; i++)
            {
                arrlist[i] = bobdataY[i];
            }
            var mean = arrlist.Sum() / l;
            for (int i = 0; i < l; i++)
            {
                diff[i] = Math.Pow((arrlist[i] - mean), 2);
            }
            var sd = Math.Sqrt(diff.Sum(x => Convert.ToDouble(x)) / (l - 1));
            MeanAndSD[0] = mean;
            MeanAndSD[1] = sd;

            return MeanAndSD;
        }
        private static double[,] Normalize(double[,] bobdataX)
        {
            int l = bobdataX.GetLength(0);
            int dim = bobdataX.GetLength(1);
            var obdataX = bobdataX;
            var arrlist = new double[l];
            var arr = new double[l];
            for (int d = 0; d < dim; d++)
            {
                for (int i = 0; i < l; i++)
                {
                    arrlist[i] = bobdataX[i, d];
                }
                arrlist = Normalize(arrlist);
                for (int i = 0; i < l; i++)
                {
                    obdataX[i, d] = arrlist[i];
                }
            }
            return obdataX;
        }
        public static double[] GetSD(List<double> MeansAndVars)
        {
            var l = MeansAndVars.Count / 2;
            var comlist = new double[l];
            var bothsidesd = new double[l * 2];
            for (int i = 0; i < l; i++)
            {
                comlist[i] = Math.Abs(MeansAndVars[2 * i + 1]);
            }
            return comlist;
        }

        public static double[] Getmean(List<double> MeansAndVars)
        {
            var l = MeansAndVars.Count / 2;
            var comlist = new double[l];
            for (int i = 0; i < l; i++)
            {

                comlist[i] = MeansAndVars[2 * i];
            }
            return comlist;
        }

        private static object getmeanerror(double[,] comparelist)
        {
            var n = comparelist.GetLength(0);
            var diff = new double[comparelist.GetLength(0)];
            for (var i = 0; i < n; i++)
            {
                diff[i] = Math.Abs(comparelist[i, 0] - comparelist[i, 1]);
            }
            double err = diff.Sum() / n;
            return err;
        }

        private static double[,] Compareresult(double[] Ytruenormal, List<double> MeansAndVars)
        {
            var l = Ytruenormal.GetLength(0);
            var comlist = new double[l, 2];
            for (int i = 0; i < l; i++)
            {
                comlist[i, 0] = Ytruenormal[i];
                comlist[i, 1] = MeansAndVars[2 * i];
            }
            return comlist;
        }

        private static double[] Normalize(double[] obdataY)
        {

            var mean = (obdataY.Sum(x => Convert.ToDouble(x))) / obdataY.GetLength(0); ;
            //var range =(max - min);
            var l = obdataY.GetLength(0);
            var diff = new double[l];
            for (int i = 0; i < l; i++)
            {
                diff[i] = Math.Pow((obdataY[i] - mean), 2);

            }
            var sd = Math.Sqrt((diff.Sum() / (obdataY.GetLength(0) - 1)));
            for (int i = 0; i < l; i++)
            {
                obdataY[i] = (obdataY[i] - mean) / sd;

            }
            return obdataY;
        }

        private static void MakeBIGThreeDplot(double[,] obdataX, double[] obdataY, double[,] testpoints, List<double> m)
        {
            var rr = testpoints.GetLength(0);
            var cc = testpoints.GetLength(1);
            var x1test = new double[rr];
            var x2test = new double[rr];
            for (int i = 0; i < rr; i++)
            {
                x1test[i] = testpoints[i, 0];
                x2test[i] = testpoints[i, 1];
            }

            var ytest = new double[rr];
            for (int i = 0; i < rr; i++)
            {
                ytest[i] = m[2 ^ i - 1]; // need to be fixed
            }



            var r = obdataX.GetLength(0);
            var c = obdataX.GetLength(1);
            var x1 = new double[r];
            var x2 = new double[r];
            for (int i = 0; i < r; i++)
            {
                x1[i] = obdataX[i, 0];
                x2[i] = obdataX[i, 1];
            }
        }

        private static double[] FunctionOfXarray(double[,] obdataX, bool setnoise, double noiseSD, bool offset, double offsetSD)
        {
            //// var rows = obdataX.GetLength(0);
            // var colums = obdataX.GetLength(1);
            // //var y = new double[rows];
            // for (int j = 0; j < rows; j++)
            // {


            //     y[j] = obdataX[j, 0] * obdataX[j, 1];
            //   //  y[j] = obdataX[j, 0] + obdataX[j, 1];
            //     //y[i] = x + Math.Pow(x, 2) - Math.Pow(x, 3) * (1 + Gen.Random.Numbers.Doubles(0, 0.3)());
            // }
            // return y;



            var rows = obdataX.GetLength(0);
            var y = new double[rows];
            if (setnoise == false)
            {
                for (int i = 0; i < rows; i++)
                {
                    {
                        // y[i] = (obdataX[i, 0] ) * (obdataX[i, 1] );
                        // y[i] = (obdataX[i, 0]*obdataX[i, 1])*10e-12;
                        //y[i] = obdataX[i, 0] * obdataX[i, 1]+Math.Sin( obdataX[i, 2]);
                        y[i] = (obdataX[i, 0] / 100000) * (obdataX[i, 1] / 100000);
                        // y[i] = obdataX[i, 0] + obdataX[i, 1];
                    }
                }
            }
            else if (setnoise == true && offset == false)
            {
                for (int i = 0; i < rows; i++)
                {

                    double m = obdataX[i, 0] * obdataX[i, 1];
                    // double m = obdataX[i, 0] + obdataX[i, 1];
                    var normals = Gen.Random.Numbers.Doubles().WithNormalDistribution(mean: m, standardDeviation: noiseSD);
                    y[i] = normals();
                }
            }
            else if (setnoise == true && offset == true)
                for (int i = 0; i < rows; i++)
                {
                    if (i >= rows - 2)
                    {
                        var ss = Gen.Random.Numbers.Doubles().WithNormalDistribution(mean: 3, standardDeviation: offsetSD);
                        y[i] = ss();
                    }
                    else
                    {

                        double m = obdataX[i, 0] * obdataX[i, 1];
                        // double m = obdataX[i, 0] + obdataX[i, 1];
                        var normals = Gen.Random.Numbers.Doubles().WithNormalDistribution(mean: m, standardDeviation: noiseSD);
                        y[i] = normals();
                    }
                    //y[i] = x + Math.Pow(x, 2) - Math.Pow(x, 3) * (1 + Gen.Random.Numbers.Doubles(0, 0.3)());
                }

            return y;
        }

        private static double[,] GetLHCsamples(double[] ranges, int dis)
        {
            var LHCindex = creatLHCindex(ranges.Count() / 2, dis);//Return n sameples, m input 
            var dim = 0;
            var LHCsampels = new double[dis, ranges.GetLength(0) / 2];



            for (var r = 0; r < ranges.Count() / 2; r++)
            {

                var c = LHCindex.GetColumn(dim);
                var fm = c.Count();
                var cv = Math.Abs(ranges[2 * r] - ranges[2 * r + 1]) / fm;
                c = c.multiply(cv);
                for (int i = 0; i < c.Count(); i++)
                {
                    c[i] = c[i] + ranges[2 * r];
                }

                var counter = 0;
                foreach (var v in c)
                {
                    LHCsampels[counter, dim] = v;
                    counter++;
                }
                dim++;
                //   LHCindex.add(c);
            }


            //foreach (var range in ranges)
            //{
            //    var c = LHCindex.GetColumn(dim);
            //    var fm = c.Count();
            //    var cv = range / fm;
            //    c = c.multiply(cv);
            //    var counter = 0;
            //    foreach (var v in c)
            //    {
            //        LHCsampels[counter, dim] = v;
            //        counter++;
            //    }
            //    dim++;
            //    //   LHCindex.add(c);
            //}
            return LHCsampels;
        }

        private static object draw(double[,] LHCindex)
        {
            var s = new object();

            foreach (var ss in LHCindex)
            {
                s = ss;
            }
            return s;
        }

        private static double[,] creatLHCindex(int dim, int num)
        {

            var matrix = new double[num, dim];
            var Drawmatrix = new double[num, dim];

            for (var i = 0; i < dim; i++)
            {
                var counter = 0;
                for (var j = 0; j < num; j++)
                {
                    matrix[j, i] = counter;
                    //  Drawmatrix[j, i] = 0;
                    counter++;
                }
            }
            var indices = Enumerable.Range(0, 100);
            // var indices = Enumerable.Range(0, 100).OrderBy(x => Gen.Random.Numbers.Integers(0, 100)());
            for (int d = 0; d < dim; d++)
            {
                var Column = matrix.GetColumn(d);
                for (var i = 0; i < num; i++)
                {
                    var index = 0;
                    if (Column.Count() != 1)
                        index = Gen.Random.Numbers.Integers(0, Column.Count())();
                    var draw = Column[index];
                    Column = Column.RemoveVectorCell(index);
                    Drawmatrix[i, d] = draw;
                }
            }
            return Drawmatrix;
        }

        //private static double[] Opt(double[] obdataX, double[] obdataY, double[] para, double noise)
        //{

        //    var optpara = new Optimization().Run(obdataX, obdataY, para, noise);
        //    return optpara;
        //}

        //private static double[,] GetRandomPara(int num, int dim)
        //{
        //    var para = new double[dim, num];
        //    for (int i = 0; i < dim; i++)
        //    {
        //        for (int j = 0; i < num; j++)
        //        {
        //            para[i, j] = Gen.Random.Numbers.Doubles(0, 1)();
        //        }
        //    }
        //    return para;
        //}

        private static double[] GetRandomYTraningData(double range1, double range2, int num)
        {
            var obdata = new double[num];
            for (var i = 0; i < num; i++)
            {
                obdata[i] = Gen.Random.Numbers.Doubles(range1, range2)();
            }
            return obdata;
        }

    

        private static double[] FunctionOfX(double[] obdataX, bool setnoise, double noiseSD, bool offset, int numofoffset, double offsetSD)
        {
            var y = new double[obdataX.Count()];
            if (setnoise == false)
            {
                for (int i = 0; i < obdataX.Count(); i++)
                {
                    {
                        var x = obdataX[i];
                        //   double m = obdataX[i] * Math.Cos(obdataX[i]);
                        double m = obdataX[i] * 10;
                        y[i] = m;
                    }
                }
            }
            else if (setnoise == true && offset == false)
            {
                for (int i = 0; i < obdataX.Count(); i++)
                {
                    // double m = obdataX[i] * Math.Cos(obdataX[i]);
                    double m = obdataX[i] * 10;
                    var normals = Gen.Random.Numbers.Doubles().WithNormalDistribution(mean: m, standardDeviation: noiseSD);
                    y[i] = normals();
                }
            }
            else if (setnoise == true || offset == true)
                for (int i = 0; i < obdataX.Count(); i++)
                {
                    if (i >= obdataX.Count() - 2)
                    {
                        var ss = Gen.Random.Numbers.Doubles().WithNormalDistribution(mean: 3, standardDeviation: offsetSD);
                        y[i] = ss();
                    }
                    else
                    {
                        var x = obdataX[i];
                        double m = obdataX[i] * Math.Cos(obdataX[i]) + 50;
                        //  double m = obdataX[i] * 10;
                        var normals = Gen.Random.Numbers.Doubles().WithNormalDistribution(mean: m, standardDeviation: noiseSD);
                        y[i] = normals();
                    }
                    //y[i] = x + Math.Pow(x, 2) - Math.Pow(x, 3) * (1 + Gen.Random.Numbers.Doubles(0, 0.3)());
                }
            return y;
        }
        private static double[,] GetCovMatrix(double[] obdataX, double[] para, double noise, bool MatlabSE)// test
        {
            var CovM = SEKernel(obdataX, para, MatlabSE);
            return CovM;
        }
        private static double[,] GetCovMatrix(double[] obdataX, double[] para, double noise)// test
        {
            var CovM = SEKernel(obdataX, para, noise);
            return CovM;
        }
        private static double[,] GetCovMatrix(double[,] obdataX, double[] para, double noise)// test
        {
            var CovM = SEKernel(obdataX, para, noise, true);
            return CovM;
        }
        public static double[,] GetCovMatrix(double[,] obdataX, double[] para, bool MatlabSE)// test
        {
            var CovM = SEKernel(obdataX, para, true);
            return CovM;
        }
        //1dxk
        internal static double[,] SEKernel(double[] obdataX, double[] para, bool MatlabSE)////test matlab
        {
            var CovM = new double[obdataX.Count(), obdataX.Count()];
            var ell = Math.Exp(para[1]);
            var sf = Math.Exp(2 * para[0]);
            //var ell =para[1];
            //var sf = para[0];

            obdataX = obdataX.divide(ell);
            for (var i = 0; i < obdataX.Count(); i++)
            {
                for (var j = 0; j < obdataX.Count(); j++)
                {
                    CovM[i, j] = Math.Pow(((obdataX[i] - obdataX[j])) / ell, 2);

                }
            }
            for (var i = 0; i < obdataX.Count(); i++)
            {
                for (var j = 0; j < obdataX.Count(); j++)
                {
                    CovM[i, j] = sf * Math.Exp(-CovM[i, j] / 2);

                }
            }

            //////////////////////
            // CovM = new double[obdataX.Count(), obdataX.Count()];
            //for (var i = 0; i < obdataX.Count(); i++)
            //{
            //    for (var j = 0; j < obdataX.Count(); j++)
            //    {
            //        var a = -((Math.Pow((obdataX[i] - obdataX[j]), 2)) / (2 * Math.Pow(para[1], 2)));
            //        CovM[i, j] = Math.Pow(para[0], 2)
            //        * Math.Pow(Math.E, -((Math.Pow((obdataX[i] - obdataX[j]), 2)) / (2 * Math.Pow(para[1], 2))));
            //        CovM[i, j] = Math.Pow(para[0], 1)
            //       * Math.Pow(Math.E, -((Math.Pow((obdataX[i] - obdataX[j]), 2)) / (2 * Math.Pow(para[1], 2))));
            //    }
            //}
            var Imatrix = CreatImatrix(obdataX.Count());

            // var noises = Imatrix.multiply(para[2]);
            var noises = Imatrix.multiply(para[2] * para[2]);
            CovM = CovM.add(noises);
            return CovM;
        }
        internal static double[,] SEKernel(double[] obdataX, double[] para, double noise)
        {
            var CovM = new double[obdataX.Count(), obdataX.Count()];
            for (var i = 0; i < obdataX.Count(); i++)
            {
                for (var j = 0; j < obdataX.Count(); j++)
                {
                    var a = -((Math.Pow((obdataX[i] - obdataX[j]), 2)) / (2 * Math.Pow(para[1], 2)));
                    CovM[i, j] = Math.Pow(para[0], 2)
                    * Math.Pow(Math.E, -((Math.Pow((obdataX[i] - obdataX[j]), 2)) / (2 * Math.Pow(para[1], 2))));
                }
            }
            var Imatrix = CreatImatrix(obdataX.Count());

            var noises = Imatrix.multiply(noise * noise);

            CovM = CovM.add(noises);
            return CovM;
        }
        internal static double[,] SEKernel(double[,] obdataX, double[] para, double noise, bool mulitDim)
        {
            var ParaSixf = para[0];
            // var ParaNoise = para[1];
            var removeindx = new int[] { 0 };
            var otherpara = para.RemoveVectorCells(removeindx);
            var size = obdataX.GetLength(0);
            var CovM = new double[size, size];
            int dim = obdataX.GetLength(1);
            for (var i = 0; i < size; i++)
            {
                for (var j = 0; j < size; j++)
                {
                    double a = 0;
                    for (var k = 0; k < dim; k++)
                    {
                        a = a - ((Math.Pow((obdataX[i, k] - obdataX[j, k]), 2)) / (2 * Math.Pow(otherpara[k], 2)));
                        //   a = a - ((Math.Pow((obdataX[i, k] - obdataX[j, k]), 2)) / (2 * Math.Pow(otherpara[k+1], 2)));
                    }
                    var b = Math.Pow(Math.E, a);
                    CovM[i, j] = Math.Pow(ParaSixf, 2) * Math.Pow(Math.E, a);
                }
            }
            var Imatrix = CreatImatrix(obdataX.GetLength(0));
            var noises = CreatmatrixhaveSameEle(noise, size);
            CovM = CovM.add(noises);
            //CovM = CovM.add(Imatrix.multiply(noise * noise));
            return CovM;
        }
        internal static double[,] SEKernel(double[,] obdataX, double[] para, bool matlab)////matlabtest
        {
            //var ell = Math.Exp(para[para.Count() - 2]);
            var ell = para[para.Count() - 2];//JMD
            var sf = Math.Exp(2 * para[0]);
            // var ParaSixf =  para[0];
            var ParaSixf = Math.Exp(2 * para[0]);
            // var ParaNoise = para[1];
            var removeindx = new int[] { 0, para.Count() - 2, para.Count() - 1 };
            var otherpara = para.RemoveVectorCells(removeindx);
            var size = obdataX.GetLength(0);
            var CovM = new double[size, size];
            int dim = obdataX.GetLength(1);

            for (var i = 0; i < size; i++)
            {
                for (var j = 0; j < size; j++)
                {
                    double a = 0;
                    for (var k = 0; k < dim; k++)
                    {
                        a = a - (Math.Pow(((obdataX[i, k] - obdataX[j, k]) / Math.Exp(otherpara[k])), 2) / 2);//with matalb
                        // a = a - (Math.Pow(((obdataX[i, k] - obdataX[j, k]) / otherpara[k]), 2) / 2);
                        //   a = a - ((Math.Pow((obdataX[i, k] - obdataX[j, k]), 2)) / (2 * Math.Pow(otherpara[k+1], 2)));
                    }
                    var b = Math.Pow(Math.E, a);
                    CovM[i, j] = ParaSixf * Math.Exp(a);
                }
            }
            var Imatrix = CreatImatrix(obdataX.GetLength(0));
            CovM = CovM.add(Imatrix.multiply(ell));
            return CovM;
        }
        private static double[,] CreatmatrixhaveSameEle(double p, int size)
        {
            var Imatrix = new double[size, size];

            for (var j = 0; j < size; j++)
            {
                for (var i = 0; i < size; i++)
                {
                    Imatrix[j, i] = p;
                }
            }

            return Imatrix;

        }
        public static double[,] CreatImatrix(int p)
        {
            var Imatrix = new double[p, p];

            for (var i = 0; i < p; i++)
            {
                Imatrix[i, i] = 1;

            }
            return Imatrix;

        }

        //var CovM = new double[obdataX.Count(), obdataX.Count()];
        //   for (var i = 0; i < obdataX.Count(); i++)
        //   {
        //       for (var j = 0; j < obdataX.Count(); j++)
        //       {
        //           var a = -((Math.Pow((obdataX[i] - obdataX[j]), 2)) / (2 * Math.Pow(para[1], 2)));
        //           CovM[i, j] = Math.Pow(para[0], 2)
        //           * Math.Pow(Math.E, -((Math.Pow((obdataX[i] - obdataX[j]), 2)) / (2 * Math.Pow(para[1], 2))));
        //       }
        //   }
        //   var Imatrix = CreatImatrix(obdataX.Count());
        //   CovM = CovM.add(Imatrix.multiply(para[2] * para[2]));
        //   return CovM;
        private static void MakeBigPlot(double[] testpoints, double[] MeansAndVars, double[] obdataX, double[] obdataY
// , int numofoffset, double[] yforplot
///  ,double[] yture
)
        {
            var XandUper = new double[testpoints.Count()];
            var counterx = 0;


            for (var i = 0; i < MeansAndVars.Count(); i++)
            {
                if (i % 2 == 0)
                {
                    continue;
                }
                else
                {
                    var a = MeansAndVars[i - 1] + MeansAndVars[i];
                    XandUper[counterx] = a;
                    counterx++;
                }
            }
            var XandLower = new double[testpoints.Count()];
            var counterxt = 0;
            for (var i = 0; i < MeansAndVars.Count(); i++)
            {
                if (i % 2 == 0)
                {
                    continue;
                }
                else
                {
                    var a = MeansAndVars[i - 1] - MeansAndVars[i];
                    XandLower[counterxt] = a;
                    counterxt++;
                }
            }
            var mean = new double[testpoints.Count()];
            var countermean = 0;
            for (var i = 0; i < MeansAndVars.Count(); i++)
            {
                if (i % 2 == 1)
                {
                    continue;
                }
                else
                {
                    mean[countermean] = MeansAndVars[i];
                    countermean++;
                }
            }
            //var Xoffset = new double[numofoffset];
            //var Yoffset = new double[numofoffset];
            //if (numofoffset != 0)
            //{
            //    for (int i = 0; i < numofoffset; i++)
            //    {
            //        var l = obdataX.GetLength(0);
            //        Xoffset[i] = obdataX[l - 1];
            //        Yoffset[i] = obdataY[l - 1];
            //        obdataX = obdataX.RemoveVectorCell(l - 1);
            //        obdataY = obdataY.RemoveVectorCell(l - 1);
            //    }
            //}



      
        }

        private static double[,] GetRandomXTraningData(double range1, double range2, int num, int dim)
        {
            var obdata = new double[num, dim];
            for (var i = 0; i < num; i++)
            {
                for (var j = 0; j < dim; j++)
                {
                    obdata[i, j] = Gen.Random.Numbers.Doubles(range1, range2)();
                }
            }
            return obdata;

        }

        public static double[,] AddnewPoint(double[,] Xobdata, double[,] testpoints, int index)
        {
            int r = Xobdata.GetLength(0);
            int c = Xobdata.GetLength(1);
            double[,] XobAndTestData = new double[r + 1, c];

            for (var i = 0; i < r + 1; i++)
            {
                for (var j = 0; j < c; j++)
                {
                    if (i < r)
                        XobAndTestData[i, j] = Xobdata[i, j];
                    else
                    {
                        XobAndTestData[i, j] = testpoints[index, j];
                    }
                }
            }
            return XobAndTestData;
        }

        //1D
       
        //for 1D
        public static List<double> OneDGetMeanAndVar(double[] Xobdata, double[] obdatay, double[] testpoints, double[] para)
        {

            bool matlab = true;
            //gpml test
            //para[0] = Math.Exp(para[0] * 2);
            //para[1] = Math.Exp(para[1]);
            //
            var optpara = para;
            var starPara = new double[2];
            var testnoise = GetRandomObData(0.1, 1, 400);
            var noisess = testnoise[0];
            var mean = GetfVactor(obdatay);
            var cov = GetCovMatrix(Xobdata, optpara, para[2], matlab);

            var MeanAndVar = new List<double>();
            for (var i = 0; i < testpoints.Count(); i++)
            {

                var XobAndTestData = AddnewPoint(Xobdata, testpoints, i);
                var newcov = GetCovMatrix(XobAndTestData, optpara, para[2], true);
                var kstart = new double[newcov.GetLength(0) - 1, 1];
                for (var j = 0; j < newcov.GetLength(0) - 1; j++)
                {
                    kstart[j, 0] = newcov[j, newcov.GetLength(0) - 1];
                }
                var Cholcov = StarMath.CholeskyDecomposition(cov);
                var CholcovT = Cholcov.transpose();
                var kstartT = kstart.transpose();
                var kInv = cov.inverse();
                var MeanStart = kstartT.multiply(kInv.multiply(mean));
                var VarStart = kstartT.multiply(kInv.multiply(kstart)).multiply(-1);
                MeanAndVar.Add(MeanStart[0, 0]);
                int c = Xobdata.GetLength(0) - 1;
                double sd = VarStart[0, 0] + cov[c, c];
                // double sdpow = Math.Pow(sd, 2);
                // double sdabsolute = Math.Pow(sdpow, 0.25);
                double sdabsolute = 2 * Math.Abs(sd);
                MeanAndVar.Add(sdabsolute);
                ////matlalb test
                //var testmean = kstart.multiply(worldalpha);
                ///
            }
            return MeanAndVar;
        }
        //1d GetMeanAndVar backup
        //public static List<double> GetMeanAndVar(double[] Xobdata, double[] obdatay, double[] testpoints, double[] para, double noise, double[] worldalpha)
        //{

        //    bool matlab = true;
        //    //gpml test
        //    //para[0] = Math.Exp(para[0] * 2);
        //    //para[1] = Math.Exp(para[1]);
        //    //
        //    var optpara = para;
        //    var starPara = new double[2];
        //    var testnoise = GetRandomObData(0.1, 1, 400);
        //    var noisess = testnoise[0];
        //    var mean = GetfVactor(obdatay);
        //    var cov = GetCovMatrix(Xobdata, optpara, noise, matlab);

        //    ////matlab test
        //    var l = Cholesky(cov.divide(noise).add(CreatImatrix(Xobdata.GetLength(0))));
        //    // l = l.transpose();
        //    var x = l.inverse();
        //    var l1 = x.multiply(obdatay);
        //    l = l.transpose();
        //    l = l.inverse();
        //    var alpha = l.multiply(l1);
        //    /////
        //    var MeanAndVar = new List<double>();
        //    for (var i = 0; i < testpoints.Count(); i++)
        //    {

        //        var XobAndTestData = AddnewPoint(Xobdata, testpoints, i);
        //        var newcov = GetCovMatrix(XobAndTestData, optpara, noise);
        //        var kstart = new double[newcov.GetLength(0) - 1, 1];
        //        for (var j = 0; j < newcov.GetLength(0) - 1; j++)
        //        {
        //            kstart[j, 0] = newcov[j, newcov.GetLength(0) - 1];
        //        }
        //        var Cholcov = StarMath.CholeskyDecomposition(cov);
        //        var CholcovT = Cholcov.transpose();
        //        var kstartT = kstart.transpose();
        //        var kInv = cov.inverse();
        //        var MeanStart = kstartT.multiply(kInv.multiply(mean));
        //        var VarStart = kstartT.multiply(kInv.multiply(kstart)).multiply(-1);
        //        MeanAndVar.Add(MeanStart[0, 0]);
        //        int c = Xobdata.GetLength(0) - 1;
        //        double sd = VarStart[0, 0] + cov[c, c];
        //        double sdpow = Math.Pow(sd, 2);
        //        double sdabsolute = Math.Pow(sdpow, 0.5);
        //        MeanAndVar.Add(sdabsolute);
        //        ////matlalb test
        //        //var testmean = kstart.multiply(worldalpha);
        //        ///
        //    }
        //    return MeanAndVar;
        //}
        private static double[] AddnewPoint(double[] Xobdata, double[] testpoints, int index)
        {
            double[] XobAndTestData = new double[Xobdata.Count() + 1];
            for (var i = 0; i < Xobdata.Count() + 1; i++)
            {
                if (i < Xobdata.Count())
                    XobAndTestData[i] = Xobdata[i];
                else
                {
                    XobAndTestData[i] = testpoints[index];
                }
            }
            return XobAndTestData;
        }
        public static List<double> MDGetMeanAndVar(double[,] Xobdata, double[] obdatay, double[,] testpoints, double[] para, double noise)
        {
            var mean = GetfVactor(obdatay);
            //  var cov = GetCovMatrix(Xobdata, para, noise);
            var cov = GetCovMatrix(Xobdata, para, true);

            var MeanAndVar = new List<double>();
            var a = testpoints.GetLength(0);
            for (var i = 0; i <= a - 1; i++)
            {
                var XobAndTestData = AddnewPoint(Xobdata, testpoints, i);
                var newcov = GetCovMatrix(XobAndTestData, para, true);
                // var newcov = GetCovMatrix(XobAndTestData, para, noise);
                var kstart = new double[newcov.GetLength(0) - 1, 1];
                for (var j = 0; j < newcov.GetLength(0) - 1; j++)
                {
                    kstart[j, 0] = newcov[j, newcov.GetLength(0) - 1];
                }
                var Cholcov = StarMath.CholeskyDecomposition(cov);
                var kstartT = kstart.transpose();
                var kInv = cov.inverse();
                var MeanStart = (kstartT.multiply(kInv)).multiply(mean);
                var VarStart = (((kstartT.multiply(kInv)).multiply(kstart)).multiply(-1));
                MeanAndVar.Add(MeanStart[0, 0]);
                //
                int c = Xobdata.GetLength(0) - 1;
                double sd = VarStart[0, 0] + cov[c, c];
                double sdpow = Math.Pow(sd, 2);
                double sdabsolute = Math.Pow(sdpow, 0.25);
                MeanAndVar.Add(sdabsolute);
                //   MeanAndVar.Add(VarStart[0, 0] + 1);
            }
            return MeanAndVar;
        }

        /// MD GetMeanAndVar Backup
        //public static List<double> GetMeanAndVar(double[,] Xobdata, double[] obdatay, double[,] testpoints, double[] para, double noise)
        //{
        //    var mean = GetfVactor(obdatay);
        //    var cov = GetCovMatrix(Xobdata, para, noise);
        //    var MeanAndVar = new List<double>();
        //    var a = testpoints.GetLength(0);
        //    for (var i = 0; i <= a - 1; i++)
        //    {
        //        var XobAndTestData = AddnewPoint(Xobdata, testpoints, i);
        //        var newcov = GetCovMatrix(XobAndTestData, para, noise);
        //        var kstart = new double[newcov.GetLength(0) - 1, 1];
        //        for (var j = 0; j < newcov.GetLength(0) - 1; j++)
        //        {
        //            kstart[j, 0] = newcov[j, newcov.GetLength(0) - 1];
        //        }
        //        var Cholcov = StarMath.CholeskyDecomposition(cov);
        //        var kstartT = kstart.transpose();
        //        var kInv = cov.inverse();
        //        var MeanStart = (kstartT.multiply(kInv)).multiply(mean);
        //        var VarStart = (((kstartT.multiply(kInv)).multiply(kstart)).multiply(-1));
        //        MeanAndVar.Add(MeanStart[0, 0]);
        //        //
        //        int c = Xobdata.GetLength(0) - 1;
        //        double sd = VarStart[0, 0] + cov[c, c];
        //        double sdpow = Math.Pow(sd, 2);
        //        double sdabsolute = Math.Pow(sdpow, 0.5);
        //        MeanAndVar.Add(sdabsolute);
        //        //   MeanAndVar.Add(VarStart[0, 0] + 1);
        //    }
        //    return MeanAndVar;
        //}

        internal static double[,] GetfVactor(double[] obdatay)
        {
            var mean = new double[obdatay.Count(), 1];
            for (var i = 0; i < obdatay.Count(); i++)
            {
                mean[i, 0] = obdatay[i];
            }
            return mean;
        }
        private static double[] GetRandomObData(double a, double b, int p)
        {

            var obdata = new double[p];
            for (var i = 0; i < p; i++)
            {
                obdata[i] = Gen.Random.Numbers.Doubles(a, b)();
            }
            return obdata;

        }
        private static int[] GetRandomInt(int a, int b, int p)
        {
            bool same = true;
            var q = new int[p];
            var random = new int[p];

            while (same == true)
            {

                for (int i = 0; i < p; i++)
                {
                    random[i] = Gen.Random.Numbers.Integers(a, b)();
                }
                q = random.Distinct().ToArray();
                if (q.Count() == random.Count())
                {
                    same = false;
                    break;
                }
                else
                {
                    continue;
                }
            }

            return random;

        }
      
        private double VecToDouble(double[,] p)
        {
            double d = p[0, 0];
            return d;
        }
        public static double[,] Cholesky(double[,] a)
        {
            int n = (int)Math.Sqrt(a.Length);

            double[,] ret = new double[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c <= r; c++)
                {
                    if (c == r)
                    {
                        double sum = 0;
                        for (int j = 0; j < c; j++)
                        {
                            sum += ret[c, j] * ret[c, j];
                        }
                        ret[c, c] = Math.Sqrt(a[c, c] - sum);
                    }
                    else
                    {
                        double sum = 0;
                        for (int j = 0; j < c; j++)
                            sum += ret[r, j] * ret[c, j];
                        ret[r, c] = 1.0 / ret[c, c] * (a[r, c] - sum);
                    }
                }

            return ret;
        }
    }
}


