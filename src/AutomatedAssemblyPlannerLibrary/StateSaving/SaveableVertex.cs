using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace Assembly_Planner
{
    class SaveableVertex
    {

        double[] position;

        public SaveableVertex()
        {
            position = null;
        }

        public SaveableVertex(Vertex theVertex)
        {
            position = theVertex.Position;
        }

        public Vertex generate()
        {
            return new Vertex(position);
        }

    }
}
