using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AssemblyEvaluation;

namespace GeometryReasoning
{
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
    public static class VertexReader
    {
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
            string id;
            int index = line.IndexOf(' ');
            if (index == -1) return false;

            id = line.Substring(0, index).ToLower();
            if (id.ToLowerInvariant() != "vertex") return false;

            var values = line.Substring(index + 1).Split(' ').ToList();
            values.RemoveAll(s => string.IsNullOrWhiteSpace(s));
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


        /// <summary>
        /// Parses the ID and values from the specified line.
        /// </summary>
        /// <param name="line">
        /// The line.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="values">
        /// The values.
        /// </param>
        private static Vertex ParseLine(string line)
        {
            string id;
            int index = line.IndexOf(' ');
            if (index == -1) return null;

            id = line.Substring(0, index).ToLower();
            if (id.ToLowerInvariant() != "vertex") return null;

            var values = line.Substring(index + 1).Split(' ');
            if (values.GetLength(0) != 3) return null;

            double x, y, z;
            if (double.TryParse(values[0], out x))
                if (double.TryParse(values[1], out y))
                    if (double.TryParse(values[2], out z))

                        return new Vertex(x, y, z);
            return null;

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
                    if (double.TryParse(words[0], out x)
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
    }
}
