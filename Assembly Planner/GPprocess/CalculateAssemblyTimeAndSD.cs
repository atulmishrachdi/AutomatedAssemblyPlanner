using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using MIConvexHull;
using Assembly_Planner.GraphSynth.BaseClasses;
using StarMathLib;
using AssemblyEvaluation;
using TVGL;
using Assembly_Planner;

namespace GPprocess
{
    public class CalculateAssemblyTimeAndSD
    {
        public static List<double> Run(designGraph graph,SubAssembly sub)
        {

            ///set up 
            var refNodes = sub.Install.Reference.PartNodes.Select(n => (Component)graph[n]).ToList();
            var movingNodes = sub.Install.Moving.PartNodes.Select(n => (Component)graph[n]).ToList();
            var install = new[] { refNodes, movingNodes };
            var connectingArcs = graph.arcs.Where(c=> c is Connection).Cast<Connection>().Where(a => ((movingNodes.Contains(a.To) && refNodes.Contains(a.From))
                                                         || (movingNodes.Contains(a.From) && refNodes.Contains(a.To))))
                                                        .ToList();
            double insertionDistance;
            var insertionDirection = AssemblyEvaluator.FindPartDisconnectMovement(connectingArcs, refNodes, out insertionDistance);

            var vt = sub.Install.Moving.CVXHull.Points.Select(p => new TVGL.Vertex(p.Position)).ToList();
            var movingobb = TVGL.MinimumEnclosure.OrientedBoundingBox(vt);


            //////////////////////////////////////////////
            //1 Rotate
            ////////////////////////
          
            //////////////////////////////////////////////
            //2 install a. travel
            ////////////////////////
            var travelmass = sub.Install.Moving.Mass;
            var travelvol = movingobb;


            var obdataX = readdata.read("D:\\Desktop\\testdata\\MassAndVolorigin.csv", 2);
            var obdataY = readdata.read("D:\\Desktop\\testdata\\Timeorgin.csv");
            var OptimPara = new double[4] { 5.430416, 15.3306, 16.16355, 0.09315 };



            var testpoints = new double[1, 2];
            testpoints[0, 0] = travelmass;
            testpoints[0, 1] = sub.Install.Moving.Volume;
           // testpoints[0, 1] = travelvol.Volume;
            //testpoints[0, 0] = mass/1000000;
            //testpoints[0, 1] = vol/1000000;
               var newm = ThreeDinput.newMDGetMeanAndVar(obdataX, obdataY, testpoints, OptimPara);
            var predicMean = ThreeDinput.Getmean(newm);
            var predicVar =ThreeDinput.Getvar(newm);
            //////////////////////////////////////////////
            //1 install b. instert
            ////////////////////////


    

            //////////////////////////////////////////////
            //2 install b. instert
            ////////////////////////


            //////////////////////////////////////////////
            //3 secure
            ////////////////////////
     


      
            var pp = new List<double>();
            pp.Add(predicMean[0]);
            pp.Add(predicVar[0]);
            return pp;
        }
    }
}
