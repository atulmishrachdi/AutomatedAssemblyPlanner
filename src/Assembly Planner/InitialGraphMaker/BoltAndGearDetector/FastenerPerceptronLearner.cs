#define NOSRC
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using TVGL.IOFunctions;

namespace Assembly_Planner
{
    static class FastenerPerceptronLearner
    {
        // This is a voted perceptron learner. What it gives me is a list of votes (c)
        // and a list of Ws. Every w from the begining to the end is saved and a vote is 
        // assigned to each. What I need to do for the test data is to take these two lists,
        // and do the dot product on x, w and then c. 
        private static int Training = 134;
        private static int Features = 3;

        internal static bool FastenerPerceptronClassifier(List<PrimitiveSurface> primitives, TessellatedSolid solid,
            List<double[]> learnerWeights, List<int> learnerVotes)
        {
            var localFeature = FeatureArrayCreator(primitives, solid);
            var signVector = 0;
            for (var k = 0; k < learnerVotes.Count; k++)
            {
                var dot = 0.0;
                for (var l = 0; l < Features; l++)
                    dot += learnerWeights[k][l] * localFeature[l];
                signVector += learnerVotes[k] * Math.Sign(dot);
            }
            var newY = Math.Sign(signVector);
            // I need intervals to check for certainity
            if (newY > 0) return true;
            return false;
        }

        internal static bool GaussianNaiveBayesClassifier(List<PrimitiveSurface> primitives, TessellatedSolid solid,
            List<double[]> learnerWeights, List<int> learnerVotes)
        {
            var localFeature = FeatureArrayCreator(primitives, solid);
            var signVector = 0;
            for (var k = 0; k < learnerVotes.Count; k++)
            {
                var dot = 0.0;
                for (var l = 0; l < Features; l++)
                    dot += learnerWeights[k][l]*localFeature[l];
                signVector += learnerVotes[k]*Math.Sign(dot);
            }
            var newY = Math.Sign(signVector);
            // I need intervals to check for certainity
            if (newY > 0) return true;
            return false;
        }

        private static void Learner(int iteration = 2000)
        {
            // Reading CSV
            var Y = new List<double>();
            var X = new double[Training, Features];
            TrainingDataReader(out X, out Y);

            var completionTimeEstimation = new double[iteration];
            for (var i = iteration - 1; i >= 0; i--)
            {
                if (i == iteration - 1)
                    completionTimeEstimation[i] = 1;
                else
                    completionTimeEstimation[i] = completionTimeEstimation[i + 1] - (1/(double) iteration) -
                                                  (1/(double) (10*iteration));
            }

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

            for (var j = 0; j < iteration; j++)
            {
                index++;
                var end = Training - 1;
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
                for (var i = 0; i < Training; i++)
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
            for (var i = 0; i < classificationErros.Count; i++)
                classificationErros[i] = classificationErros[i] / 5;
            WritingWeightsAndVotesInCsv(W,c);
            //Console.WriteLine(classificationErros.Min());
            //Console.ReadLine();
        }

        private static void TrainingDataReader(out double[,] X, out List<double> Y)
        {
            Y = new List<double>();
            X = new double[Training, Features];
            var reader =
				new StreamReader(

					#if NOSRC
					File.OpenRead(Program.state.inputDir+"/../training/TrainingData.csv")
					#else
					File.OpenRead("src/Assembly Planner/InitialGraphMaker/BoltAndGearDetector/ClassifierFiles/TrainingData.csv")
					#endif

				);
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

        private static void WritingWeightsAndVotesInCsv(List<double[]> weights, List<int> vots)
        {
            // the first m columns are weights (with m features (real features+1))
            // the last column is the vote

			var path =
			#if NOSRC
				Program.state.inputDir+"/../training";
			#else
				"src/Assembly Planner/InitialGraphMaker/BoltAndGearDetector/ClassifierFiles";
			#endif

            //Path to write the csv to:
            var weightsAndVotesPath = path + "/WeightsAndVotes.csv";
            if (!File.Exists(weightsAndVotesPath))
                File.Create(weightsAndVotesPath).Close();
            const string delimter = ",";
            var output = new List<string[]>();
            for (var i = 0; i < weights.Count; i++)
            {
                var array = new string[Features+1]; // if we have 2 features, Feature == 3, I added 1 for vote
                for (var j = 0; j < Features; j++)
                    array[j] = weights[i][j].ToString();
                array[array.Length - 1] = vots[i].ToString();
                output.Add(array);
            }
            var length = output.Count;
            using (TextWriter writer = File.CreateText(weightsAndVotesPath))
                for (int index = 0; index < length; index++)
                    writer.WriteLine(string.Join(delimter, output[index]));
        }

        private static double[] FeatureArrayCreator(List<PrimitiveSurface> primitives, TessellatedSolid solid)
        {
            var cones = primitives.Where(p => p is Cone).ToList();
            var cylinder = primitives.Where(p => p is Cylinder).ToList();
            var coneArea = cones.Sum(c => c.Area);
            var cylinderArea = cylinder.Sum(c => c.Area);
            return new[] { 1, (coneArea + cylinderArea) / solid.SurfaceArea, cones.Count };
        }

        internal static void RunPerecptronLearner(bool regenerateTrainingData)
        {
            // this functions, finds the training stls, opens them,
            // read them and creates the csv file of the training data
            // which includes the features of each solid.

            // Here is what I need to pay attention:
            //    1. if the csv file exists and the user doesnt want to improve(!) the results
            //       do nothing
            //    2. if the csv doesnt exist or the user has new training stls, we can run it and
            //       improve the classifier.

            var path = 

				#if NOSRC 
				Program.state.inputDir+"src/Assembly Planner/InitialGraphMaker/BoltAndGearDetector";
				#else
				"bin/training";
				#endif
            
            //Path to write the csv to:
			#if NOSRC
            var trainingDataPath = path + "/TrainingData.csv";
            var weightsAndVotesPath = path + "/WeightsAndVotes.csv";
			#else
			var trainingDataPath = path + "/ClassifierFiles/TrainingData.csv";
			var weightsAndVotesPath = path + "/ClassifierFiles/WeightsAndVotes.csv";
			#endif

            if (!regenerateTrainingData && File.Exists(weightsAndVotesPath))
                return;
            if (!regenerateTrainingData && !File.Exists(weightsAndVotesPath) && File.Exists(trainingDataPath))
            {
                // CSV of the training data exists, but weights and votes, dont exist, therefore: run the Learner
                Learner(); // this will automatically create the csv containing weights and votes
                return;
            }
            if (!regenerateTrainingData && !File.Exists(weightsAndVotesPath) && !File.Exists(trainingDataPath))
                Console.WriteLine("Sorry!! csv files don't exist. We need to generate the training data");
                //statusReporter.PrintMessage("BOUNDING GEOMETRIES ARE SUCCESSFULLY CREATED.", 1f);
            //Path to read STLs from:

			#if NOSRC
			var stlFastenerPath = path + "/Fastener";
			var stlNotFastenerPath = path + "/notFastener";
			#else
			var stlFastenerPath = path + "/TrainingSTLs/Fastener";
			var stlNotFastenerPath = path + "/TrainingSTLs/notFastener";
			#endif

            var fastenersTraining = StlToSolid(stlFastenerPath);
            var fastenerPrimitive = BlockingDetermination.PrimitiveMaker(fastenersTraining);

            var ntFastenersTraining = StlToSolid(stlNotFastenerPath);
            var notFastenerPrimitive = BlockingDetermination.PrimitiveMaker(ntFastenersTraining);

            if (!File.Exists(trainingDataPath))
                File.Create(trainingDataPath).Close();

            // now fill the csv:
            TrainingDataCsvFiller(trainingDataPath, fastenerPrimitive, notFastenerPrimitive);
            Learner();
        }

        private static void TrainingDataCsvFiller(string filePath,
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> fastenerPrimitive,
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> notFastenerPrimitive)
        {
            const string delimter = ",";
            var output = new List<string[]>();
            OutputFeatureArrayCreator(fastenerPrimitive, true, output);
            OutputFeatureArrayCreator(notFastenerPrimitive, false, output);
            Training = output.Count;
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
                var flat = solidPrim.Where(p => p is Flat).ToList();
                var cylinder = solidPrim.Where(p => p is Cylinder).ToList();
                var sphere = solidPrim.Where(p => p is Sphere).ToList();

                //double coneFacesCount = cones.Sum(c => c.Faces.Count);
                //double flatFacesCount = flat.Sum(f => f.Faces.Count);
                //double cylinderFacesCount = cylinder.Sum(c => c.Faces.Count);
                //double sphereFacesCount = sphere.Sum(c => c.Faces.Count);

                var coneArea = cones.Sum(c => c.Area);
                var flatArea = flat.Sum(c => c.Area);
                var cylinderArea = cylinder.Sum(c => c.Area);
                //var sphereArea = sphere.Sum(c => c.Area);

                //var feature1 = flatFacesCount/(flatArea/solid.SurfaceArea);
                //var feature2 = coneFacesCount/(coneArea/solid.SurfaceArea);
                //var feature3 = cylinderFacesCount/(cylinderArea/solid.SurfaceArea);
                //var feature4 = coneFacesCount/solid.Faces.Count();
                //var feature5 = flatFacesCount/solid.Faces.Count();
                //var feature6 = cylinderFacesCount/solid.Faces.Count();
                var feature7 = (coneArea+cylinderArea) / solid.SurfaceArea;
                //var feature8 = partSize[solid]/maxSize;
                var feature9 = cones.Count; //number of cone primitives
                var featureArray = new List<string>
                {
                    fastenerData ? 1.ToString() : (-1).ToString(),
                    feature7.ToString(),
                    feature9.ToString()
                };
                Features = featureArray.Count;
                output.Add(featureArray.ToArray());
            }
        }

        internal static List<double[]> ReadingLearnerWeightsAndVotesFromCsv(out List<int> votes)
        {
            votes = new List<int>();
            var weights = new List<double[]>();
            var reader =
                new StreamReader(
                    File.OpenRead(
						#if NOSRC
						Program.state.inputDir+"/fastener"
						#else
                        "src/Assembly Planner/InitialGraphMaker/BoltAndGearDetector/ClassifierFiles/WeightsAndVotes.csv"
						#endif
					));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');

                votes.Add(Convert.ToInt32(values[values.Length-1]));
                var w = new double[values.Length - 1];
                for (var i = 0; i < values.Length-1; i++)
                    w[i] = Convert.ToDouble(values[i]);
                weights.Add(w);
            }
            return weights;
        }

        private static List<TessellatedSolid> StlToSolid(string InputDir)
        {
            var parts = new List<TessellatedSolid>();
            var di = new DirectoryInfo(InputDir);
            var fis = di.EnumerateFiles("*.STL");
            Parallel.ForEach(fis, fileInfo =>
            //foreach (var fileInfo in fis)
            {
                var ts = IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name);
                lock (parts)
                {
                    parts.Add(ts[0]);
                }
            }
            );//
            parts =
                parts.Where(p => !p.Faces.Any(f => f.Edges.Any(e => e.OtherFace == null || e.OwnedFace == null)))
                    .ToList();
            return parts;
        }
    }
}
