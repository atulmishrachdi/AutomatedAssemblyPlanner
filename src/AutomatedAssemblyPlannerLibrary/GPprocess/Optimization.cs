using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;
using StarMathLib;
using System.Diagnostics;


namespace GPprocess
{
    public class Optimization
    {
        
        public double[] Run(double[] Xobdata, double[] obdatay, double[] para   //,double noise
            )
        {
            Parameters.Verbosity = VerbosityLevels.OnlyCritical;
            //Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var optMethod = new NelderMead();
            optMethod.Add(new GoldenSection(1e-8, 0.001));
            optMethod.Add(new DeltaFConvergence(1e-7));
            var opt = new OneDObjectiveFunc(Xobdata, obdatay, para);
            optMethod.Add(opt);
            double[] xStar;
            var xInit = para;
            try
            {
                var fstar_opt = optMethod.Run(out xStar, xInit);
                var output = new double[4] { xStar[0], xStar[1],xStar[2], fstar_opt };
                return output;
            }
            catch
            {
                var output = new double[4] {100, 1000,1000,100000 };
                return output;
            }
            // return xStar;

        }
        public double[] Run(double[,] Xobdata, double[] obdatay, double[] para)
        {
            Parameters.Verbosity = VerbosityLevels.OnlyCritical;
            // this next line is to set the Debug statements from OOOT to the Console.
            //Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            // var optMethod = new GradientBasedOptimization();
            // var Y = ThreeDinput.GetfVactor(obdatay);
            //    var optMethod = new GradientBasedOptimization();
            //    var optMethod = new GeneralizedReducedGradientActiveSet();
            //  var optMethod = new HillClimbing();

            var optMethod = new NelderMead();
            // optMethod.Add(new StochasticNeighborGenerator);
            // var optMethod = new NelderMead();
            //    optMethod.Add(new CyclicCoordinates());
            // optMethod.Add(new CyclicCoordinates());
            optMethod.Add(new GoldenSection(1e-7, 0.01));
            //   optMethod.Add(new DeltaXConvergence(1e-10));
            optMethod.Add(new DeltaFConvergence(1e-6));
            // optMethod.Add(new MaxSpanInPopulationConvergence(1e-3));
            //optMethod.Add(new inequalityWithConstant())
            var opt = new OneDObjectiveFunc(Xobdata, obdatay, para);
            optMethod.Add(opt);
            // optMethod.Add(new Inequality(opt, Xobdata, obdatay));
            // optMethod.Add(new OptimizationToolbox.greaterThanConstant { constant = 0.0, index = 0 });
            //optMethod.Add(new OptimizationToolbox.greaterThanConstant { constant = 0.0, index = 1 });
            //optMethod.Add(new OptimizationToolbox.greaterThanConstant { constant = 0.0, index = 2 });
            //optMethod.Add(new OptimizationToolbox.lessThanConstant() { constant = 0.30, index = 2 });
            //optMethod.Add(new OptimizationToolbox.squaredExteriorPenalty(optMethod, 1.0));
            // var p = new double[2] { para[0], para[1] };
            double[] xStar;
            var xInit = para;
            try
            {
                var fstar_opt = optMethod.Run(out xStar, xInit);
                var output = new double[5] { xStar[0], xStar[1], xStar[2],xStar[3] ,fstar_opt };
               // var output = new double[4] { xStar[0], xStar[1], xStar[2],  fstar_opt };//1d
                return output;
            }
            catch
            {
               // var output = new double[4] {  1000, 1000, 1000, 100000000 };//1d
              var output = new double[5] { 100, 1000, 1000,1000, 100000000 };
                return output;
            }


            // return xStar;

        }


    }
}
