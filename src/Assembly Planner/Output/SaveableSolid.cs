using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using System.Xml.Serialization;

namespace GraphSynth.OutputXML
{
    [Serializable]
    public class SaveableSolid
    {

        public readonly List<string> Comments;
        public readonly string FileName;
        public readonly string Language;
        public Color SolidColor;
        public readonly UnitType Units;
        public List<int[]> Faces;
        public List<double[]> Vertices;


        int[] toIndexes(Vertex[] Vertices, PolygonalFace theFace)
        {
            int[] result = new int[3];
            result[0] = Array.FindIndex(Vertices, face => face == theFace.Vertices[0] );
            result[1] = Array.FindIndex(Vertices, face => face == theFace.Vertices[1] );
            result[2] = Array.FindIndex(Vertices, face => face == theFace.Vertices[2] );
            return result;
        }

        double[] toCoords(Vertex theVertex)
        {
            double[] result = new double[3];
            result[0] = theVertex.X;
            result[1] = theVertex.Y;
            result[2] = theVertex.Z;
            return result;
        }

        public SaveableSolid(TessellatedSolid theSolid)
        {
            Comments = theSolid.Comments;
            FileName = theSolid.FileName;
            Language = theSolid.Language;
            SolidColor = theSolid.SolidColor;
            Units = theSolid.Units;
            Vertices = new List<double[]>();
            foreach(Vertex v in theSolid.Vertices)
            {
                Vertices.Add(toCoords(v));
            }
            Faces = new List<int[]>();
            foreach(PolygonalFace p in theSolid.Faces)
            {
                Faces.Add(toIndexes(theSolid.Vertices,p));
            }
        }

        public TessellatedSolid generate()
        {
            List<Color> theColors = new List<Color>();
            theColors.Add(SolidColor);
            return new TessellatedSolid(Vertices, Faces, theColors, Units, "", FileName, Comments, Language);
        }

    }
}
