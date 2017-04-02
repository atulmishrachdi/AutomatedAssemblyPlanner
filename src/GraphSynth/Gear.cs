using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace BaseClasses
{
    public enum GearType
    {
        Spur,
        Rack,
        Internal,
        Bevel
    }
    public class Gear
    {
        public TessellatedSolid Solid;
        public double[] Axis;
        public double[] PointOnAxis;
        public double LargeCylinderRadius;
        public double SmallCylinderRadius;
        public GearType Type;
        public TessellatedSolid[] MatedWith;
    }
}
