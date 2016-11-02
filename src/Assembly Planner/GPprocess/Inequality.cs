using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;
using StarMathLib;
namespace GPprocess
{
    class Inequality:IInequality
    {
        private readonly OneDObjectiveFunc Opt;
        private readonly double[] Xobdata;
        private readonly double[] obdatay;


        public  Inequality(OneDObjectiveFunc opt, double[] Xobdata, double[] obdatay)
         {
             this.Xobdata = Xobdata;
            this.obdatay = obdatay;
             Opt = opt;

         }
          public double calculate(double[] x)
        {
            //var Y = ThreeDinput.GetfVactor(obdatay);
            //var k = ThreeDinput.SEKernel(Xobdata, x);
            //var firspart = -0.5 * VecToDouble((Y.transpose()).multiply(StarMath.inverse(k)).multiply(Y));
            //var secondpart = -0.5 * Math.Log(k.determinant());
              if (x[0]<0||x[1]<0||x[2]<0)
              {
                  return 0.0;
              }
              return -(Opt.calculate(x));
        }
          private double VecToDouble(double[,] p)
          {
              double d = p[0, 0];
              return d;
          }
    }
}
