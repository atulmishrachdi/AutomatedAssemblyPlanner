using System.Collections.Generic;
using Assembly_Planner;

namespace AssemblyEvaluation
{
    public class Action
    {
        public double Time;
        public double TimeSD;
        public List<Tool> Tools;
    }
    public class MovingAction : Action
    {
        public double travaldistance { get; set; }
    }
    public class SecureAction : Action
    {
        public List<Fastener> Fasteners { get; set; }
    }
    public class RotateAction : Action
    {
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
