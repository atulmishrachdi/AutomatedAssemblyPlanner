using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace Assembly_Planner.StateSaving
{

    
    class TrackableSolid : TessellatedSolid
    {

        public TrackableSolid(IList<double[]> normals, IList<List<double[]>> vertsPerFace, IList<Color> colors, UnitType units = UnitType.unspecified, string name = "", string filename = "", List<string> comments = null, string language = "")
        : base(normals, vertsPerFace,colors, units,name, filename, comments, language)
        {

        }

        public TrackableSolid(IList<PolygonalFace> faces, IList<Vertex> vertices = null, IList<Color> colors = null, UnitType units = UnitType.unspecified, string name = "", string filename = "", List<string> comments = null, string language = "")
        : base(faces, vertices, colors, units, name, filename, comments, language)
        {

        }

    }
}
