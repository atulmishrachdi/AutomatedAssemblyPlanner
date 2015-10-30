using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using TVGL.IOFunctions;

namespace Assembly_Planner
{
    static class FastenerLearner
    {
        // This is a voted perceptron learner. What it gives me is a list of votes (c)
        // and a list of Ws. Every w from the begining to the end is saved and a vote is 
        // assigned to each. What I need to do for the test data is to take these two lists,
        // and do the dot product on x, w and then c. 
        private const int Test = 134;
        private static int Features = 3;

        internal static bool FastenerPerceptronLearner(List<PrimitiveSurface> primitives, TessellatedSolid solid, bool runLearner = false)
        {
            var feature = FeatureArrayCreator(primitives,solid);
            List<int> votes;
            List<double[]> weights;
            if (runLearner)
            {
                // the very first time we run the code, this needs to be done. But after weights are created and stored
                // in a csv file, then we can ask user if they want to run the learner to get a better results or not.
                // We can run it again only if the training data is updated by user or by the experiments that the code 
                // does.
                // first create the csv from the training stls:
                weights = Learner(out votes);
            }
            else
            {
                // open existing votes from the CSV in the same folder that the .cs file exists
                // this is a more common case.
            }
            return false;
        }

        private static List<double[]> Learner(out List<int> votes, int interation = 2000)
        {
            // Reading CSV
            var Y = new List<double>();
            var X = new double[Test, Features];
            TrainingDataReader(out X, out Y);

            // Defining Variables
            var n = 0;
            var W = new List<double[]>();
            var ini = new double[Features];
            for (var i = 0; i < Features; i++)
                ini[i] = 0.0;
            W.Add(ini);
            var c = new List<int> {0};
            var error = 0.0;
            var index = 0;
            var classificationErros = new List<double>();

            for (var j = 0; j < interation; j++)
            {
                index++;
                var end = Test - 1;
                var shuff = Enumerable.Range(0, ++end).ToList();
                shuff.Shuffle();
                foreach (var rnd in shuff)
                {
                    var randX = new double[Features];
                    for (var i = 0; i < Features; i++)
                        randX[i] = X[rnd, i];
                    var randY = Y[rnd];
                    var u = 0.0;
                    for (var k = 0; k < Features; k++)
                        u += W[n][k] * randX[k];
                    if (u * randY > 0)
                        c[n]++;
                    else
                    {
                        var newW = new double[Features];
                        for (var i = 0; i < Features; i++)
                            newW[i] = W[n][i] + randY*randX[i];
                        W.Add(newW);
                        n++;
                        c.Add(0);
                    }
                }
                for (var i = 0; i < Test; i++)
                {
                    var signVector = 0;
                    for (var k = 0; k < c.Count; k++)
                    {
                        var dot = 0.0;
                        for (var l = 0; l < Features; l++)
                            dot += W[k][l] * X[i, l];
                        signVector += c[k] * Math.Sign(dot);
                    }
                    var newY = Math.Sign(signVector);
                    if (newY != Y[i])
                        error = error + 1;
                }
                if (index != 5) continue;
                classificationErros.Add(error);
                error = 0;
                index = 0;
            }
            votes = c;
            for (var i = 0; i < classificationErros.Count; i++)
                classificationErros[i] = classificationErros[i] / 5;
            return W;
            //Console.WriteLine(classificationErros.Min());
            //Console.ReadLine();
        }

        private static void TrainingDataReader(out double[,] X, out List<double> Y)
        {
            Y = new List<double>();
            X = new double[Test, Features];
            var reader =
                new StreamReader(
                    File.OpenRead(
                        "../../InitialGraphMaker/BoltAndGearDetector/TrainingData.csv"));
            var counter = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                Y.Add(Convert.ToDouble(values[0]));
                X[counter, 0] = 1;
                for (var i = 1; i < values.Length; i++)
                    X[counter, i] = Math.Round(Convert.ToDouble(values[i]), 4);
                counter++;
            }
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var provider = new RNGCryptoServiceProvider();
            var n = list.Count;
            while (n > 1)
            {
                var box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                var k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private static double[] FeatureArrayCreator(List<PrimitiveSurface> primitives, TessellatedSolid solid)
        {
            var cones = primitives.Where(p => p is Cone).ToList();
            var cylinder = primitives.Where(p => p is Cylinder).ToList();
            var coneArea = cones.Sum(c => c.Area);
            var cylinderArea = cylinder.Sum(c => c.Area);
            return new[] { cones.Count, (coneArea + cylinderArea) / solid.SurfaceArea };
        }

        private static List<TessellatedSolid> StlToSolid(string InputDir)
        {
            var parts = new List<TessellatedSolid>();
            var di = new DirectoryInfo(InputDir);
            var fis = di.EnumerateFiles("*.STL");
            // Parallel.ForEach(fis, fileInfo =>
            foreach (var fileInfo in fis)
            {
                var ts = IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name);
                //ts.Name = ts.Name.Remove(0, 1);
                //lock (parts) 
                parts.Add(ts[0]);
            }
            //);
            return parts;
        }

        internal static void TrainingDataGenerator(bool regenerateTrainingData)
        {
            // this functions, finds the training stls, opens them,
            // read them and creates the csv file of the training data
            // which includes the features of each solid.

            // Here is what I need to pay attention:
            //    1. if the csv file exists and the user doesnt want to improve(!) the results
            //       do nothing
            //    2. if the csv doesnt exist or the user has new training stls, we can run it and
            //       improve the classifier.

            var path = "../../InitialGraphMaker/BoltAndGearDetector";
            
            //Path to write the csv to:
            var filePath = path + "/TrainingData.csv";
            if (!File.Exists(filePath) && !regenerateTrainingData)
                Console.WriteLine("csv file doesn'e exist. We need to generate the training data");
            if (File.Exists(filePath) && !regenerateTrainingData)
                return;
            //Path to read STLs from:
            var stlFastenerPath = path + "TrainingSTLs/Fastener";
            var fastenersTraining = StlToSolid(stlFastenerPath);
            var fastenerPrimitive = BlockingDetermination.PrimitiveMaker(fastenersTraining);

            var stlNotFastenerPath = path + "TrainingSTLs/notFastener";
            var ntFastenersTraining = StlToSolid(stlNotFastenerPath);
            var notFastenerPrimitive = BlockingDetermination.PrimitiveMaker(ntFastenersTraining);

            if (!File.Exists(filePath))
                File.Create(filePath).Close();

            // now fill the csv:
            TrainingDataCsvFiller(filePath, fastenerPrimitive, notFastenerPrimitive);
        }

        private static void TrainingDataCsvFiller(string filePath,
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> fastenerPrimitive,
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> notFastenerPrimitive)
        {
            const string delimter = ",";
            var output = new List<string[]>();
            OutputFeatureArrayCreator(fastenerPrimitive, true, output);
            OutputFeatureArrayCreator(notFastenerPrimitive, false, output);

            var length = output.Count;
            using (TextWriter writer = File.CreateText(filePath))
                for (int index = 0; index < length; index++)
                    writer.WriteLine(string.Join(delimter, output[index]));
        }

        private static void OutputFeatureArrayCreator(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive, bool fastenerData,
            List<string[]> output)
        {
            // y is equal to 1 for "fastenerData == true" and equal to -1 for "fastenerData == false"
            foreach (var solid in solidPrimitive.Keys) //.Where(s => !approvedNoise.Contains(s)))
            {
                var solidPrim = solidPrimitive[solid];
                var cones = solidPrim.Where(p => p is Cone).ToList();
                //var flat = solidPrim.Where(p => p is Flat).ToList();
                var cylinder = solidPrim.Where(p => p is Cylinder).ToList();
                var sphere = solidPrim.Where(p => p is Sphere).ToList();

                //double coneFacesCount = cones.Sum(c => c.Faces.Count);
                //double flatFacesCount = flat.Sum(f => f.Faces.Count);
                //double cylinderFacesCount = cylinder.Sum(c => c.Faces.Count);
                //double sphereFacesCount = sphere.Sum(c => c.Faces.Count);

                var coneArea = cones.Sum(c => c.Area);
                //var flatArea = flat.Sum(c => c.Area);
                var cylinderArea = cylinder.Sum(c => c.Area);
                //var sphereArea = sphere.Sum(c => c.Area);

                //var feature1 = flatFacesCount/(flatArea/solid.SurfaceArea);
                //var feature2 = coneFacesCount/(coneArea/solid.SurfaceArea);
                //var feature3 = cylinderFacesCount/(cylinderArea/solid.SurfaceArea);
                //var feature4 = coneFacesCount/solid.Faces.Count();
                //var feature5 = flatFacesCount/solid.Faces.Count();
                //var feature6 = cylinderFacesCount/solid.Faces.Count();
                var feature7 = (coneArea + cylinderArea) / solid.SurfaceArea;
                //var feature8 = partSize[solid]/maxSize;
                var feature9 = cones.Count; //number of cone primitives
                var featureArray = new List<string>
                {
                    fastenerData ? 1.ToString() : (-1).ToString(),
                    feature7.ToString(),
                    feature9.ToString()
                };

                Features = featureArray.Count + 1;
                output.Add(featureArray.ToArray());
            }
        }
    }
}
