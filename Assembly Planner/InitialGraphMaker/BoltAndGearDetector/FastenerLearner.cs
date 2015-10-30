using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace Assembly_Planner
{
    static class FastenerLearner
    {
        // This is a voted perceptron learner. What it gives me is a list of votes (c)
        // and a list of Ws. Every w from the begining to the end is saved and a vote is 
        // assigned to each. What I need to do for the test data is to take these two lists,
        // and do the dot product on x, w and then c. 
        private const int Test = 134;
        private const int Features = 3;
        private const int T = 1000;

        internal static bool FastenerPerceptronLearner(List<PrimitiveSurface> primitives, TessellatedSolid solid, bool runLearner = false)
        {
            var feature = FeatureArrayCreator(primitives,solid);
            if (runLearner)
            {
                // the very first time we run the code, this needs to be done. But after weights are created and stored
                // in a csv file, then we can ask user if they want to run the learner to get a better results or not.
                // We can run it again only if the training data is updated by user or by the experiments that the code 
                // does.
                Learner();
            }
            else
            {
                // open existing votes from the CSV
                // this is a more common case.
            }
            return false;
        }

        internal static void Learner()
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

            for (var j = 0; j < T; j++)
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
            for (var i = 0; i < classificationErros.Count; i++)
                classificationErros[i] = classificationErros[i] / 5;
            Console.WriteLine(classificationErros.Min());
            Console.ReadLine();
        }

        private static void TrainingDataReader(out double[,] X, out List<double> Y)
        {
            Y = new List<double>();
            X = new double[Test, Features];
            var reader =
                new StreamReader(
                    File.OpenRead(
                        "../../InitialGraphMaker/BoltAndGearDetector/TrainingFastener.csv"));
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
    }
}
