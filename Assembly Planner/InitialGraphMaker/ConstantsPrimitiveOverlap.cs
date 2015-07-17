using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Planner
{
    class ConstantsPrimitiveOverlap
    {
        public const double ParralelLines = 0.005; //0.001
        public const double ParralelLines2 = 0.05; //0.001
        public const double RadiusDifs = 3; //0.02;
        public const double PointOnLine = 0.00001;
        public const double PointPoint = 0.0001;
        public const double PlaneDist = 0.035; // 0.0001
        public const double CheckWithGlobDirsParall = 0.00015;  //0.05
        public const double CheckWithGlobDirs = -0.0001;
        public const double EqualToZero = 5e-4;
        public const double EqualToZero2 = 0.15;
        public const double FractionIncreaseForAABBIntersect = 0.005;
    }
}
