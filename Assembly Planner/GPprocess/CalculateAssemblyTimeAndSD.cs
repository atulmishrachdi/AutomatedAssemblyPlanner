using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using MIConvexHull;
using Assembly_Planner.GraphSynth.BaseClasses;
using StarMathLib;
using AssemblyEvaluation;
using TVGL;
using Assembly_Planner;

namespace GPprocess
{
    public class CalculateAssemblyTimeAndSD
    {
        private static double[,] obdataX;


        public static double GetTimeAndSD(double[] testpoints, string action, out  double meantime1, out double SD)
        {
            meantime1 = new double();
            SD = new double();
            var l = testpoints.Length;

            var testpointsmatrix = new double[1, l];
            for (int i = 0; i < l; i++)
            {
                testpointsmatrix[0, i] = testpoints[i];
            }

            if (action.StartsWith("m") || action.StartsWith("M"))//done
            {
                obdataX = readdata.read("C:/WeifengDOC/Desktop/gpxk/movingdata/dataset3/TrainX.csv", 5);
                var obdataY = readdata.read("C:/WeifengDOC/Desktop/gpxk/movingdata/dataset3/TrainY.csv");
                var OptimPara = new double[7] { 0.26, 0.71, 0.21, 0.26, 0.26, 0.01, 0.06 };
                var newm = ThreeDinput.newMDGetMeanAndVar(obdataX, obdataY, testpointsmatrix, OptimPara);
                meantime1 = ThreeDinput.Getmean(newm)[0];
                SD = ThreeDinput.GetSD(newm)[0];
            }

            if (action.StartsWith("i") || action.StartsWith("L"))
            {
                var obdataX = readdata.read("C:/WeifengDOC/Desktop/gpxk/installdata/dataset3/TrainX.csv", 6);
                var obdataY = readdata.read("C:/WeifengDOC/Desktop/gpxk/installdata/dataset3/TrainY.csv");
                var OptimPara = new double[8] { 0.77, 0.28, 0.21, 0.14, 0.27, 0.05, 0.37, 0.06 };
                var newm = ThreeDinput.newMDGetMeanAndVar(obdataX, obdataY, testpointsmatrix, OptimPara);
                meantime1 = ThreeDinput.Getmean(newm)[0];
                SD = ThreeDinput.GetSD(newm)[0];
            }
            //if (action.StartsWith("s") || action.StartsWith("S"))
            //{
            //    var obdataX = readdata.read(Bridge.CSVPath + "testdata/MassAndVolorigin.csv", 2);
            //    var obdataY = readdata.read(Bridge.CSVPath + "testdata/Timeorgin.csv");
            //    var OptimPara = new double[4] { 5.430416, 15.3306, 16.16355, 0.09315 };
            //    var newm = ThreeDinput.newMDGetMeanAndVar(obdataX, obdataY, testpointsmatrix, OptimPara);
            //    meantime1 = ThreeDinput.Getmean(newm)[0];
            //    SD = ThreeDinput.GetSD(newm)[0];
            //}
            //if (action.StartsWith("r") || action.StartsWith("R"))
            //{
            //    var obdataX = readdata.read(Bridge.CSVPath + "testdata/MassAndVolorigin.csv", 2);
            //    var obdataY = readdata.read(Bridge.CSVPath + "testdata/Timeorgin.csv");
            //    var OptimPara = new double[4] { 5.430416, 15.3306, 16.16355, 0.09315 };
            //    var newm = ThreeDinput.newMDGetMeanAndVar(obdataX, obdataY, testpointsmatrix, OptimPara);
            //    meantime1 = ThreeDinput.Getmean(newm)[0];
            //    SD = ThreeDinput.GetSD(newm)[0];
            //}



            return 1.1;
        }
    }
}
