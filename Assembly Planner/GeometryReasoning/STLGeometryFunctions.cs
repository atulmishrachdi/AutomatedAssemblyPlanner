using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AssemblyEvaluation;
using MIConvexHull;
using StarMathLib;

namespace GeometryReasoning
{
    public static class STLGeometryFunctions
    {

        public static double findMaxPlaneHeightInDirection(IEnumerable<Vertex> points, Vector direction)
        {
            return points.Max(point => StarMath.dotProduct(direction.Position, point.Position, 3));
        }
        public static double findMinPlaneHeightInDirection(IEnumerable<Vertex> points, Vector direction)
        {
            return points.Min(point => StarMath.dotProduct(direction.Position, point.Position, 3));
        }

        public static double pointRaySquareDistance(Ray R, IVertex P) // Equivalent to Cross Product
        {
            Vector v = R.MakeVectorTo(P);
            var cross = StarMath.crossProduct(R.Direction, v.Position);
            return StarMath.norm2(cross);
        }

        public static double findConeHalfAngle(Ray R, Vertex P)
        {
            Vector v = R.MakeVectorTo(P);
            v.NormalizeInPlace();
            return StarMath.dotProduct(R.Direction, v.Position, 3);
        }

        public static bool RayIntersectsWithFace(Ray ray, DefaultConvexFace<Vertex> face)
        {
            if (Math.Abs(StarMath.dotProduct(ray.Direction, face.Normal, 3)) < Constants.Values.NearlyParallelFace) return false;
            var inPlaneVerts = new Vertex[face.Vertices.GetLength(0)];
            var negativeDirCounter = 3;
            for (int i = 0; i < face.Vertices.GetLength(0); i++)
            {
                var vectFromRayToFacePt = new Vector(ray.Position);
                vectFromRayToFacePt = vectFromRayToFacePt.MakeVectorTo(face.Vertices[i]);
                var dxtoPlane = StarMath.dotProduct(ray.Direction, vectFromRayToFacePt.Position, 3);
                if (dxtoPlane < 0) negativeDirCounter--;
                if (negativeDirCounter == 0) return false;
                inPlaneVerts[i] = new Vertex(StarMath.add(face.Vertices[i].Position, StarMath.multiply(-dxtoPlane, ray.Direction, 3), 3));
            }  
            if (inPlaneVerts.Min(v => v.Position[0]) > ray.Position[0]) return false;
            if (inPlaneVerts.Max(v => v.Position[0]) < ray.Position[0]) return false;
            if (inPlaneVerts.Min(v => v.Position[1]) > ray.Position[1]) return false;
            if (inPlaneVerts.Max(v => v.Position[1]) < ray.Position[1]) return false;
            if (inPlaneVerts.Min(v => v.Position[2]) > ray.Position[2]) return false;
            if (inPlaneVerts.Max(v => v.Position[2]) < ray.Position[2]) return false;
            if (inPlaneVerts.GetLength(0)>3) return true;
            var crossProductsToCorners = new List<double[]>();
            for (int i = 0; i < 3; i++)
            {
                var j = (i == 2) ? 0 : i + 1;
                var crossProductsFrom_i_To_j = StarMath.crossProduct(StarMath.normalizeInPlace(StarMath.subtract(inPlaneVerts[i].Position, ray.Position,3),3),
                    StarMath.normalizeInPlace(StarMath.subtract(inPlaneVerts[j].Position, ray.Position),3));
                if (StarMath.norm2(crossProductsFrom_i_To_j, true) < Constants.Values.NearlyOnLine) return false;
                crossProductsToCorners.Add(crossProductsFrom_i_To_j);
            }
            for (int i = 0; i < 3; i++)
            {
                var j = (i == 2) ? 0 : i + 1;
                if (StarMath.dotProduct(crossProductsToCorners[i], crossProductsToCorners[j], 3) <= 0) return false;
            }
            return true;
        }


        // --------------------------------------------------------------------------------------------------------------------
        // <copyright file="StLReader.cs" company="Helix 3D Toolkit">
        //   http://helixtoolkit.codeplex.com, license: MIT
        // </copyright>
        // <summary>
        //   Provides an importer for StereoLithography .StL files.
        // </summary>
        // --------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Provides an importer for StereoLithography .StL files.
        /// </summary>
        /// <remarks>                                                                                                                                               
        /// The format is documented on <a href="http://en.wikipedia.org/wiki/STL_(file_format)">Wikipedia</a>.
        /// </remarks>
        public static Boolean ReadFromSTLFile(Stream stream, out List<Vertex> vertices)
        {
            vertices = new List<Vertex>();
            var reader = new StreamReader(stream);
            var counter = 0;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    continue;
                }

                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("\0") || line.StartsWith("#") || line.StartsWith("!")
                    || line.StartsWith("$"))
                {
                    continue;
                }
                Vertex vertex;
                if (TryParseLine(line, out vertex))
                {
                    vertices.Add(vertex);
                    counter++;
                }
            }
            return counter >= 4;
        }

        private static bool TryParseLine(string line, out Vertex vertex)
        {
            vertex = null;
            int index = line.IndexOf(' ');
            if (index == -1) return false;

            string id = line.Substring(0, index).ToLower();
            if (id.ToLowerInvariant() != "vertex") return false;

            var values = line.Substring(index + 1).Split(' ').ToList();
            values.RemoveAll(string.IsNullOrWhiteSpace);
            if (values.Count != 3) return false;

            double x, y, z;
            if (double.TryParse(values[0], out x))
                if (double.TryParse(values[1], out y))
                    if (double.TryParse(values[2], out z))
                    {
                        vertex = new Vertex(x, y, z);
                        return true;
                    }
            return false;
        }


        internal static Boolean ReadFromTXTFile(FileStream stream, out List<Vertex> vertices)
        {
            bool ContinueLoop = true;
            vertices = new List<Vertex>();
            var sr = new StreamReader(stream);
            var counter = 0;
            while (ContinueLoop)
            {
                String line = sr.ReadLine();
                if (line != null)
                {
                    double x, y, z;
                    string[] words = line.Split(' ', ',');
                    if (words.GetLength(0)>=3
                        && double.TryParse(words[0], out x)
                        && double.TryParse(words[1], out y)
                        && double.TryParse(words[2], out z))
                    {
                        vertices.Add(new Vertex(x, y, z));
                        counter++;
                    }
                    else ContinueLoop = false;
                }
                else
                {
                    ContinueLoop = false;
                }
            }
            return counter >= 4;
        }

        internal static void WriteVerticesToText(List<Vertex> verts, string fileName)
        {
            try
            {
                var fileInfo = new FileInfo(fileName);
                var fileStream = fileInfo.Open(FileMode.Create);
                var sw = new StreamWriter(fileStream);
                foreach (var vert in verts)
                {
                    sw.WriteLine(vert.Position[0] + " " + vert.Position[1] + " " + vert.Position[2]);
                }
                sw.Close();
                fileStream.Close();
            }
            catch(Exception e)
            { Console.WriteLine("Unable to write point cloud for "+fileName+" because of the following error: "+e.Message);}
        }
    }
}
