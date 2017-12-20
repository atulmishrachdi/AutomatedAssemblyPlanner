using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace Assembly_Planner
{
    class SaveableBoundingBox
    {
        public SaveableVertex Center;
        public SaveableVertex[] CornerVertices;
        public double[] Dimensions;
        public double[][] Directions;
        public List<SaveableVertex>[] PointsOnFaces;
        public double Volume;

        public SaveableBoundingBox()
        {
            Center = new SaveableVertex();
            CornerVertices = new SaveableVertex[0];
            Dimensions = new double[0];
            Directions = new double[0][];
            PointsOnFaces = new List<SaveableVertex>[0];
            Volume = 0.0;
        }

        public SaveableBoundingBox(BoundingBox theBox)
        {
            Center = new SaveableVertex(theBox.Center);
            CornerVertices = (SaveableVertex[])theBox.CornerVertices.Select((el, idx) => new SaveableVertex(el));
            Dimensions = theBox.Dimensions;
            Directions = theBox.Directions;
            PointsOnFaces = (List<SaveableVertex>[])theBox.PointsOnFaces.Select((el, idx) => (el.Select((elem, indx) => new SaveableVertex(elem))));
            Volume = theBox.Volume;
        }

        public BoundingBox generate()
        {
            return new BoundingBox
            {

                Center = Center.generate(),
                CornerVertices = (Vertex[])CornerVertices.Select((el,
                idx) => el.generate()),
                Dimensions = Dimensions,
                Directions = Directions,
                PointsOnFaces = (List<Vertex>[])PointsOnFaces
                .Select((el, idx) => (el.Select((elem, indx) => elem.generate()))),
                Volume = Volume
            };
        }


    }
}
