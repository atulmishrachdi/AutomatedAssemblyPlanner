using System.IO;
using System.Linq;
using GraphSynth;
using GraphSynth.Representation;
using System.Collections.Generic;
using AssemblyEvaluation;
using MIConvexHull;
using System.Threading.Tasks;
using System;
using System.Runtime.InteropServices;

namespace GeometryReasoning
{
    public class InputData
    {
        public designGraph graphAssembly; //this is the graphsynth graph of the 
        //assembly
        public string InputDir;

        public Dictionary<string, ConvexHull<Vertex, DefaultConvexFace<Vertex>>> ConvexHullDictionary;
        public Dictionary<string, double[]> BoundingBoxDictionary;



        public InputData(GlobalSettings settings)
        {
            graphAssembly = settings.seed;
            InputDir = settings.InputDirAbs;
            //var basicFiler = new BasicFiler(InputDir, "output", "");
            //if (File.Exists(InputDir + "input.gxml"))
            //{
            //    settings.seed = graphAssembly = (designGraph)basicFiler.Open(InputDir + "input.gxml")[0];
            //   Parallel.Invoke(LoadCADData, LoadAndSaveTesselatedPartFiles);
            LoadAndSaveTesselatedPartFiles(); 
            //BuildFromSAT();
            MakeBBDictionaryFromCVXHullDictionary();
            //}
            //else
            //{
            //    graphAssembly = new designGraph();
            //   Parallel.Invoke(LoadCADData, LoadOrMakeLiaisonData, LoadAndSaveTesselatedPartFiles);
            //  BuildFromSAT();
            //   MakeBBDictionaryFromCVXHullDictionary();
            //   if (!BlockingDetermination.DefineBlocking(graphAssembly, ConvexHullDictionary, BoundingBoxDictionary))
            //       return;
            //basicFiler.Save(InputDir + "input.gxml", graphAssembly);
            //  settings.seed = graphAssembly;
            //}
            DataInterface.TerminateACIS();
        }

        /// <summary>
        /// This function loads the .SAT file from the directory
        /// and returns the parts of the assembly.
        /// </summary>
        private void LoadCADData()
        {
            try
            {
                DataInterface.Initialize(InputDir + Constants.CADAssemblyFileName);
            }
            catch (Exception exception)
            { Console.WriteLine("Unable to LoadCADData because: " + exception.Message); }
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
                    SearchIO.output("CVXHull for " + fileInfo.Name);
                    ConvexHullDictionary.Add(Path.GetFileNameWithoutExtension(fileInfo.Name),
                        ConvexHull.Create(vertices));
                    SearchIO.output("...done");
                }
            }
            //);
            fis = di.EnumerateFiles("*.txt");
            foreach (var fileInfo in fis)
            {
                if (!ConvexHullDictionary.ContainsKey(Path.GetFileNameWithoutExtension(fileInfo.Name)))
                {
                    SearchIO.output("CVXHull for " + fileInfo.Name);
                    if (STLGeometryFunctions.ReadFromTXTFile(fileInfo.Open(FileMode.Open), out vertices))
                        ConvexHullDictionary.Add(Path.GetFileNameWithoutExtension(fileInfo.Name),
                            ConvexHull.Create(vertices));
                    SearchIO.output("...done");
                }
            }
        }

        /// <summary>
        /// This function will load in the liaison graph if it is provided
        /// and then create the GraphSynth graph. If it is not provided, then
        /// we will do the clash detection to create it.
        /// </summary>
        private void LoadOrMakeLiaisonData()
        {
            LiaisonGraphCreator.Make(graphAssembly, InputDir + Constants.AssemblyXMLFileName);
        }

        private void BuildFromSAT()
        {
            var numParts = graphAssembly.nodes.Count;
            if (numParts <= 1) numParts = DataInterface.NumberOfParts();
            for (int i = 0; i < numParts; i++)
            {
                var partName = "";
                if (i >= graphAssembly.nodes.Count || graphAssembly.nodes[i].name[0] == 'n')
                {
                    IntPtr intPtr = DataInterface.NameOfPart(i);
                    partName = Marshal.PtrToStringAnsi(intPtr);
                    //  if (!graphAssembly.nodes.Exists(n => n.name == partName))
                    graphAssembly.addNode(partName);
                }
                else partName = graphAssembly.nodes[i].name;
                ConvexHull<Vertex, DefaultConvexFace<Vertex>> cvxhull = null;
                if (!ConvexHullDictionary.ContainsKey(partName))
                {
                    Console.Write("Faceting part name: " + partName + "...");
                    int pointNum = DataInterface.VerticesOfPart(i);
                    Console.Write("done...");
                    var vertsInARow = new double[pointNum];
                    DataInterface.GetPointDouble(pointNum, vertsInARow);

                    var verts = new List<Vertex>();
                    for (int j = 0; j < pointNum; j += 3)
                        verts.Add(new Vertex(vertsInARow[j], vertsInARow[j + 1], vertsInARow[j + 2]));
                    STLGeometryFunctions.WriteVerticesToText(verts, InputDir + partName + ".txt");
                    Console.Write("creating cvx hull from " + pointNum + " points...");
                    if (verts.Count == 0) continue;
                    cvxhull = ConvexHull.Create(verts);
                    Console.WriteLine("...done (" + cvxhull.Points.Count() + " pts).");
                    ConvexHullDictionary.Add(partName, cvxhull);
                    verts.Clear();
                }
                else cvxhull = ConvexHullDictionary[partName];
                var partNode = graphAssembly[partName];
                /* the one thing we can get from CVX hull that we might not have in the 
                 * node is the center position - or what is assumed to be the center of gravity */
                if (!partNode.localVariables.Contains(Constants.TRANSLATION))
                {
                    partNode.localVariables.Add(Constants.TRANSLATION);
                    partNode.localVariables.Add(cvxhull.Points.Average(v => v.Position[0]));//x
                    partNode.localVariables.Add(cvxhull.Points.Average(v => v.Position[1]));//y
                    partNode.localVariables.Add(cvxhull.Points.Average(v => v.Position[2]));//z;
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
