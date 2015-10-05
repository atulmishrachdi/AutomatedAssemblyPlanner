using System.Collections.Generic;

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
    }
    public class RotateAction : Action
    {
        public double[] RotationTransform;
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

        public double[] InstallPoint { get; set; }
    }
}
