using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace Assembly_Planner.StateSaving
{
    class SaveableVertex
    {
        double[] position;

        SaveableVertex()
        {
            position = null;
        }

        SaveableVertex(Vertex theVertex)
        {
            position = theVertex.Position;
        }

        Vertex generate()
        {
            return new Vertex(position);
        }

    }
}
