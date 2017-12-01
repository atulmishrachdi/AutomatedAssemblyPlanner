#define NOSRC
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
using Accord.Statistics;
using Assembly_Planner;
using StarMathLib;
using System.Management.Instrumentation;



namespace GPprocess
{
    public class CalculateAssemblyTimeAndSD
    {
		#if NOSRC
		public static Dictionary<List<double[]>, List<double[,]>> MoveDictionary = SaveandLoadData.ReadCSVforclusters(Program.state.inputDir+"/../training/ClusterMove.csv");
		public static Dictionary<List<double[]>, List<double[,]>> InstallDictionary = SaveandLoadData.ReadCSVforclusters(Program.state.inputDir+"/../training/ClusterInstall.csv");
		public static Dictionary<List<double[]>, List<double[,]>> SecureDictionary = SaveandLoadData.ReadCSVforclusters(Program.state.inputDir+"/../training/ClusterSecure.csv");
		public static Dictionary<List<double[]>, List<double[,]>> RotateDictionary = SaveandLoadData.ReadCSVforclusters(Program.state.inputDir+"/../training/ClusterRotate.csv");
		public static List<double[]> usermovedata = SaveandLoadData.ReadUserFeedback(Program.state.inputDir+"/../training/UserMoveData.csv");
		public static List<double[]> userinstalldata = SaveandLoadData.ReadUserFeedback(Program.state.inputDir+"/../training/UserInstallData.csv");
		public static List<double[]> usersecuredata = SaveandLoadData.ReadUserFeedback(Program.state.inputDir+"/../training/UserSecureData.csv");
		public static List<double[]> userrotatedata = SaveandLoadData.ReadUserFeedback(Program.state.inputDir+"/../training/UserRotateData.csv");
		public static List<double[]> usermoveplusinstalldata = SaveandLoadData.ReadUserFeedback(Program.state.inputDir+"/../training/UserMoveplusInstallData.csv");
		#else
		public static Dictionary<List<double[]>, List<double[,]>> MoveDictionary = SaveandLoadData.ReadCSVforclusters(Directory.GetCurrentDirectory() + "/src/Assembly Planner/Evaluation/TrainningTimeData/ClusterMove.csv");
		public static Dictionary<List<double[]>, List<double[,]>> InstallDictionary = SaveandLoadData.ReadCSVforclusters(Directory.GetCurrentDirectory() + "/src/Assembly Planner/Evaluation/TrainningTimeData/ClusterInstall.csv");
		public static Dictionary<List<double[]>, List<double[,]>> SecureDictionary = SaveandLoadData.ReadCSVforclusters(Directory.GetCurrentDirectory() + "/src/Assembly Planner/Evaluation/TrainningTimeData/ClusterSecure.csv");
		public static Dictionary<List<double[]>, List<double[,]>> RotateDictionary = SaveandLoadData.ReadCSVforclusters(Directory.GetCurrentDirectory() + "/src/Assembly Planner/Evaluation/TrainningTimeData/ClusterRotate.csv");
		public static List<double[]> usermovedata = SaveandLoadData.ReadUserFeedback(Directory.GetCurrentDirectory() + "/src/Assembly Planner/Evaluation/TrainningTimeData/UserMoveData.csv");
		public static List<double[]> userinstalldata = SaveandLoadData.ReadUserFeedback(Directory.GetCurrentDirectory() + "/src/Assembly Planner/Evaluation/TrainningTimeData/UserInstallData.csv");
		public static List<double[]> usersecuredata = SaveandLoadData.ReadUserFeedback(Directory.GetCurrentDirectory() + "/src/Assembly Planner/Evaluation/TrainningTimeData/UserSecureData.csv");
		public static List<double[]> userrotatedata = SaveandLoadData.ReadUserFeedback(Directory.GetCurrentDirectory() + "/src/Assembly Planner/Evaluation/TrainningTimeData/UserRotateData.csv");
		public static List<double[]> usermoveplusinstalldata = SaveandLoadData.ReadUserFeedback(Directory.GetCurrentDirectory() + "/src/Assembly Planner/Evaluation/TrainningTimeData/UserMoveplusInstallData.csv");
		#endif

		//  Gettrainningdata(address, actionname, out obdataX, out obdataY);
        public static void GetTimeAndSD(double[] testpoints, string actionname, out double time, out double SD)
        {
            var inputdictionary = new Dictionary<List<double[]>, List<double[,]>>();
            double[] userx;
            double usery;
            var userfeedbacks = new List<double[]>();
            if (actionname.StartsWith("m") || actionname.StartsWith("M"))
            {
                userfeedbacks = usermovedata;
                inputdictionary = MoveDictionary;
            }
            else if (actionname.StartsWith("i") || actionname.StartsWith("I"))
            {
                userfeedbacks = userinstalldata;
                inputdictionary = InstallDictionary;
            }
            else if (actionname.StartsWith("S") || actionname.StartsWith("s"))
            {
                userfeedbacks = usersecuredata;
                inputdictionary = SecureDictionary;
            }
            else
            {
                userfeedbacks = userrotatedata;
                inputdictionary = RotateDictionary;
            }
            Gethistoricaldata(testpoints, userfeedbacks, out usery);
            if (usery != -1)
            {
                time = usery;
                SD = 0;
            }
            else
            {
                TimeAndSD(testpoints, inputdictionary, userfeedbacks, out time, out SD);
            }
            if (time < 13.389 && time > 13.30)
            {
                var sss = 1;
            }
        }

        public static void Gethistoricaldata(double[] testpointsx, List<double[]> userfeedbacks, out double usery)
        {
            usery = -1;
            foreach (var fb in userfeedbacks)
            {
                var fbx = OnlineGPupdating.GetXfromTraining(OnlineGPupdating.RowtoMatrix(fb));
                if (EvaluationForBinaryTree.IsSameImputs(testpointsx, fbx))
                {
                    usery = fb[fb.Length - 1];
                    break;
                }
                else
                {
                    usery = -1;
                }
            }
        }

        public static void TimeAndSD(double[] testpoints, Dictionary<List<double[]>, List<double[,]>> trainedDictionary, List<double[]> userfeedbacks,
            out double meantime, out double SD)
        {
            //need work
            //    SaveandLoadCluters.WriteClutersToCSV(trainedDictionary, Bridge.CSVPath + "TrainningTimeData/xxkk.csv");
            SD = 0;
            meantime = 0;
            var measures = new List<double>();
            var means = new List<double>();
            var SDs = new List<double>();
            OnlineGPupdating.GPprediction(trainedDictionary, testpoints, out measures, out means, out SDs);
            var extractindex = measures.IndexOf(measures.Max());
            meantime = Math.Exp(means[extractindex]);
            SD = Math.Exp(SDs[extractindex]);
            var maxtime = new double();
            var mintime = new double();
            GetMaxandMinTimefromSignlecluster(trainedDictionary, extractindex, out maxtime, out mintime);
            if (meantime > maxtime * 1.5)
                meantime = maxtime * 1.5;
            if (meantime < mintime * 0.5)
                meantime = mintime * 0.5;
        }
        private static void GetMaxandMinTimefromSignlecluster(Dictionary<List<double[]>, List<double[,]>> trainedDictionary, int extractindex, out double maxtime, out double mintime)
        {
            maxtime = double.NegativeInfinity;
            mintime = double.PositiveInfinity;
            var values = trainedDictionary[trainedDictionary.Keys.ToList()[extractindex]];
            foreach (var value in values)
            {
                var y = OnlineGPupdating.GetYfromTraining(value);
                if (y > maxtime)
                {
                    maxtime = y;
                }
                if (y < mintime)
                {
                    mintime = y;
                }
            }
            maxtime = Math.Exp(maxtime);
            mintime = Math.Exp(mintime);
        }

        public static void GetTimeAndSD(SubAssembly sub, double[] testpoints, string actionname, out double time,
            out double SD)
        {
            time = 0;
            SD = 0;
            var inputdictionary = new Dictionary<List<double[]>, List<double[,]>>();
            double[,] subinputs;
            double[] userx;
            double usery, subtime, subsd;
            var userfeedbacks = new List<double[]>();
            if (actionname.StartsWith("S") || actionname.StartsWith("s"))
            {
                userfeedbacks = usersecuredata;
                inputdictionary = SecureDictionary;
                Gethistoricaldata(testpoints, userfeedbacks, out usery);
                //   usery = -1; // disable userfeedback
                if (usery != -1)
                {
                    time = usery;
                    SD = 0;
                }
                else
                {
                    foreach (var otherf in sub.Secure.Fasteners)
                    {
                        if (otherf.SecureModelInputs != null)
                        {
                            if (EvaluationForBinaryTree.IsSameImputs(testpoints, otherf.SecureModelInputs) && otherf.Time > 0.001)
                            //if time is included in subassembly
                            {
                                time = otherf.Time;
                                SD = 0;
                                break;
                            }
                            else
                            {
                                TimeAndSD(testpoints, inputdictionary, userfeedbacks, out time, out SD);
                                break;
                            }
                        }

                    }
                }
            }
            else
            {
                if (actionname.StartsWith("m") || actionname.StartsWith("M"))
                {
                    userfeedbacks = usermovedata;
                    inputdictionary = MoveDictionary;
                    subinputs = sub.MoveRoateModelInputs;
                    subtime = sub.MoveRoate.Time;
                    subsd = sub.MoveRoate.TimeSD;
                }
                else if (actionname.StartsWith("i") || actionname.StartsWith("I"))
                {
                    userfeedbacks = userinstalldata;
                    inputdictionary = InstallDictionary;
                    subinputs = sub.InstallModelInputs;
                    subtime = sub.Install.Time;
                    subsd = sub.Install.TimeSD;
                }
                else
                {
                    userfeedbacks = userrotatedata;
                    inputdictionary = RotateDictionary;
                    subinputs = sub.RotateModelInputs;
                    subtime = sub.Rotate.Time;
                    subsd = sub.Rotate.TimeSD;
                }
                Gethistoricaldata(testpoints, userfeedbacks, out usery);
                //  usery = -1; // disable userfeedback
                if (usery != -1)
                {

                    time = usery;
                    SD = 0;
                    if (actionname.StartsWith("i") && time > 2.9 && time < 3.01)
                    {
                        var sss = 1;
                    }
                }
                else
                {
                    if (EvaluationForBinaryTree.IsSameImputs(testpoints, subinputs) && subtime != 0)
                    //if time is included in subassembly
                    {
                        time = subtime;
                        SD = subsd;
                        if (actionname.StartsWith("i") && time > 2.9 && time < 3.01)
                        {
                            var sss = 1;
                        }

                    }
                    else
                    {
                        TimeAndSD(testpoints, inputdictionary, userfeedbacks, out time, out SD);
                        if (actionname.StartsWith("i") && time > 2.9 && time < 3.01)
                        {
                            var sss = 1;
                        }
                    }
                }
            }
        }
    }
}
