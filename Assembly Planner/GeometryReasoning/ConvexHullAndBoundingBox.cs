using System.IO;
using System.Linq;
using GeometryReasoning;
using GraphSynth;
using GraphSynth.Representation;
using System.Collections.Generic;
using AssemblyEvaluation;
using MIConvexHull;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices;

namespace Assembly_Planner
{
    public class ConvexHullAndBoundingBox
    {
        public designGraph graphAssembly;
        public string InputDir;
        public Dictionary<string, ConvexHull<Vertex, DefaultConvexFace<Vertex>>> ConvexHullDictionary;
        public Dictionary<string, double[]> BoundingBoxDictionary;

        public ConvexHullAndBoundingBox(designGraph graphAssembly1)
        {
            graphAssembly = graphAssembly1;
            InputDir = "../../../Test/Pump Assembly";
            LoadAndSaveTesselatedPartFiles(); 
            MakeBBDictionaryFromCVXHullDictionary();
        }

        private void LoadAndSaveTesselatedPartFiles()
        {
            ConvexHullDictionary = new Dictionary<string, ConvexHull<Vertex, DefaultConvexFace<Vertex>>>();
            var di = new DirectoryInfo(InputDir);
            List<Vertex> vertices;
            IEnumerable<FileInfo> fis = di.EnumerateFiles("*.STL");
            //Parallel.ForEach(fis, fileInfo =>
            foreach (var fileInfo in fis)
            {
                if (STLGeometryFunctions.ReadFromSTLFile(fileInfo.Open(FileMode.Open), out vertices))
                {
                    ConvexHullDictionary.Add(Path.GetFileNameWithoutExtension(fileInfo.Name),
                        ConvexHull.Create(vertices));
                }
            }
            //);
            fis = di.EnumerateFiles("*.txt");
            foreach (var fileInfo in fis)
            {
                if (!ConvexHullDictionary.ContainsKey(Path.GetFileNameWithoutExtension(fileInfo.Name)))
                {
                    if (STLGeometryFunctions.ReadFromTXTFile(fileInfo.Open(FileMode.Open), out vertices))
                        ConvexHullDictionary.Add(Path.GetFileNameWithoutExtension(fileInfo.Name),
                            ConvexHull.Create(vertices));
                }
            }
        }
        private void MakeBBDictionaryFromCVXHullDictionary()
        {
            BoundingBoxDictionary = new Dictionary<string, double[]>();
            foreach (var part in ConvexHullDictionary)
            {
                var points = part.Value.Points;
                BoundingBoxDictionary.Add(part.Key, new[]
                {                                   
                    points.Min(v=>v.Position[0]),      //xmin
                    points.Max(v=>v.Position[0]),      //xmax
                    points.Min(v=>v.Position[1]),      //ymin
                    points.Max(v=>v.Position[1]),      //ymax
                    points.Min(v=>v.Position[2]),      //zmin
                    points.Max(v=>v.Position[2])       //zmax
                });
            }
        }

    }
}
