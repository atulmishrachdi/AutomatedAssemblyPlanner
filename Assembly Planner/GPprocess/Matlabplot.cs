using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPprocess
{
    class Matlabplot
    {
        public static void Displacements(double[,] x, double[] y)
        {

            var matlab = new MLApp.MLApp();
            matlab.Execute("format long g");

            var x1 = new StringBuilder();
            var x2 = new StringBuilder();
            var  f = new StringBuilder();

            for (int i = 0; i < y.GetLength(0); i++)
            {
                x1.Append((x[i,0] + 1) + " ");
                x2.Append((x[i,1] + 1) + " ");
                f.Append(y[i] + " ");
            }
          
            matlab.Execute("clc");
           // matlab.Execute("close all");
            matlab.Execute("x1=[" + x1 + "]");
            //matlab.Execute("jK=");
            matlab.Execute("x2=[" + x2 + "]");
            matlab.Execute("f=[" + f + "]");
            matlab.Execute("figure");
           
            matlab.Execute("scatter3(x1,x2,f)");
           // matlab.Execute("plot3(x1,x2,f");

        }
        public static void Displacements(double[,] x, double[] y,double[]v)
        {

            var matlab = new MLApp.MLApp();
            matlab.Execute("format long g");

            var x1 = new StringBuilder();
            var x2 = new StringBuilder();
            var f = new StringBuilder();
            var vari = new StringBuilder();
            var upper = new StringBuilder();
            var lower = new StringBuilder();

            var uppervari = new double[v.GetLength(0)];
            var lowervari = new double[v.GetLength(0)];
            for (int i = 0; i < v.GetLength(0); i++)
            {
                uppervari[i]=y[i]+v[i];
                lowervari[i]=y[i]-v[i];
            }

            for (int i = 0; i < y.GetLength(0); i++)
            {
                x1.Append((x[i, 0] + 1) + " ");
                x2.Append((x[i, 1] + 1) + " ");
                f.Append(y[i] + " ");
                upper.Append(uppervari[i] + " ");
                lower.Append(lowervari[i] + " ");
            }

            matlab.Execute("clc");
            matlab.Execute("x1=[" + x1 + "]");
            matlab.Execute("x2=[" + x2 + "]");
            matlab.Execute("f=[" + f + "]");
            matlab.Execute("upper=[" + upper + "]");
            matlab.Execute("lower=[" + lower + "]");
            matlab.Execute("figure");
            matlab.Execute("scatter3(x1,x2,f)");
            matlab.Execute("hold on");
          //  Console.WriteLine(matlab.Execute("scatter3(x1,x2,upper)"));
            matlab.Execute("scatter3(x1,x2,upper)");
            matlab.Execute("hold on");
            matlab.Execute("scatter3(x1,x2,lower)");
            //matlab.Execute("hold on");
        }
        public static void Displacements(double[,] x, double[] y, double[] v,double[] trueY)
        {

            var matlab = new MLApp.MLApp();
            matlab.Execute("format long g");

            var x1 = new StringBuilder();
            var x2 = new StringBuilder();
            var f = new StringBuilder();
            var vari = new StringBuilder();
            var upper = new StringBuilder();
            var lower = new StringBuilder();
            var tY = new StringBuilder();

            var uppervari = new double[v.GetLength(0)];
            var lowervari = new double[v.GetLength(0)];
            for (int i = 0; i < v.GetLength(0); i++)
            {
                uppervari[i] = y[i] 
                    +Math.Abs(v[i])
                   ;
                lowervari[i] = y[i] 
                    - Math.Abs(v[i])
                    ;
            }

            for (int i = 0; i < y.GetLength(0); i++)
            {
                x1.Append(x[i, 0]  + " ");
                x2.Append(x[i, 1]  + " ");
              //  f.Append(y[i] + " ");
                upper.Append(uppervari[i] + " ");
                lower.Append(lowervari[i] + " ");
                tY.Append(trueY[i] + " ");
            }

            matlab.Execute("clc");
            matlab.Execute("x1=[" + x1 + "]");
            matlab.Execute("x2=[" + x2 + "]");
            matlab.Execute("ff=[" + tY + "]");
            matlab.Execute("upper=[" + upper + "]");
            matlab.Execute("lower=[" + lower + "]");
            matlab.Execute("figure");
            Console.WriteLine(matlab.Execute("scatter3(x1,x2,ff)"));
            matlab.Execute("scatter3(x1,x2,f1)");
            matlab.Execute("hold on");
            //  Console.WriteLine(matlab.Execute("scatter3(x1,x2,upper)"));
            matlab.Execute("scatter3(x1,x2,upper)");
            matlab.Execute("hold on");
            matlab.Execute("scatter3(x1,x2,lower)");
            //matlab.Execute("hold on");
        }
        public static void Displacements(double[,] x, double[] y, double[] yt,bool mandm)
        {

            var matlab = new MLApp.MLApp();
            matlab.Execute("format long g");

            var x1 = new StringBuilder();
            var x2 = new StringBuilder();
            var f = new StringBuilder();
            var ft = new StringBuilder();
            for (int i = 0; i < y.GetLength(0); i++)
            {
                x1.Append(x[i, 0] + " ");
                x2.Append(x[i, 1]  + " ");
                f.Append(y[i] + " ");
                ft.Append(yt[i] + " ");
            }

            matlab.Execute("clc");
            matlab.Execute("x1=[" + x1 + "]");
            matlab.Execute("x2=[" + x2 + "]");
            matlab.Execute("f=[" + f + "]");
            matlab.Execute("ft=[" + ft + "]");
          
            matlab.Execute("figure");
            matlab.Execute("scatter3(x1,x2,f)");
            matlab.Execute("hold on");
            //  Console.WriteLine(matlab.Execute("scatter3(x1,x2,upper)"));
            matlab.Execute("scatter3(x1,x2,ft)");
            //matlab.Execute("hold on");
            //matlab.Execute("scatter3(x1,x2,lower)");
            //matlab.Execute("hold on");
        }
    }
}
