using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using Assembly_Planner;

namespace GPprocess
{
  public  class SaveandLoadData
    {
        public static void WriteClutersToCSV(Dictionary<List<double[]>, List<double[,]>> clusters, string csvaddress)
        {

            System.IO.File.Delete(csvaddress);
            //before your loop
            var csv = new StringBuilder();
            foreach (var clu in clusters)
            {
                string str = "key";
                csv.AppendLine(str);
                foreach (var keyvalue in clu.Key)
                {
                    str = "";
                    for (int i = 0; i < keyvalue.Length; ++i)
                    {
                        str += keyvalue[i].ToString();
                        if (i < keyvalue.Length)
                            str += ",";
                    }
                    csv.AppendLine(str);
                }
                str = "values";
                csv.AppendLine(str);
                foreach (var mvalue in clu.Value)
                {
                    var value = OnlineGPupdating.MatrixtoRow(mvalue);
                    str = "";
                    for (int i = 0; i < value.Length; ++i)
                    {
                        str += value[i].ToString();
                        if (i < value.Length)
                            str += ",";
                    }
                    csv.AppendLine(str);
                }
            }
            //   csv.AppendLine("Name,age");
            File.AppendAllText(csvaddress, csv.ToString());
        }

        public static Dictionary<List<double[]>, List<double[,]>> ReadCSVforclusters(string address)
        {
            var reachbottom = false;
            var clusters = new Dictionary<List<double[]>, List<double[,]>>();
            StreamReader sr = new StreamReader(address);
            string csvline = "";
            var listofnum = new List<double>();
            var allline = new List<string>();
            while ((csvline = sr.ReadLine()) != null)
            {
                allline.Add(csvline);

            }
            allline.Add("end");
            for (int i = 0; i < allline.Count - 1; i++)
            {
                var newkey = new List<double[]>();
                var newvalue = new List<double[,]>();
                var line = allline[i];

                if (line.Equals("key"))
                {
                    for (int j = 1; j < 6; j++)//change
                    {
                        var nextline = allline[i + j];
                        var keyelement = new List<double>();
                        var elements = nextline.Split(',');
                        for (int k = 0; k < elements.Length - 1; k++)
                        {
                            keyelement.Add(Convert.ToDouble(elements[k]));
                        }
                        newkey.Add(keyelement.ToArray());

                    }
                    i = i + 5;//change
                }

                do
                {
                    i++;
                    line = allline[i];
                    if (line.Equals("values"))
                        continue;
                    if (line.Equals("end"))
                        break;
                    var elements = line.Split(',');
                    var keyelement = new List<double>();
                    for (int k = 0; k < elements.Length - 1; k++)
                    {
                        keyelement.Add(Convert.ToDouble(elements[k]));
                    }
                    newvalue.Add(OnlineGPupdating.RowtoMatrix(keyelement.ToArray()));

                }
                while (!allline[i + 1].Equals("key"));


                clusters.Add(newkey, newvalue);

            }

            sr.Close();
            return clusters;
        }
       
        public static List<double[]> ReadUserFeedback(string address)
        {
            StreamReader sr = new StreamReader(address);
            string line = "";
            var d = 0;
            var data = new List<double[]>();
            while ((line = sr.ReadLine()) != null)
            {
                var listofnum = new List<double>();
                var a = line.Split(',');
                if (a.Length == 1)
                    continue;
                d = a.Count();

                foreach (var v in a)
                {
                    double b = Convert.ToDouble(v);
                    listofnum.Add(b);
                }
                data.Add(listofnum.ToArray());
            }
            sr.Close();
            return data;
        }
        public static void WriteUserFeedback(double[] newdata, string csvaddress)
        {
            var fxnew = OnlineGPupdating.GetXfromTraining(newdata);
            var fynew = newdata[newdata.Length - 1];
            var olddata = ReadUserFeedback(csvaddress);
            int deletindex = 0;
            bool needtodelete = false;
            for (int i = 0; i < olddata.Count; i++)
            {
                var f = olddata[i];
                var fxold = OnlineGPupdating.GetXfromTraining(f);
                if (EvaluationForBinaryTree.IsSameImputs(fxold, OnlineGPupdating.RowtoMatrix(fxnew)))
                {
                    f[f.Length - 1] = fynew;
                    needtodelete = true;
                    deletindex = i;
                    break;
                }
            }
            if (needtodelete)
            {
                olddata.RemoveAt(deletindex);
            }
            olddata.Add(newdata);
            System.IO.File.Delete(csvaddress);
            var csv = new StringBuilder();
            foreach (var value in olddata)
            {
                var str = "";
                for (int i = 0; i < value.Length; ++i)
                {
                    str += value[i].ToString();
                    if (i < value.Length - 1)
                        str += ",";
                }
                csv.AppendLine(str);
            }
            //   csv.AppendLine("Name,age");
            File.AppendAllText(csvaddress, csv.ToString());
            csv.Clear();
        }
    }
}
