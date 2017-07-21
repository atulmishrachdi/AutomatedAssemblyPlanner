using System.Collections.Generic;
using System.Xml.Serialization;
using Assembly_Planner;

namespace Assembly_Planner
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
    public class MoveRotateAction : Action
    {

    }
    public class RotateAction : Action
    {
        [XmlIgnore]
        public double[,] TransformationMatrix;
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
