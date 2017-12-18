using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Accord.Math;
using RandomGen;
using StarMathLib;
using Accord.Statistics.Analysis;
using Accord.Statistics.Models.Regression.Linear;
using Assembly_Planner;
using Plan_Generation.AssemblyEvaluation;

namespace GPprocess
{
    public class OnlineGPupdating
    {
        public static int LHCsize = 100;
        public static string actionname;
        public static int maximumpointswithincluster =80;
        public static int numberofpartitions = 2;
        public static int numberoftryparting = 500;
        public static int kmeanrun = 0;

        //public static double[] OptimPara = new double[] // install

        //{
        //    0.292,
        //    0.392,
        //    0.171,
        //    0.712,
        //    0.712,
        //    0.612,
        //    0.773,
        //    0.213,
        //    1.053
        //};
        //public static double[] OptimPara = new double[8] //moveing
        //     {
        //            //all
        //            0.8067,
        //            0.26535,
        //            0.6062,
        //            0.42575,
        //            0.98715,
        //            0.90695,
        //            0.0544,
        //            1.10745,
        //     };   
        public static void GPprediction(Dictionary<List<double[]>, List<double[,]>> trainedclusters, double[] feedbackpoint, out List<double> measures, out List<double> means,out List< double> SDs )
        {
           // feedbackpoint = MatrixtoRow (trainedclusters[trainedclusters.Keys.First()][0]);
            var truevalue = feedbackpoint[feedbackpoint.Length - 1];
            var mfeedbackpoint = RowtoMatrix(feedbackpoint);
             measures = new List<double>();
            var preclosestcenter = Getpreclosestcenter(trainedclusters, mfeedbackpoint, out measures);
             means = new List<double>();
             SDs = new List<double>();
            foreach (var trainedcluster in trainedclusters)
            {
                var meanansd =  newMDGetMeanAndVar(trainedcluster, feedbackpoint);
                means.Add(meanansd[0]);
                SDs.Add(meanansd[1]);
            }
           // return allmean;
        }

        //public static List<double> newMDGetMeanAndVar(double[,] Xobdata, double[] obdatay, double[,] testpoints,
        //      double[] para, string actionname, double[] Ytrueb)
        private static void GetXfromValues(List<double[,]> trainedcluster, out double[,] Xobdata, out double[] obdatay)
        {
            obdatay = new double[trainedcluster.Count];
            Xobdata = new double[trainedcluster.Count, trainedcluster[0].GetLength(1)-1];
            for (int i = 0; i < trainedcluster.Count; i++)
            {
                obdatay[i] = trainedcluster[i][0, trainedcluster[0].GetLength(1) - 1];
                for (int j = 0; j < trainedcluster[0].GetLength(1) - 1; j++)
                {
                    Xobdata[i, j] = trainedcluster[i][0, j];
                }
            }
        }
        public static List<double> newMDGetMeanAndVar(KeyValuePair<List<double[]>,List<double[,]>> trainedcluster,double[] testpoint)
        {
            var testpoints = RowtoMatrix(testpoint);     
            double[,] MeanStart, Xobdata;
            double[] obdatay;
            GetXfromValues(trainedcluster.Value, out Xobdata, out obdatay);

            var GPpara = trainedcluster.Key[2];
            var Regpara = trainedcluster.Key[1];
            var mean = GPwithMeanfunction.GetfVactor(obdatay);
          //  var cov = GPwithMeanfunction.GetCovMatrix(Xobdata, GPpara, true);
            var cov = trainedcluster.Key[3].Reshape(obdatay.Length, obdatay.Length, MatrixOrder.CRowMajor);
            var kInv = trainedcluster.Key[4].Reshape(obdatay.Length, obdatay.Length, MatrixOrder.CRowMajor);
            var MeanAndVar = new List<double>();
            var a = testpoints.GetLength(0);
            double variance;
            var h =RowtoMatrix(CalculatRegression.gety(Xobdata, Regpara));
            var hstarts = RowtoMatrix(CalculatRegression.gety(testpoints, Regpara));
            int c = Xobdata.GetLength(0) - 1;
            for (var i = 0; i <= a - 1; i++)
            {
                var XobAndTestData = GPwithMeanfunction.AddnewPoint(Xobdata, testpoints, i);
                var newcov = GPwithMeanfunction.GetCovMatrix(XobAndTestData, GPpara, true);
                var kstart = new double[newcov.GetLength(0) - 1, 1];
                for (var j = 0; j < newcov.GetLength(0) - 1; j++)
                {
                    kstart[j, 0] = newcov[j, newcov.GetLength(0) - 1];
                }
                var kstartT = kstart.transpose();
              
                //  MeanStart = (kstartT.multiply(kInv)).multiply(mean);
                if (GPpara.Length - Xobdata.GetLength(1) == 2)
                {
                    MeanStart = (kstartT.multiply(kInv)).multiply(mean);
                    var VarStart = (((kstartT.multiply(kInv)).multiply(kstart)).multiply(-1));
                    variance = VarStart[0, 0] + cov[c, c];
                }
                else //with mean fuc
                {
                    var regy = CalculatRegression.gety(testpoints, Regpara);
                  //  var regy = CalculatRegression.gety(testpoints, i, actionname)[0, 0] * GPpara[GPpara.Length - 1];
                    var regym = new double[1, 1];
                    regym[0, 0] = regy[0];
                    var fb = RowtoMatrix(CalculatRegression.gety(Xobdata,Regpara)).transpose().multiply(GPpara[GPpara.Length - 1]);
                    MeanStart = regym.add((kstartT.multiply(kInv)).multiply(mean.subtract(fb)));
                    //var VarStart = (((kstartT.multiply(kInv)).multiply(kstart)).multiply(-1));
                    //var variancestar = VarStart[0, 0] + cov[c, c];
                    var hstart = new double[1, 1];
                    hstart[0, 0] = hstarts[0, i];
                    var r = hstart.subtract((h.multiply(kInv)).multiply(kstart));
                    var hkk = ((h.multiply(kInv)).multiply(h.transpose())).inverse();

                    //variance = variancestar + (r.transpose().multiply(hkk))[0, 0];
                    variance = cov[c, c] + (r.transpose().multiply(hkk))[0, 0];
                }
                MeanAndVar.Add(MeanStart[0, 0]);
                //
                //double sdpow = Math.Pow(sd, 2);
                double sdabsolute = Math.Sqrt(Math.Abs(variance));
                MeanAndVar.Add(sdabsolute);
            }
            return MeanAndVar;
        }

       

        private static double[,] GetdaigonalWeight(double[] optimPara)
        {
            int counter = 0;
            var weight = new double[optimPara.Length - 3];
            for (int i = 0; i < optimPara.Length; i++)
            {
                if (i != 0 && i != optimPara.Length - 1 && i != optimPara.Length - 2)
                {
                    var ss = (1/ optimPara[i])*(1/ optimPara[i]);
                    weight[counter] = ss;
                    counter++;
                }
            }
            return CreatDaiMatrix(weight);
        }

        public static void Updatrainedtaclusters(double[] feedbackpointxy,
               Dictionary<List<double[]>, List<double[,]>> trainedclusters)
        {
            var existingpoint = false;
            var deletindex = 0;
            var mfeedbackpointx = GetXfromTraining(feedbackpointxy);
            var mfeedbackpoint = RowtoMatrix(mfeedbackpointx);
            var measures = new List<double>();
            var preclosestcenter = Getpreclosestcenter(trainedclusters, mfeedbackpoint, out measures);
            var maxmeasure = measures.Max();
            var threahold = 1e-10; ;//TBD
                                    // var threahold = double.NegativeInfinity;
            var closestclu = trainedclusters[trainedclusters.Keys.First(k => k[0].SequenceEqual(preclosestcenter))];
            foreach (var point in closestclu)
            {
                var xpointm = GetXfromTraining(point);
                var xfeedback = MatrixtoRow(GetXfromTraining(RowtoMatrix(feedbackpointxy)));
                if (EvaluationForBinaryTree.IsSameImputs(xfeedback, xpointm))
                {
                    existingpoint = true;
                    break;
                }
                else
                {
                    deletindex++;
                }
            }
            if (existingpoint)
            {
                var counter = 0;
                var newclosestclu = closestclu;

                for (int i = 0; i < feedbackpointxy.Length; i++)
                {
                    newclosestclu[deletindex][0, i] = feedbackpointxy[i];
                }
                var samekey = trainedclusters.Keys.First(k => k[0].SequenceEqual(preclosestcenter));
                trainedclusters.Remove(samekey);
                trainedclusters.Add(samekey, newclosestclu);
            }
            if (maxmeasure > threahold) // add point to cluster
            {
                var newvalues = new List<double[,]>(closestclu);
                newvalues.Add(RowtoMatrix(feedbackpointxy));
                if (newvalues.Count > 100)//remove data point
                {
                    // var diaweight = GetdaigonalWeight(trainedclu.Keys.First()[2]);
                    //  var farestcenterpoint = GetClosetPoint(closestclu, RowtoMatrix(feedbackpointxy), diaweight);
                    var oldkey = new List<double[]>(trainedclusters.Keys.First(k => k[0].SequenceEqual(preclosestcenter)));
                    var untrainedclu = new Dictionary<List<double[]>, List<double[,]>>();
                    var removeindex = Gen.Random.Numbers.Integers(0, closestclu.Count - 1)();
                    newvalues.RemoveAt(removeindex);
                    var newcenter = new double[1, feedbackpointxy.Length];
                    foreach (var p in newvalues)
                    {
                        newcenter = newcenter.add(p);
                    }
                    newcenter = newcenter.divide(newvalues.Count);
                    oldkey[0] = MatrixtoRow(newcenter);
                    untrainedclu.Add(oldkey, newvalues);
                    var trainedclu = UpdataRegAndGP(untrainedclu);
                    trainedclusters.Remove(trainedclusters.Keys.First(k => k[0].SequenceEqual(preclosestcenter)));
                    trainedclusters.Add(trainedclu.Keys.First(), newvalues);
                }
                else//add point and update
                {
                    var oldkey = new List<double[]>(trainedclusters.Keys.First(k => k[0].SequenceEqual(preclosestcenter)));
                    var untrainedclu = new Dictionary<List<double[]>, List<double[,]>>();
                    var newcenter = new double[1, feedbackpointxy.Length];
                    foreach (var p in newvalues)
                    {
                        newcenter = newcenter.add(p);
                    }
                    newcenter = newcenter.divide(newvalues.Count);
                    oldkey[0] = MatrixtoRow(newcenter);
                    untrainedclu.Add(oldkey, newvalues);
                    var trainedclu = UpdataRegAndGP(untrainedclu);
                    trainedclusters.Remove(trainedclusters.Keys.First(k => k[0].SequenceEqual(preclosestcenter)));
                    trainedclusters.Add(trainedclu.Keys.First(), newvalues);
                }
            }
            else //generate new cluster
            {
                var newcluter = Generatenewcluter(feedbackpointxy);
                trainedclusters.Add(newcluter.Keys.First(), newcluter.Values.First());
            }

        }
        private static double[] GetcoveriantArray(KeyValuePair<List<double[]>, List<double[,]>> initialclusture)
        {
            var obdataX = new double[initialclusture.Value.Count, initialclusture.Value[0].GetLength(1) - 1];
            var obdataY = new double[initialclusture.Value.Count];

            for (int i = 0; i < initialclusture.Value.Count; i++)
            {
                obdataY[i] = initialclusture.Value[i][0, initialclusture.Value[0].GetLength(1) - 1];
                for (int j = 0; j < initialclusture.Value[0].GetLength(1) - 1; j++)
                {
                    obdataX[i, j] = initialclusture.Value[i][0, j];
                }
            }
            var cov = GPwithMeanfunction.GetCovMatrix(obdataX, initialclusture.Key[2], true);
            return cov.Reshape();
        }
        private static Dictionary<List<double[]>, List<double[,]>> Generatenewcluter(double[] feedbackpoint )
        {

            var nearfeedbackpoint = new double[feedbackpoint.Length];
            for (int i = 0; i < feedbackpoint.Length; i++)
            {
                nearfeedbackpoint[i] = feedbackpoint[i]*1.001;
            }
            var newclusterdic = new Dictionary<List<double[]>, List<double[,]>>();
            var newclusterpoint = new List<double[,]>();
            newclusterpoint.Add(RowtoMatrix(feedbackpoint));
            newclusterpoint.Add(RowtoMatrix(nearfeedbackpoint));
            var newkey = new List<double[]> {new double[10], new double[10], new double[10]};
            newclusterdic.Add(newkey, newclusterpoint);
            newclusterdic = UpdataRegAndGP(newclusterdic);
            newclusterdic.Keys.First()[0] = feedbackpoint;
            return newclusterdic;
        }

        private static Dictionary<double[], double> GetDataDictionary(double[,] obdataX, double[] obdataY)
        {
            var dic = new Dictionary<double[], double>();

            for (int i = 0; i < obdataY.Length; i++)
            {
                try
                {
                    dic.Add(obdataX.GetRow(i), obdataY[i]);
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return dic;
        }
        private static void GetCoveriantandInvArray(KeyValuePair<List<double[]>, List<double[,]>> initialclusture, out double[] covarray, out double[] invarray)
        {

            var obdataX = new double[initialclusture.Value.Count, initialclusture.Value[0].GetLength(1) - 1];
            var obdataY = new double[initialclusture.Value.Count];

            for (int i = 0; i < initialclusture.Value.Count; i++)
            {
                obdataY[i] = initialclusture.Value[i][0, initialclusture.Value[0].GetLength(1) - 1];
                for (int j = 0; j < initialclusture.Value[0].GetLength(1) - 1; j++)
                {
                    obdataX[i, j] = initialclusture.Value[i][0, j];
                }
            }
            var cov = GPwithMeanfunction.GetCovMatrix(obdataX, initialclusture.Key[2], true);
            invarray = cov.inverse().Reshape();
            covarray = cov.Reshape();

        }
        private static Dictionary<List<double[]>, List<double[,]>> UpdataRegAndGP(
            Dictionary<List<double[]>, List<double[,]>> initialclustures)
        {
            var trainedclusters = new Dictionary<List<double[]>, List<double[,]>>();
            //key 1 is cluster center, key 2 is reg parameter, key 3 is gp parameter
            foreach (var initialclusture in initialclustures)
            {
                var newkey = new List<double[]>();
                var conterkey = initialclusture.Key[0];
                var regkey = new double[initialclusture.Key[1].Length];
                var gpkey = new double[initialclusture.Key[2].Length];
                double[] covarray, invarray;
                updateregression(initialclusture, out regkey);
                updataGP(initialclusture, out gpkey);
                newkey.Add(conterkey);
                newkey.Add(regkey);
                newkey.Add(gpkey);
                GetCoveriantandInvArray(initialclusture, out covarray, out invarray); //fill row first
                newkey.Add(covarray);
                newkey.Add(invarray);
                trainedclusters.Add(newkey, initialclusture.Value);
            }
            return trainedclusters;
        }

        private static void updataGP(KeyValuePair<List<double[]>, List<double[,]>> initialclusture, out double[] newkey)
        {
            var obdataX = new double[initialclusture.Value.Count, initialclusture.Value[0].GetLength(1) - 1];
            var obdataY = new double[initialclusture.Value.Count];
            var dim = obdataX.GetLength(1);
            for (int i = 0; i < initialclusture.Value.Count; i++)
            {
                obdataY[i] = initialclusture.Value[i][0, initialclusture.Value[0].GetLength(1) - 1];
                for (int j = 0; j < initialclusture.Value[0].GetLength(1) - 1; j++)
                {
                    obdataX[i, j] = initialclusture.Value[i][0, j];
                }
            }
            var lowrange = 0;
            var uprange = 2;
            var ranges = new double[dim * 2 + 6];
            for (var i = 0; i <= dim * 2 + 2; i = i + 2)
            {
                ranges[i] = lowrange;
                ranges[i + 1] = uprange;
            }
            var fv = double.PositiveInfinity;
            var oldsample = GetLHCsamples(ranges, LHCsize);
            var oldoptimpara = initialclusture.Key[2];
            var samples =new double[oldsample.GetLength(0)+1, oldsample.GetLength(1)];
            samples= oldsample.InsertRow(oldoptimpara, oldsample.GetLength(0));
            samples.InsertRow(initialclusture.Key[2]);
            // need work to add previous optimed gp parameters.
            //   samples = samples.InsertRow(trainedclusters.Keys[1], samples.GetLength(0) + 1);
            var OptimPara = new double[3 + dim];
            var para = new double[2 + dim]; // Order: sixf,sixl,noise
            for (var i = 0; i < LHCsize; i++)
            {
                para = samples.GetRow(i);
                var CandidatePara = Opt(obdataX, obdataY, para);
                if (CandidatePara[dim + 3] < fv)
                {
                    fv = CandidatePara[CandidatePara.GetLength(0) - 1];
                    for (int k = 0; k != dim + 3; k++)
                    {
                        OptimPara[k] = CandidatePara[k];
                    }
                    foreach (var c in OptimPara)
                    {
                        var d = Convert.ToDouble(c).ToString("0.00");
                        Console.Write("{0},", d);
                    }
                    Console.WriteLine("f={0:F3}", fv);
                }
                Console.Write("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b");
                Console.Write(" {0}%", 100 * (i + 1) / LHCsize);
            }
            newkey= OptimPara;

          
        }

        private static void updateregression(KeyValuePair<List<double[]>, List<double[,]>> initialclusture, out double[] newkey)
        {
            ///update for regression
                var subkey = new List<double[]>();
                double[][] inputs = new double[initialclusture.Value.Count][];
                var outputs = new double[initialclusture.Value.Count];
                for (int i = 0; i < initialclusture.Value.Count; i++)
                {
                    var mxvalue = GetXfromTraining(initialclusture.Value[i]);
                    var arrayx = MatrixtoRow(mxvalue);
                    for (int j = 0; j < arrayx.Length; j++)
                    {
                        if (arrayx[j] == 0)
                            arrayx[j] = 1e-3;
                    }
                    inputs[i] = arrayx;
                    outputs[i] = GetYfromTraining(initialclusture.Value[i]);
                }
                var ols = new OrdinaryLeastSquares();
               ols.IsRobust = true;
                ols.UseIntercept = true;
                MultipleLinearRegression regression = ols.Learn(inputs, outputs);
                var coe = new List<double>();
                foreach (var c in regression.Coefficients)
                {
                    coe.Add(c);
                }
                coe.Add(regression.Intercept);
            //  var newkey = new List<double[]>();
            //initialclusture.Key.Add();
            //subkey.Add(coe.ToArray());
            newkey = coe.ToArray();
               // OneDObjectiveFunc.Regcoe = coe.ToArray();
        }

        private static double[] Opt(double[,] obdataX, double[] obdataY, double[] para)
        {
            var optpara = new Optimization().Run(obdataX, obdataY, para);
            return optpara;
        }

        private static double[,] GetLHCsamples(double[] ranges, int dis)
        {
            var LHCindex = creatLHCindex(ranges.Count()/2, dis); //Return n sameples, m input 
            var dim = 0;
            var LHCsampels = new double[dis, ranges.GetLength(0)/2];
            var scale = Math.Abs(ranges[1] - ranges[0])/dis;
            for (var r = 0; r < ranges.Count()/2; r++)
            {
                if (dim == 8)
                {
                    var sgs = 1;
                }
                var c = LHCindex.GetColumn(dim);
                var fm = c.Count();
                var u = ranges[2*r];
                var ff = ranges[2*r + 1];
                var cv = Math.Abs(ranges[2*r] - ranges[2*r + 1])/fm;
                c = c.multiply(scale);
                for (int i = 0; i < c.Count(); i++)
                {
                    c[i] = c[i] + ranges[2*r];
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
            return LHCsampels;
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

        private static object draw(double[,] LHCindex)
        {
            var s = new object();

            foreach (var ss in LHCindex)
            {
                s = ss;
            }
            return s;
        }

        public static double[] MatrixtoRow(double[,] mxvalue)
        {
            var arr = new double[mxvalue.GetLength(1)];
            for (int i = 0; i < mxvalue.GetLength(1); i++)
            {
                arr[i] = mxvalue[0, i];
            }
            return arr;
        }

        public static double[,] RowtoMatrix(double[] xvalue)
        {
            var matrix = new double[1, xvalue.Length];
            for (int i = 0; i < matrix.GetLength(1); i++)
            {
                matrix[0, i] = xvalue[i];
            }
            return matrix;
        }

        //private static object GPprediction(Dictionary<double[,], List<double[,]>> initialclustures, double[] newpoint)
        //{

        //}

        //private static Dictionary<List<double[]>, List<double[,]>> Partitioningdata(double[,] trainxy,
        //    int numberofpartitions)
        //{
        //    var maxerr = double.NegativeInfinity;
        //    var winner = new Dictionary<List<double[]>, List<double[,]>>();
        //    for (int k = 0; k < numberoftryparting; k++)
        //    {
        //        var randomindex = new List<int>();
        //        while (randomindex.Count != numberofpartitions)
        //        {
        //            int randindex = Gen.Random.Numbers.Integers(0, trainxy.GetLength(0))();
        //            if (!randomindex.Any(a => a.Equals(randindex)))
        //                randomindex.Add(randindex);
        //        }
        //        var tempcenterpointlist = trainxy.GetRows(randomindex);
        //        var temprestpointlist = trainxy.RemoveRows(randomindex);

        //        var tempcentery = new double[numberofpartitions];

        //        //  var tempresty = obdataY.RemoveRows(randomindex);
        //        //Gen.Random.Numbers.Integers(0, trainxy.GetLength(0));
        //        var clusters = new Dictionary<List<double[]>, List<double[,]>>();
        //        //add random center

        //        for (int i = 0; i < tempcenterpointlist.GetLength(0); i++)
        //        {
        //            var keylist = new List<double[]>();
        //            keylist.Add(MatrixtoRow(tempcenterpointlist.GetRows(new List<int> { i })));
        //            keylist.Add(new double[1]);
        //            keylist.Add(OptimPara);
        //            clusters.Add(keylist, new List<double[,]> { tempcenterpointlist.GetRows(new List<int> { i }) });
        //        }
        //        for (int i = 0; i < temprestpointlist.GetLength(0); i++)
        //        {
        //            var temppoint = temprestpointlist.GetRows(new List<int> { i });
        //            initialzedictionary(temppoint, clusters);
        //        }
        //        var error = 0.0;
        //        foreach (var clu in clusters)
        //        {
        //            var weight = GetdaigonalWeight(clu.Key[2]);
        //            var center = GetXfromTraining(RowtoMatrix(clu.Key[0]));
        //            foreach (var xy in clu.Value)
        //            {
        //                var temppointx = GetXfromTraining(xy);
        //                var measure = Math.Pow(Math.E, (-0.5)*temppointx.subtract(center)
        //                    .multiply(weight).multiply(temppointx.subtract(center).transpose())[0, 0]);
        //                error = error + measure;
        //            }
        //        }
               
        //        if (error > maxerr)
        //        {
        //            kmeanrun = k;
        //            maxerr = error;
        //            winner = clusters;
        //        }
        //        //  var reclusters = Partitioningdata(clusters, numberofpartitions);
        //    }
        //    return winner;
        //}

        //private static void initialzedictionary(double[,] temppoint,
        //    Dictionary<List<double[]>, List<double[,]>> clusters)
        //{
        //    var listmeasure = new List<double>();
        //    var preclosestcenter = Getpreclosestcenter(clusters, temppoint,out listmeasure);
        //    var newclu = new List<double[,]>(clusters[clusters.Keys.First(k => k[0].SequenceEqual(preclosestcenter))]);
        //    newclu.Add(temppoint);
        //    var newcenter = new double[1, temppoint.GetLength(1)];
        //    foreach (var p in newclu)
        //    {
        //        newcenter = newcenter.add(p);
        //    }
        //    newcenter = newcenter.divide(newclu.Count);
        //    //if (newclu.Count > maximumpointswithincluster)
        //    //{
        //    //    var farestcenterpoint = GetClosetPoint(newclu, temppoint, daimatrix);
        //    //}
        //    //else
        //    //{
        //    //}
        //    //withinnewcluster delet point if full
        //    //
        //    var newkey = new List<double[]>();
        //    newkey.Add(MatrixtoRow(newcenter));
        //    newkey.Add(new double[1]);
        //    newkey.Add(OptimPara);
        //    clusters.Add(newkey, newclu);
        //    clusters.Remove(clusters.Keys.First(k => k[0].SequenceEqual(preclosestcenter)));
        //}

        private static double[,] GetClosetPoint(List<double[,]> newclu, double[,] temppointxy, double[,] daimatrix)
        {
            var maxmeasure = double.NegativeInfinity;
            var temppoint = GetXfromTraining(temppointxy);
            var closestpoint = new double[1, temppoint.GetLength(1)];
            var secondclosestpoint = new double[1, temppoint.GetLength(1)];
            foreach (var pointxy in newclu)
            {
                var point = GetXfromTraining(pointxy);
                var measure = Math.Pow(Math.E, (-0.5)*temppoint.subtract(point)
                    .multiply(daimatrix).multiply(temppoint.subtract(point).transpose())[0, 0]);
                if (measure > maxmeasure)
                {
                    secondclosestpoint = closestpoint;
                    maxmeasure = measure;
                    closestpoint = point;
                }
            }
            if (GetsigleRow(temppoint).SequenceEqual(GetsigleRow(closestpoint)))
            {
                closestpoint = secondclosestpoint;
            }
            return closestpoint;
        }

        private static double[] GetsigleRow(double[,] Rows)
        {
            var row = new double[Rows.GetLength(1)];
            for (int i = 0; i < Rows.GetLength(1); i++)
            {
                row[i] = Rows[0, i];
            }
            return row;
        }
        private static double[] Getpreclosestcenter(Dictionary<List<double[]>, List<double[,]>> clusters,
            double[,] temppointx, out List<double> listofmeasures)
        {
            listofmeasures = new List<double>();
            double  maxmeasure = double.NegativeInfinity;
            var closestcenter = new double[temppointx.Length];
            foreach (var clu in clusters)
            {
                if (clu.Key[2].Length == 0)
                {
                    var fdsaf = 1;
                }
                var weight = GetdaigonalWeight(clu.Key[2]);
                var center = GetXfromTraining(RowtoMatrix(clu.Key[0]));
                var measure = Math.Pow(Math.E, (-0.5)* temppointx.subtract(center)
                    .multiply(weight).multiply(temppointx.subtract(center).transpose())[0, 0]);
                listofmeasures.Add(measure);
                if (measure > maxmeasure)
                {
                    maxmeasure = measure;
                    closestcenter = clu.Key[0];
                }
            }
            return closestcenter;
        }

        //private static double[,] Getpreclosestcenter(Dictionary<List<double[]>, List<double[,]>> clusters, double[,] temppoint, double[,] weight)
        //{
        //    var listofmeasures = new List<double>();
        //    var minmeasure = double.NegativeInfinity;
        //    var minclu = new double[1, temppoint.Length];
        //    foreach (var clu in clusters)
        //    {
        //        var keyx = GetXfromTraining(RowtoMatrix(clu.Key[0]));
        //        var temppointx = GetXfromTraining(temppoint);
        //        var measure = Math.Pow(Math.E, (-0.5) * temppointx.subtract(keyx)
        //              .multiply(weight).multiply(temppointx.subtract(keyx).transpose())[0, 0]);
        //        listofmeasures.Add(measure);
        //        if (measure > minmeasure)
        //        {
        //            minmeasure = measure;
        //            minclu = RowtoMatrix(clu.Key[0]);
        //        }
        //    }
        //    return minclu;
        //}

        public static double[,] GetXfromTraining(double[,] xy)
        {
            var keyx = new double[1, xy.GetLength(1) - 1];
            for (int i = 0; i < xy.GetLength(1) - 1; i++)
            {
                keyx[0, i] = xy[0, i];
            }
            return keyx;
        }
        public static double[] GetXfromTraining(double[] xy)
        {
            var keyx = new double[ xy.Length- 1];
            for (int i = 0; i < xy.Length - 1; i++)
            {
                keyx[i] = xy[i];
            }
            return keyx;
        }
        private static double[,] GetXfromTrainingwithIntercept(double[,] key)
        {
            var keyx = new double[1, key.GetLength(1)];
            keyx[0, 0] = 1;
            for (int i = 0; i < key.GetLength(1) - 1; i++)
            {
                keyx[0, i + 1] = key[0, i];
            }
            return keyx;
        }

        public static double GetYfromTraining(double[,] key)
        {
            return key[0, key.GetLength(1) - 1];
        }

        public static double[,] CreatDaiMatrix(double[] input)
        {
            int p = input.Length();
            var Imatrix = new double[p, p];
            for (var i = 0; i < p; i++)
            {
                Imatrix[i, i] = input[i];
            }
            return Imatrix;
        }

        
    }
}
