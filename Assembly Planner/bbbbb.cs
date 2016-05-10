//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using AssemblyEvaluation;
//using GraphSynth.Representation;
//using TVGL;
//using TVGL.IOFunctions;

//namespace Assembly_Planner
//{
//    public class bbbbb
//    {
//        public static Dictionary<string, List<TessellatedSolid>> Solids = new Dictionary<string, List<TessellatedSolid>>();
//        public static Dictionary<string, List<TessellatedSolid>> SolidsNoFastener = new Dictionary<string, List<TessellatedSolid>>();
//        private static List<int> globalDirPool = new List<int>();
//        public static designGraph AssemblyGraph;
//    //    public static ReportStatus StatusReporter;
//        public static string CSVPath;
//        public const double MeshMagnifier = 1000.0;

//        static void Main(string[] args)
//        {
//            var s = 1;
//            ExecutePlanGeneration();

//        }

    
     
//        public static AssemblySequence ExecutePlanGeneration()
//        {
//            /////////////pro
//            //var inputDir = " C:/WeifengDOC/Desktop/4";

//            //  var inputDir = " C:/WeifengDOC/Desktop/cr";
//            //x var inputDir = " C:/WeifengDOC/Desktop/cnr";
//            //var inputDir = " C:/WeifengDOC/Desktop/sandwich";
//            //x   var inputDir = " C:/WeifengDOC/Desktop/forppt/cj";
//            //x  var inputDir = " C:/WeifengDOC/Desktop/forppt/rj";
//            //x var inputDir = " C:/WeifengDOC/Desktop/forppt/pj";
//            //var inputDir = " C:/WeifengDOC/Desktop/forppt/sj";
//            //x var inputDir = " C:/WeifengDOC/Desktop/forppt/conej";
//            //x var inputDir = " C:/WeifengDOC/Desktop/forppt/planarj";
//            // var inputDir = " Z:/Windows.Documents/Desktop/stabilitytest/3";


//            /////////////lab
//            /// 


//            var inputDir = " C:/WeifengDOC/Desktop/sim3";

//            //  var inputDir = " C:/WeifengDOC/Desktop/cr";
//            //x var inputDir = " C:/WeifengDOC/Desktop/cnr";
//            //var inputDir = " C:/WeifengDOC/Desktop/sandwich";
//            //x   var inputDir = " C:/WeifengDOC/Desktop/forppt/cj";
//            //x  var inputDir = " C:/WeifengDOC/Desktop/forppt/rj";
//            //x var inputDir = " C:/WeifengDOC/Desktop/forppt/pj";
//            //var inputDir = " C:/WeifengDOC/Desktop/forppt/sj";
//            //x var inputDir = " C:/WeifengDOC/Desktop/forppt/conej";
//            //x var inputDir = " C:/WeifengDOC/Desktop/forppt/planarj";
//            //var inputDir = " Z:/Windows.Documents/Desktop/pumpsolidworks/";




//            var solids = GetSTLs(inputDir);
//            Solids.Clear();
//            foreach (var solid in solids)
//                Solids.Add(solid.Name, new List<TessellatedSolid>() { solid });
//            DisassemblyDirectionsWithFastener.RunGeometricReasoning(Solids);
//            var assemblyGraph = new designGraph();
//            globalDirPool = DisassemblyDirectionsWithFastener.RunGraphGeneration(assemblyGraph, Solids);
//            NonadjacentBlockingWithPartitioning.Run(assemblyGraph, Solids, globalDirPool);
//            //   GraphSaving.SaveTheGraph(assemblyGraph);
//            //      var fileName = "C:/WeifengDOC/Desktop/graph/FPM.gxml";
//            // var  assemblyGraph = (designGraph)GraphSaving.OpenSavedGraph(fileName)[0];
//            AssemblyGraph = assemblyGraph;
//            globalDirPool = GraphSaving.RetrieveGlobalDirsFromExistingGraph(assemblyGraph);
//            var AssemblyEvaluator = new EvaluationForBinaryTree(Solids);
//            //    RotateBeforeTreeSearch.Run(assemblyGraph);
//            RotateBeforeTreeSearch.GenerateReactionForceInfo(AssemblyGraph);
//            var assemblyPlan = RecursiveOptimizedSearch.Run(assemblyGraph, Solids, globalDirPool);
//            //if (statusReporter.CancelProcess) return null;
//            //OptimalOrientation.Run(assemblyPlan, statusReporter);
//            //  OptimalOrientation2.Run(assemblyPlan, statusReporter,assemblyGraph);
//            bool finish = false;
//           // printTreeeeee(assemblyPlan.Subassemblies[0],);
//            return assemblyPlan;
//        }

//        //private static void printTreeeeee(SubAssembly tree, bool finish)
//        //{
//        //    var instalside = (SubAssembly)tree.Install.Reference;
//        //    instalside.Install.Reference.
//        //    var movingside = (SubAssembly)tree.Install.Moving;
//        //    var movingnamelist = movingside.PartNames;
//        //    var refnamelist = instalside.PartNames;
//        //    var leafnumber = 2;


//        //    for (int i = 0; i < movingnamelist.Count; i++)
//        //    {
//        //        Console.Write(movingnamelist[i] + " && ");
//        //    }

//        //    Console.Write("______||______");

//        //    for (int i = 0; i < refnamelist.Count; i++)
//        //    {
//        //        Console.Write(refnamelist[i] + " @@ ");
//        //    }

//        //    Console.WriteLine();
//        //    if (!finish)
//        //        printTreeeeee(instalside);
//        //    printTreeeeee(movingside);

//        //}
//        private static void ChangeSolidNameFromGeomUUIDtoPartUUID(List<TessellatedSolid> solids,
//            Dictionary<string, HashSet<TessellatedSolid>> subAssemblies)
//        {
//            foreach (var solid in solids)
//            {
//                foreach (var subAssem in subAssemblies.Keys.Where(subAssem => subAssemblies[subAssem].Contains(solid)))
//                {
//                    solid.Name = subAssem;
//                    break;
//                }
//            }
//        }
//        private static List<TessellatedSolid> GetSTLs(string InputDir)
//        {
//            Console.WriteLine("Loading STLs ....");
//            var parts = new List<TessellatedSolid>();
//            var di = new DirectoryInfo(InputDir);
//            var fis = di.EnumerateFiles("*.STL");
//            // Parallel.ForEach(fis, fileInfo =>
//            foreach (var fileInfo in fis)
//            {
//                var ts = IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name);
//                //ts.Name = ts.Name.Remove(0, 1);
//                //lock (parts) 
//                parts.Add(ts[0]);
//            }
//            return parts;
//        }
//    }
//}
