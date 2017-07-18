using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace Assembly_Planner.StateSaving
{
    class SaveableBoundingBox
    {
        public SaveableVertex Center;
        public SaveableVertex[] CornerVertices;
        public double[] Dimensions;
        public double[][] Directions;
        public List<SaveableVertex>[] PointsOnFaces;
        public double Volume;
    }
}
