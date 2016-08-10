using System.Collections.Generic;
using System.Xml.Serialization;
using Assembly_Planner;

namespace AssemblyEvaluation
{
    public class Action
    {
        public double Time;
        public double TimeSD;
        public List<Tool> Tools;
    }
    public class SecureAction : Action
    {
        public List<Fastener> Fasteners { get; set; }
    }
    public class RotateAction : Action
    {
        [XmlIgnore]
        public double[,] TransformationMatrix;

        [XmlArray("RotateMatrix")]
        public double [][] RotationMatrix{
            get
            {
                int ypos;
                int xpos = 0;
                int width = TransformationMatrix.GetLength(0);
                int height = TransformationMatrix.GetLength(1);
                double[][] result = new double[width][];
                while (xpos < width)
                {
                    result[xpos] = new double[height];
                    ypos = 0;
                    while (ypos < height)
                    {
                        result[xpos][ypos] = TransformationMatrix[xpos, ypos];
                        ypos++;
                    }
                    xpos++;
                }
                return result;
            }
            set
            {

            }
        }

    }
    public class InstallAction : Action
    {
        public Part Reference
        {
            get;
            set;
        }

        public Part Moving
        {
            get;
            set;
        }

        public double[] InstallDirection { get; set; }
        public double InstallDistance { get; set; }
        public double[] InstallDirectionRotated { get; set; }
        public double[] InstallPoint { get; set; }
    }
}
