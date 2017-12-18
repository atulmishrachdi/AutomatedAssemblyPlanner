using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace GPprocess
{
    public class readdata
    {
        public static double[] read(string address)
        {
            StreamReader sr = new StreamReader(address);
            string line = "";
            int counter = 0;
            var listofnum = new List<double>();
            while ((line = sr.ReadLine()) != null)
            {
                var a = line;
                double b = Convert.ToDouble(a);
                listofnum.Add(b);
            }
            var data = new double[listofnum.Count];
            int i = 0;
            foreach (var item in listofnum)
            {
                data[i] = item;
                i++;
            }
            return data;
        }
        public static double[,] read(string address, bool MultiDimensions, out int d)
        {
            StreamReader sr = new StreamReader(address);
            string line = "";
            var listofnum = new List<double>();
            d = 0;
            while ((line = sr.ReadLine()) != null)
            {
                var a = line.Split(',');
                d = a.Count();

                foreach (var v in a)
                {
                    double b = Convert.ToDouble(v);
                    listofnum.Add(b);
                }
            }
            var data = new double[listofnum.Count / d, d];
            int counter = 0;
            int counter2 = 0;
            foreach (var v in listofnum)
            {
                data[counter, counter2] = v;
                if (counter2 == d - 1)
                {
                    counter2 = 0;
                    counter++;
                    continue;
                }
                counter2++;
            }
            return data;
        }
        public static double[,] read(string address, bool MultiDimensions)
        {
            StreamReader sr = new StreamReader(address);
            string line = "";
            var listofnum = new List<double>();
           int d = 0;
            while ((line = sr.ReadLine()) != null)
            {
                var a = line.Split(',');
                d = a.Count();

                foreach (var v in a)
                {
                    double b = Convert.ToDouble(v);
                    listofnum.Add(b);
                }
            }
            var data = new double[listofnum.Count / d, d];
            int counter = 0;
            int counter2 = 0;
            foreach (var v in listofnum)
            {
                data[counter, counter2] = v;
                if (counter2 == d - 1)
                {
                    counter2 = 0;
                    counter++;
                    continue;
                }
                counter2++;
            }
            return data;
        }

    }
}
