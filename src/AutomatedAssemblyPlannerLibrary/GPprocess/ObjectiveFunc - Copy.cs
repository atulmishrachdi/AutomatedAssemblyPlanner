//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using OptimizationToolbox;
//using StarMathLib;

//namespace GPprocess
//{
//    //public class c1 : IInequality, IDifferentiable
//    //{

//    //    public double calculate(double[] x)
//    //    {
//    //        throw new NotImplementedException();
//    //    }

//    //    public double deriv_wrt_xi(double[] x, int i)
//    //    {
//    //        throw new NotImplementedException();
//    //    }
//    //}
//    public class ObjectiveFunc: IObjectiveFunction //, IDifferentiable

//    {
//        public ObjectiveFunc(double[,] Xobdata, double[] obdatay)
//        {
//            this.Xobdata = Xobdata;
//            this.obdatay = obdatay;
//            Y = ThreeDinput.GetfVactor(obdatay);
//        }

//        public ObjectiveFunc(double[] Xobdata1D, double[] obdatay)
//        {
//            this.Xobdata = Xobdata;
//            this.obdatay = obdatay;
//            Y = ThreeDinput.GetfVactor(obdatay);
//        }

//        public double calculate(double[] x)
//        {
//            var k = ThreeDinput.SEKernel(Xobdata, x);

//            var firspart = -0.5 * VecToDouble((Y.transpose()).multiply(StarMath.inverse(k)).multiply(Y));
//            var secondpart = -0.5 * Math.Log(k.determinant());
//           return -(firspart + secondpart);

//        }
//        public double calculate(double[] x)
//        {
//            var k = ThreeDinput.SEKernel(Xobdata1D, x);
//            var firspart = -0.5 * VecToDouble((Y.transpose()).multiply(StarMath.inverse(k)).multiply(Y));
//            var secondpart = -0.5 * Math.Log(k.determinant());
//            return -(firspart + secondpart);

//        }

//        private double VecToDouble(double[,] p)
//        {
//            double d = p[0, 0];
//            return d;
//        }
    
//        public double deriv_wrt_xi(double[] x, int i)
//        {
//            throw new NotImplementedException();
//        }
//        private readonly double[,] Xobdata1D;
//        private readonly double[,] Xobdata;
//        private readonly double[] obdatay;
//        private readonly double[,] Y;
//    }
//}
