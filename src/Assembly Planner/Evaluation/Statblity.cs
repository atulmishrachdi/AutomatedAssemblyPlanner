//using System.Data;
//using Assembly_Planner;
//using GraphSynth;
//using GraphSynth.Representation;
//using MIConvexHull;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using StarMathLib;
//using TVGL.IOFunctions;
//using TVGL;
//using Assembly_Planner.GraphSynth.BaseClasses;

//namespace AssemblyEvaluation
//{
//    class Stability
//    {
//        public int NumOfMovalbeDire(SubAssembly newSubAsm, AssemblyCandidate c,List<TessellatedSolid> solids)
//        {

//            //so far, g is along +y direction
//            var GDirection = new double[]{0,1,0};
//            var ListOfmovingNodes = new List<Component>();
//            foreach(var p in newSubAsm.Install.Moving.PartNodes)
//            {
//                ListOfmovingNodes.Add((Component)c.graph.nodes.Find(n => n.name.Equals(p)));
//            }
//            if (ListOfmovingNodes.Count == 1)
//            {
//                return 0; 
//            }
//            //DisassemblyDirections.FreeLocalDirectionFinder()
//            var movablecomp = newSubAsm.Install.Moving.PartNodes;
      
//            foreach (var m in movablecomp)
//            {
//                var s = solids.Find(a => a.Name.Equals(m));
//            }
//            var ListOfmovingdire = new List<double[]>();

//            //to check # of parts that can move toward ground, if more than 2, assembly maybe not that statble
//            var demension = new List<int>() {0,0,0,0,0,0};
//            for (var i = 0; i <= 2; i++)
//            {
//                foreach (var comp in ListOfmovingNodes)
//                {
//                    var partmovingdire = DisassemblyDirectionsWithFastener.FreeLocalDirectionFinder(comp, ListOfmovingNodes);
//                    if (partmovingdire.Any(a => a[i].CompareTo(0) < 0))
//                    {
//                        demension[i]++;
//                    }
//                    if (partmovingdire.Any(a => a[i].CompareTo(0) > 0))
//                    {
//                        demension[i + 3]++;
//                    }
//                }
//            }
//            if (demension.Any(a=>a.CompareTo(2)>0))
//            {
//                return 9999; 
//            }
//            // internal statbility  (1. Central mass is outside Convexhull? )  
//            var movablesolids= new List<TessellatedSolid>();
//            foreach (var s in solids)
//            {
//                if (movablecomp.Any(a => a.Equals(s.Name)))
//                    movablesolids.Add(s);
//            }

//            foreach (var ms in movablesolids)
//            {
//                var msnode = c.graph.nodes.Find(n => n.name.Equals(ms.Name));
//                var sss = msnode.arcs;
//                var partmovingdire = DisassemblyDirectionsWithFastener.FreeLocalDirectionFinder((Component)c.graph.nodes.Find(n=>n.name.Equals(ms.Name)), ListOfmovingNodes);
//                //if(partmovingdire.Any(a => a[1].CompareTo(0) > 0))
//                //   continue;

//                var blockingfaces = BlockingDetermination.OverlappingSurfaces.FindAll(f1 => f1.Solid1 == ms || f1.Solid2 == ms);

//                //var blockingfaces = BlockingDetermination.OverlappingSurfaces.FindAll(f1 => f1.Solid1 == ms || f1.Solid2 == ms );
//                blockingfaces.RemoveAll(b=>newSubAsm.Install.Reference.PartNodes.Any(a => a.Contains(b.Solid1.Name) || a.Contains(b.Solid2.Name)));
//               // var blockingfacesConvexhull = MakeProjectConvexHull(blockingfaces, GDirection);
//                var f1pionts= new  List<TVGL.Point>();
//                var f1piontsWithextra= new  List<TVGL.Point>();
//                var f2pionts = new List<TVGL.Point>();
//                foreach (var p in blockingfaces[0].Overlappings[0][0].Vertices)
//                {
//                    var point = new TVGL.Point(p,p.X,p.Y);
//                    f1pionts.Add(point);
//                }
//                var ListOfConvexHullVer = MinimumEnclosure.ConvexHull2D(f1pionts);
//                var x = msnode.localVariables[msnode.localVariables.IndexOf(Constants.Values.CENTEROFMASS) + 1];
//                var y = msnode.localVariables[msnode.localVariables.IndexOf(Constants.Values.CENTEROFMASS) + 2];
//                var newvertex = new TVGL.Vertex[9] ;
//                var newpoint = new TVGL.Point(newvertex[0],x,y);
//                f1pionts.Add(newpoint);
//                var NewListOfConvexHullVer = MinimumEnclosure.ConvexHull2D(f1pionts);
//                if (NewListOfConvexHullVer.Union(ListOfConvexHullVer) != NewListOfConvexHullVer)
//                {
//                    return 9999;
//                } 
//            }




//            return ListOfmovingdire.Count;
//        }

//        //private List<TVGL.Point> MakeProjectConvexHull(List<OverlappedSurfaces> blockingfaces, double[] GDirection)
//        //{
//        //    var facesnormaltoG = new List<OverlappedSurfaces>();
//        //    foreach(var f in blockingfaces)
//        //    {
//        //         if(f.Overlappings.
//        //    }
//        //}
        
//    }
//}
