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
    internal static class FastenerGaussianNaiveBayes
    {
        private static double[][] NormalParamsClass1;
        private static double[][] NormalParamsClass2;
        private static int Training = 134;
        private static int Features = 2;

        internal static bool GaussianNaiveBayesClassifier(List<PrimitiveSurface> primitives, TessellatedSolid solid)
        {
            var localFeature = FeatureArrayCreator(primitives, solid);
            if (TestClass1OrClass2(localFeature) == 1) return true;
            return false;
        }

        internal static void RunGaussianNaiveBayes()
        {
            var class1 = new double[Training, Features];
            var class2 = new double[Training, Features];
            TrainingDataReader(out class1, out class2);
            NormalParamsClass1 = NormalParamsCalculater(class1);
            NormalParamsClass2 = NormalParamsCalculater(class2);
        }

        private static double[][] NormalParamsCalculater(double[,] classi)
        {
            var normalParams = new double[Features][];
            
            for (var i = 0; i < Features; i++)
            {
                var featureData = new List<double>();
                for (var j = 0; j < Training; j++)
                    featureData.Add(classi[j, i]);

                var mean = MeanCalculater(featureData);
                var sigma = SigmaCalculater(mean, featureData);
                normalParams[i] = new[] {mean, sigma};
            }
            return normalParams;
        }

        private static double MeanCalculater(ICollection<double> data)
        {
            return data.Sum()/data.Count;
        }

        private static double SigmaCalculater(double mean, ICollection<double> data)
        {
            var sumOfSquaresOfDifferences = data.Select(val => Math.Pow(val - mean, 2)).Sum();
            return Math.Sqrt(sumOfSquaresOfDifferences/data.Count);
        }

        private static double LikelihoodCalculater(double theta, double sigma, double mean)
        {
            return (1/(Math.Sqrt(2*Math.PI*(Math.Pow(sigma, 2)))))*
                   (Math.Exp((-(Math.Pow(theta - mean, 2)))/(2*Math.Pow(sigma, 2))));
        }

        internal static int TestClass1OrClass2(double[] calculatedFeatures, double class1ToClass2Ratio = 0.5)
        {
            var productLikelihoodsClass1 = 1.0;
            for (int i = 0; i < NormalParamsClass1.Length; i++)
            {
                var featureNorm = NormalParamsClass1[i];
                var lild = LikelihoodCalculater(calculatedFeatures[i], featureNorm[1], featureNorm[0]);
                productLikelihoodsClass1 *= lild;
            }
            var productLikelihoodsClass2 = 1.0;
            for (int i = 0; i < NormalParamsClass2.Length; i++)
            {
                var featureNorm = NormalParamsClass2[i];
                var lild = LikelihoodCalculater(calculatedFeatures[i], featureNorm[1], featureNorm[0]);
                productLikelihoodsClass2 *= lild;
            }
            var evidence = class1ToClass2Ratio*productLikelihoodsClass1 +
                           ((1 - class1ToClass2Ratio)*productLikelihoodsClass2);
            var posteriorClass1 = class1ToClass2Ratio*(productLikelihoodsClass1)/evidence;
            var posteriorClass2 = (1 - class1ToClass2Ratio)*(productLikelihoodsClass2)/evidence;
            if (posteriorClass1 >= posteriorClass2) return 1;
            return -1;
        }

        private static void TrainingDataReader(out double[,] class1, out double[,] class2)
        {
            class1 = new double[Training, Features];
            class2 = new double[Training, Features];
            var reader =
                new StreamReader(
                    File.OpenRead("../../InitialGraphMaker/BoltAndGearDetector/ClassifierFiles/TrainingData.csv"));
            var counter = 0;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                if (Convert.ToDouble(values[0]) == 1) // this can be only 1 or -1
                    for (var i = 1; i < values.Length; i++)
                        class1[counter, i - 1] = Math.Round(Convert.ToDouble(values[i]), 4);
                else
                    for (var i = 1; i < values.Length; i++)
                        class2[counter, i - 1] = Math.Round(Convert.ToDouble(values[i]), 4);
                counter++;
            }
        }

        private static double[] FeatureArrayCreator(List<PrimitiveSurface> primitives, TessellatedSolid solid)
        {
            var cones = primitives.Where(p => p is Cone).ToList();
            var cylinder = primitives.Where(p => p is Cylinder).ToList();
            var coneArea = cones.Sum(c => c.Area);
            var cylinderArea = cylinder.Sum(c => c.Area);
            return new[] {(coneArea + cylinderArea)/solid.SurfaceArea, cones.Count};
        }
    }
}
