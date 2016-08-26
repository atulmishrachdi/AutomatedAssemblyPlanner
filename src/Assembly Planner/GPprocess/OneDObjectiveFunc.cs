using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizationToolbox;
using StarMathLib;

namespace GPprocess
{
    //public class c1 : IInequality, IDifferentiable
    //{

    //    public double calculate(double[] x)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public double deriv_wrt_xi(double[] x, int i)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
    public class OneDObjectiveFunc: IObjectiveFunction //, IDifferentiable

    {
        public OneDObjectiveFunc(double[] Xobdata, double[] obdatay, double[] para, double noise)
        {
            this.Xobdata = Xobdata;
            this.obdatay = obdatay;
            this.noise = noise;
            this.para = para;

            Y = ThreeDinput.GetfVactor(obdatay);
        }
        public OneDObjectiveFunc(double[,] MDXobdata, double[] obdatay, double[] para)
        {
            this.MDXobdata = MDXobdata;
            this.obdatay = obdatay;
         //   this.noise = noise;
            this.para = para;


            Y = ThreeDinput.GetfVactor(obdatay);
        }
        public OneDObjectiveFunc(double[] Xobdata, double[] obdatay, double[] para)//matlab test 
        {
            this.Xobdata = Xobdata;
            this.obdatay = obdatay;
            //this.noise = noise;
            this.para = para;

            Y = ThreeDinput.GetfVactor(obdatay);
        }
        public double calculate(double[] x)//matlab test 
        {
            var matlab = true;
            var md = true;
            double[,] k;

            if (matlab && !md)
            {
                k = ThreeDinput.SEKernel(Xobdata, x, true);
            }
            else if (matlab && md)
            {
                k = ThreeDinput.SEKernel(MDXobdata, x, true);
            }
            else if (x.GetLength(0) == 2)
            {
                k = ThreeDinput.SEKernel(Xobdata, x, noise);
            }
            else
            {
                k = ThreeDinput.SEKernel(MDXobdata, x, noise, true);
            }


            var liknoise = Math.Exp(2 * para[3]);   //MD  
            var l = Cholesky(k.divide(liknoise).add(CreatImatrix(MDXobdata.GetLength(0))));//MD


            //var liknoise = Math.Exp(2 * para[2]);//1D
            //var l = Cholesky(k.divide(liknoise).add(CreatImatrix(Xobdata.GetLength(0))));//1D
            

            var y = l.inverse();
            var l1 = y.multiply(obdatay);
            l = l.transpose();
            l = l.inverse();
           
            var alpha = l.multiply(l1).divide(liknoise);


           l = Cholesky(k.divide(liknoise).add(CreatImatrix(MDXobdata.GetLength(0))));//MD
          // l = Cholesky(k.divide(liknoise).add(CreatImatrix(Xobdata.GetLength(0))));//1D
            var f = alpha.multiply(Y);
            var firstp = f[0] / 2;
            var secondp = sumlogl(l);
            var thrdp = l.GetLength(0) * Math.Log(2 * Math.PI * liknoise) / 2;
            var total = firstp + secondp + thrdp;

            //if (x.Any(a => a.CompareTo(0) == -1)//0 is sf, 1 is L
            if (x[0] < 0 || x[1] < 0 
                || x[2] < 0
                || x[3] > 1
                ) 
               
                
            {
                return 10000000;
            }
            if (total == 100000)
            { 
                int xx = 1;
            }
            return total;







            ///OLD log likelihoold
            //var firspart = -0.5 * VecToDouble((Y.transpose()).multiply(StarMath.inverse(k)).multiply(Y));
            //var kt = StarMath.CholeskyDecomposition(k);
            //var kkt = k.determinant();
            //var secondpart = -0.5 * Math.Log(k.determinant());
            //if (x.Any(a => a.CompareTo(0) == -1)//0 is sf, 1 is L
            //    //|| x[0] > 0.5
            //    // || x[1] < 0.1
            //    //|| x[2] > 1.1
            //    //|| x[3] > 1.1
            //    //|| x[2] < 0.8
            //    //|| x[3] < 0.8
            //    )
            //{
            //    return 10000000;
            //}
            ////else if (-(firspart + secondpart) < -1000)
            ////{
            ////    var a = -(firspart + secondpart);
            ////    return 10000000;
            ////}
            //var re = -(firspart + secondpart); 
            //  return -(firspart + secondpart);

        }
        //public double calculate(double[] x)////commemt
        //{
        //    var matlab = true;
        //    double[,] k;

        //    if (matlab)
        //    {
        //        k = ThreeDinput.SEKernel(Xobdata, x, noise, true);
        //    }
        //    else if (x.GetLength(0) == 2)
        //    {
        //        k = ThreeDinput.SEKernel(Xobdata, x, noise);
        //    }
        //    else
        //    {
        //        k = ThreeDinput.SEKernel(MDXobdata, x, noise, true);
        //    }


        //    var l = Cholesky(k.divide(noise).add(CreatImatrix(Xobdata.GetLength(0))));
        //    // l = l.transpose();
        //    var y = l.inverse();
        //    var l1 = y.multiply(obdatay);
        //    l = l.transpose();
        //    l = l.inverse();
        //    var alpha = l.multiply(l1).divide(noise);

        //    //
        //    //var kstart = new double[k.GetLength(0), 1];
        //    //for (var j = 0; j < k.GetLength(0) - 1; j++)
        //    //{
        //    //    kstart[j, 0] = kstart[j, k.GetLength(0) - 1];
        //    //}
        //    //

        //    l = Cholesky(k.divide(noise).add(CreatImatrix(Xobdata.GetLength(0))));
        //    var f = alpha.multiply(Y);
        //    var firstp = f[0] / 2;
        //    var secondp = sumlogl(l);
        //    var thrdp = l.GetLength(0) * Math.Log(2 * Math.PI * noise) / 2;
        //    var total = firstp + secondp + thrdp;

        //    if (x.Any(a => a.CompareTo(0) == -1)//0 is sf, 1 is L
        //        //|| x[0] > 0.5
        //        // || x[1] < 0.1
        //        //|| x[2] > 1.1
        //        //|| x[3] > 1.1
        //        //|| x[2] < 0.8
        //        //|| x[3] < 0.8
        //        )
        //    {
        //        return 10000000;
        //    }
        //    return total;
        //    ///OLD log likelihoold
        //    //var firspart = -0.5 * VecToDouble((Y.transpose()).multiply(StarMath.inverse(k)).multiply(Y));
        //    //var kt = StarMath.CholeskyDecomposition(k);
        //    //var kkt = k.determinant();
        //    //var secondpart = -0.5 * Math.Log(k.determinant());
        //    //if (x.Any(a => a.CompareTo(0) == -1)//0 is sf, 1 is L
        //    //    //|| x[0] > 0.5
        //    //    // || x[1] < 0.1
        //    //    //|| x[2] > 1.1
        //    //    //|| x[3] > 1.1
        //    //    //|| x[2] < 0.8
        //    //    //|| x[3] < 0.8
        //    //    )
        //    //{
        //    //    return 10000000;
        //    //}
        //    ////else if (-(firspart + secondpart) < -1000)
        //    ////{
        //    ////    var a = -(firspart + secondpart);
        //    ////    return 10000000;
        //    ////}
        //    //var re = -(firspart + secondpart); 
        //    //  return -(firspart + secondpart);

        //}

        private double VecToDouble(double[,] p)
        {
            double d = p[0, 0];
            return d;
        }
        public double sumlogl(double[,] l)
        {
            int size = l.GetLength(0);
            var sumlogdiag=0.0;

            for (int i = 0; i < size; i++)
            {
                sumlogdiag+=Math.Log(l[i,i]);
            }
            return sumlogdiag;
            
        }
        public static double[,] CreatImatrix(int p)
        {
            var Imatrix = new double[p, p];

            for (var i = 0; i < p; i++)
            {
                Imatrix[i, i] = 1;

            }
            return Imatrix;

        }
        public static double[,] Cholesky(double[,] a)
        {
            int n = (int)Math.Sqrt(a.Length);

            double[,] ret = new double[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c <= r; c++)
                {
                    if (c == r)
                    {
                        double sum = 0;
                        for (int j = 0; j < c; j++)
                        {
                            sum += ret[c, j] * ret[c, j];
                        }
                        ret[c, c] = Math.Sqrt(a[c, c] - sum);
                    }
                    else
                    {
                        double sum = 0;
                        for (int j = 0; j < c; j++)
                            sum += ret[r, j] * ret[c, j];
                        ret[r, c] = 1.0 / ret[c, c] * (a[r, c] - sum);
                    }
                }

            return ret;
        }
        public double deriv_wrt_xi(double[] x, int i)
        {
            throw new NotImplementedException();
        }
        private readonly double[,] MDXobdata;
        private readonly double[] Xobdata;
        private readonly double[] obdatay;
        private readonly double [] para;
        private readonly double[,] Y;
        private readonly double noise;

    }
}
