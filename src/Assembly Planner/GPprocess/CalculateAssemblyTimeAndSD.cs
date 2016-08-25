using System;
using Assembly_Planner;
using GPprocess;
using Tool = Assembly_Planner.Tool;
using StarMathLib;

namespace Assembly_Planner

{
    public class CalculateAssemblyTimeAndSD
    {
        private static double[,] MovingobdataX = readdata.read("src/Assembly Planner/Evaluation/TrainningTimeData/movingdata/dataset4/TrainX.csv", 5);
        private static double[] MovingobdataY = readdata.read("src/Assembly Planner/Evaluation/TrainningTimeData/movingdata/dataset4/TrainY.csv");
        private static double[,] InstallobdataX = readdata.read("src/Assembly Planner/Evaluation/TrainningTimeData/installdata/dataset3/TrainX.csv", 6);
        private static double[] IinstallobdataY = readdata.read("src/Assembly Planner/Evaluation/TrainningTimeData/installdata/dataset3/TrainY.csv");
        // private static double[,] RotateobdataX = readdata.read("src/Assembly Planner/Evaluation/TrainningTimeData/rotatedata/dataset1/TrainX.csv", 10);
        // private static double[] RotateobdataY = readdata.read("src/Assembly Planner/Evaluation/TrainningTimeData/rotatedata/dataset1/TrainY.csv");
        // rotate only with angle
        private static double[,] RotateobdataX = readdata.read("src/Assembly Planner/Evaluation/TrainningTimeData/rotatedata/dataset1withonlyangles/TrainXangle.csv", 5);
        private static double[] RotateobdataY = readdata.read("src/Assembly Planner/Evaluation/TrainningTimeData/rotatedata/dataset1withonlyangles/TrainY.csv");
        private static double[] OptimParamoving = new double[7] { 0.37666666666666671, 0.8939, 0.30833333333333335, 0.12666666666666668, 0.45833333333333337, 0.15333333333333335, 0.018333333333333333 };
        private static double[,] COVmove =ThreeDinput.GetCovMatrix(MovingobdataX, OptimParamoving, true);
        private static double[] OptimParainstall = new double[8] { 0.77, 0.28, 0.21, 0.14, 0.27, 0.05, 0.37, 0.06 };
        private static double[,] COVinstall = ThreeDinput.GetCovMatrix(InstallobdataX, OptimParainstall, true);
        private static double[] OptimPararotate = new double[7] { 0.95113333333333339, 0.21833333333333335, 0.23833333333333334, 0.155, 0.46166666666666667, 0.18000000000000002, 0.01 };//need work
        private static double[,] COVrotate = ThreeDinput.GetCovMatrix(RotateobdataX, OptimPararotate, true);
        private static double[,] kInvmove = COVmove.inverse();
        private static double[,] kInvinstall = COVinstall.inverse();
        private static double[,] kInvrotate = COVrotate.inverse();




        public static void GetTimeAndSD(double[] testpoints, string action, out double meantime1, out double SD)
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
                var OptimPara = new double[7] { 0.37666666666666671, 0.8939, 0.30833333333333335, 0.12666666666666668, 0.45833333333333337, 0.15333333333333335, 0.018333333333333333 };
                var newm = ThreeDinput.newMDGetMeanAndVar(MovingobdataX, MovingobdataY, testpointsmatrix, OptimPara,COVmove,kInvmove);
                meantime1 = ThreeDinput.Getmean(newm)[0];
                SD = ThreeDinput.GetSD(newm)[0];
            }

            if (action.StartsWith("i") || action.StartsWith("I"))
            {
                var OptimPara = new double[8] { 0.77, 0.28, 0.21, 0.14, 0.27, 0.05, 0.37, 0.06 };
                var newm = ThreeDinput.newMDGetMeanAndVar(InstallobdataX, IinstallobdataY, testpointsmatrix, OptimPara,COVinstall,kInvinstall);
                meantime1 = ThreeDinput.Getmean(newm)[0];
                SD = ThreeDinput.GetSD(newm)[0];
            }

            //if (action.StartsWith("r") || action.StartsWith("R"))
            //{
            //    var OptimPara = new double[12] { 0.3627, 0.0455, 0.1677, 0.322, 0.387, 0.205, 0.315, 0.27, 0.182, 0.632, 0.026, 0.008 };//need work
            //    var newm = ThreeDinput.newMDGetMeanAndVar(RotateobdataX, RotateobdataY, testpointsmatrix, OptimPara);
            //    meantime1 = ThreeDinput.Getmean(newm)[0];
            //    SD = ThreeDinput.GetSD(newm)[0];
            //    meantime1 = GetRotatetimefromeReg(testpoints);
            //}

            // rotate with only angle
            if (action.StartsWith("r") || action.StartsWith("R"))
            {
                var OptimPara = new double[7] { 0.95113333333333339, 0.21833333333333335, 0.23833333333333334, 0.155, 0.46166666666666667, 0.18000000000000002, 0.01 };//need work
                var newm = ThreeDinput.newMDGetMeanAndVar(RotateobdataX, RotateobdataY, testpointsmatrix, OptimPara,COVrotate,kInvrotate);
                meantime1 = ThreeDinput.Getmean(newm)[0];
                SD = ThreeDinput.GetSD(newm)[0];
            }

        }

        private static double GetRotatetimefromeReg(double[] testpoints)
        {
            return 1.1138 + -2.0797 * testpoints[0] + 0.6611 * testpoints[1] + -1.4833 * testpoints[2] + 4.721 * testpoints[3] +
                   0.17206 * testpoints[4];

        }

        public static double GetSecureTime(Fastener f)
        {
            var x1 = f.OverallLength;
            var x2 = f.Diameter;
            var x3 = f.NumberOfThreads;
            var x5 = 1;//inster difficulty
            var x6 = 0.0;
            if (f.Nuts != null)
            {
                x6 = 1;
            }
            var x7 = 1;
            if (f.Tool != Tool.powerscrewdriver)
            {
                x7 = 0;
            }
            var logtime = 9.17 - 0.049 * x1 - 1.662 * x2 + 0.053 * x3 + 1.543 * x5
                          + 2.774 * x6 - 0.904 * x7 + Math.Pow(0.001 * x1, 2) + Math.Pow(0.101 * x2, 2) - Math.Pow(0.004 * x3, 2)
                          - 0.188 * x2 * x5 - 0.651 * x2 * x6 + 0.563 * x3 * x6 + 0.055 * x3 * x7 + 0.356 * x5 * x7;
            return Math.Pow(Math.E, logtime);
        }


    }
}
