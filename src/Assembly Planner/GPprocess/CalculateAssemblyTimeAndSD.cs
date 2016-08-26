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
using AwokeKnowing.GnuplotCSharp;
using OptimizationToolbox;
using System.IO;
using System.Xml.Linq;


namespace GPprocess
{
    public class CalculateAssemblyTimeAndSD
    {
        private static string address1 = "src/Assembly Planner/Evaluation/TrainningTimeData/JMDmove.csv";
        private static string address2 = "src/Assembly Planner/Evaluation/TrainningTimeData/JMDinstall.csv";
        private static string address3 = "src/Assembly Planner/Evaluation/TrainningTimeData/JMDrotate.csv";
        private static string address4= "src/Assembly Planner/Evaluation/TrainningTimeData/JMDsecure.csv";
        private static double[,] xmove = Gettrainningdata(address1);
        private static double[] ymove = Gettrainningdata(address1,true);

        private static double[,] xinstall = Gettrainningdata(address2);
        private static double[] yinstall = Gettrainningdata(address2, true);

        private static double[,] xsecure = Gettrainningdata(address3);
        private static double[] ysecure = Gettrainningdata(address3, true);

        private static double[,] xrotate = Gettrainningdata(address4);
        private static double[] yrotate = Gettrainningdata(address4, true);

        //   Gettrainningdata(address, actionname, out obdataX, out obdataY);
        public static void GetTimeAndSD(double[] testpoints, string actionname, out double time, out double SD)
        {
            double[,] obdataX;
            double[] obdataY;
            if (actionname.StartsWith("m") || actionname.StartsWith("M"))
            {
                obdataX = xmove;
                obdataY = ymove;
            }
            else if (actionname.StartsWith("i") || actionname.StartsWith("I"))
            {
                obdataX = xinstall;
                obdataY = yinstall;
            }
            else if (actionname.StartsWith("S") || actionname.StartsWith("s"))
            {
                obdataX = xsecure;
                obdataY = ysecure;
            }
            else
            {
                    obdataX = xrotate;
                    obdataY = yrotate;
            }
            

            double lntime;
            double lnsd;
            TimeAndSD(testpoints, actionname, obdataX, obdataY, out lntime, out lnsd);
            time = Math.Exp(lntime);
            SD = Math.Exp(lnsd);
            //mult times;
            //var exptime = new List<double>();
            //var expSD = new List<double>();
            //var expci95 = new List<double>();
            //var logci95 = new List<double>();          
            //foreach (var t in lntime)
            //{
            //    if (actionname.StartsWith("m"))
            //    {
            //        exptime.Add(Math.Exp(t) + 1);
            //    }
            //    else
            //    {
            //        exptime.Add(Math.Exp(t));
            //    }
            //}

            //for (int i = 0; i < exptime.Count; i++)
            //{
            //    var bound = 1.96 * SD[i];
            //    var upper = time[i] + bound;
            //    var lower = time[i] - bound;
            //    logci95.Add(upper);
            //    logci95.Add(lower);
            //}
            //for (int i = 0; i < exptime.Count; i++)
            //{
            //    var bound = 1.96 * SD[i];
            //    var upper = exptime[i] + Math.Exp(bound);
            //    var lower = exptime[i] - Math.Exp(bound);
            //    expci95.Add(upper);
            //    expci95.Add(lower);
            //    expSD.Add(Math.Exp(bound));
            //}
            //var msr = new List<double>();
        }

        //public static void TimeAndSD(double[,] testpoints, string action, out double[] time, out double[] SD)
        //      {
        //          var meantime1 = new double();
        //          var testpointsmatrix = testpoints;

        //          if (action.StartsWith("m") || action.StartsWith("M")) //done
        //          {
        //              var OptimPara = new double[8]
        //              {
        //                  ////all
        //                  //0.8067,
        //                  //0.26535,
        //                  //0.6062,
        //                  //0.42575,
        //                  //0.98715,
        //                  //0.90695,
        //                  //0.0544,
        //                  //1.10745,

        //                  ////90%
        //                  0.7265,
        //                  0.8067,
        //                  1.10745,
        //                  0.7666,
        //                  0.4057,
        //                  0.8067,
        //                  0.0742,
        //                  1.10745,
        //              };
        //              var newm = GPwithMeanfunction.newMDGetMeanAndVar(obdataX, obdataY, testpointsmatrix, OptimPara, action);
        //              time = GPwithMeanfunction.Getmean(newm);
        //              SD = GPwithMeanfunction.GetSD(newm);
        //          }

        //          else if (action.StartsWith("i") || action.StartsWith("I"))
        //          {
        //              var trainningaddress = "../../../../GPdata/JMDinstall7log - Copy.csv";
        //              Readwholedata.getdata(trainningaddress, actionname, out obdataX, out obdataY);
        //              var OptimPara = new double[9]
        //              {
        //                  ////all
        //                  //0.291511111,
        //                  //0.391733333,
        //                  //0.171244444,
        //                  //0.712444444,
        //                  //0.712444444,
        //                  //0.612222222,
        //                  //0.772577778,
        //                  //0.2128,
        //                  //1.0532

        //                  //90
        //                  0.431822222,
        //                  0.932933333,
        //                  1.013111111,
        //                  0.832711111,
        //                  1.033155556,
        //                  1.0532,
        //                  0.672355556,
        //                  0.1732,
        //                  0.892844444,
        //              };
        //              var newm = GPwithMeanfunction.newMDGetMeanAndVar(obdataX, obdataY, testpointsmatrix, OptimPara,
        //                  action);
        //              time = GPwithMeanfunction.Getmean(newm);
        //              SD = GPwithMeanfunction.GetSD(newm);
        //          }
        //          else if (action.StartsWith("s") || action.StartsWith("S"))
        //          {
        //              var trainningaddress = "../../../../GPdata/JMDsecurereorder.csv";
        //              Readwholedata.getdata(trainningaddress, actionname, out obdataX, out obdataY);
        //              var OptimPara = new double[9]
        //              {
        //                  //////all
        //                  //0.471911111,
        //                  //0.532044444,
        //                  //0.351644444,
        //                  //0.892844444,
        //                  //1.033155556,
        //                  //0.111111111,
        //                  //0.572133333,
        //                  //0.1534,
        //                  //1.033155556,

        //                  /////90
        //                  /// 
        //                  0.532044444,
        //                  0.892844444,
        //                  0.532044444,
        //                  0.592177778,
        //                  0.612222222,
        //                  1.013111111,
        //                  0.993066667,
        //                  0.0346,
        //                  1.033155556,
        //              };
        //              var newm = GPwithMeanfunction.newMDGetMeanAndVar(obdataX, obdataY, testpointsmatrix, OptimPara,
        //                  action);
        //              time = GPwithMeanfunction.Getmean(newm);
        //              SD = GPwithMeanfunction.GetSD(newm);
        //          }
        //          else

        //          {
        //              var trainningaddress = "../../../../GPdata/JMDrotatelog.csv";
        //              Readwholedata.getdata(trainningaddress, actionname, out obdataX, out obdataY);
        //              var OptimPara = new double[8]
        //              {
        //                  ////all
        //                  //0.46585,
        //                  //0.62625,
        //                  //0.8468,
        //                  //0.74655,
        //                  //0.78665,
        //                  //0.2453,
        //                  //0.0544,
        //                  //1.0473

        //                  ////90
        //                  0.3656,
        //                  0.3656,
        //                  1.06735,
        //                  0.9671,
        //                  0.98715,
        //                  0.8869,
        //                  0.0544,
        //                  0.90695,
        //              };
        //              var newm = GPwithMeanfunction.newMDGetMeanAndVar(obdataX, obdataY, testpointsmatrix, OptimPara, action);
        //              time = GPwithMeanfunction.Getmean(newm);
        //              SD = GPwithMeanfunction.GetSD(newm);
        //          }
        //          //   return meantime1;
        //      }
        static void TimeAndSD(double[] testpoints, string action, double[,] obdataX, double[] obdataY,
            out double meantime1, out double SD)
        {
            SD = 0;
            meantime1 = 0;
            var l = testpoints.Length;

            var testpointsmatrix = new double[1, l];
            for (int i = 0; i < l; i++)
            {
                testpointsmatrix[0, i] = testpoints[i];
            }
            if (action.StartsWith("m") || action.StartsWith("M")) //done
            {
                var OptimPara = new double[8]
                {
                    0.8067,
                    0.26535,
                    0.6062,
                    0.42575,
                    0.98715,
                    0.90695,
                    0.0544,
                    1.10745,
                };
                var newm = GPwithMeanfunction.newMDGetMeanAndVar(obdataX, obdataY, testpointsmatrix, OptimPara, action);
                meantime1 = GPwithMeanfunction.Getmean(newm)[0];
                SD = GPwithMeanfunction.GetSD(newm)[0];
            }

         else    if (action.StartsWith("i") || action.StartsWith("I"))
            {
                var OptimPara = new double[9]
                {
                    0.291511111,
                    0.391733333,
                    0.171244444,
                    0.712444444,
                    0.712444444,
                    0.612222222,
                    0.772577778,
                    0.2128,
                    1.0532
                };
                var newm = GPwithMeanfunction.newMDGetMeanAndVar(obdataX, obdataY, testpointsmatrix, OptimPara, action);
                meantime1 = GPwithMeanfunction.Getmean(newm)[0];
                SD = GPwithMeanfunction.GetSD(newm)[0];
            }
          else  if (action.StartsWith("s") || action.StartsWith("S"))
            {
                var OptimPara = new double[9]
                {
                    0.471911111,
                    0.532044444,
                    0.351644444,
                    0.892844444,
                    1.033155556,
                    0.111111111,
                    0.572133333,
                    0.1534,
                    1.033155556,
                };
                var newm = GPwithMeanfunction.newMDGetMeanAndVar(obdataX, obdataY, testpointsmatrix, OptimPara, action);
                meantime1 = GPwithMeanfunction.Getmean(newm)[0];
                SD = GPwithMeanfunction.GetSD(newm)[0];
            }
            else if (action.StartsWith("R") || action.StartsWith("r"))
            {
                var OptimPara = new double[8]
                {
                    0.46585,
                    0.62625,
                    0.8468,
                    0.74655,
                    0.78665,
                    0.2453,
                    0.0544,
                    1.0473
                };
                var newm = GPwithMeanfunction.newMDGetMeanAndVar(obdataX, obdataY, testpointsmatrix, OptimPara, action);
                meantime1 = GPwithMeanfunction.Getmean(newm)[0];
                SD = GPwithMeanfunction.GetSD(newm)[0];
            }
        }

        public static double[] Gettrainningdata(string alldataaddress, bool oned)
        {
           
            var alldata = readdata.read(alldataaddress, true);
            int d = alldata.GetLength(1)-1;
            var v = alldata.GetColumns(new List<int> {d});
            var y = new double[v.GetLength(0)];
            for (int i = 0; i < v.GetLength(0); i++)
            {
                y[i] = v[i,0];
            }
            return y;
        }
        public static double[,]  Gettrainningdata(string alldataaddress )
        {
            
            int d;
            var alldata = readdata.read(alldataaddress, true, out d);
            var trainindex = new List<int>();
            for (int i = 0; i < d-1; i++)
            {
                trainindex.Add(i);
            }
           return  alldata.GetColumns(trainindex);
        }
    }
}
