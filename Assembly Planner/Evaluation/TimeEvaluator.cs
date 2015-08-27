using System;
using System.Collections.Generic;
using System.Linq;
using Assembly_Planner;
using GraphSynth.Representation;

namespace AssemblyEvaluation
{
    class TimeEvaluator
    {
        public const double TRANSFERSPEED = 0.8; // Transfer Speed: 0.1 (m/sec)
        
        public List<double> EvaluateTimeAndSDForInstall(List<Connection> connectingArcs, double travelDistance, double insertionDistance, SubAssembly newSubAsm)
        {
            // 1. Install          units are in kg and m 
            // a. travel time

            var TimeAndSD = new List<double>();
            var movingMass = newSubAsm.Install.Moving.Mass/1000;
            var movingVol = newSubAsm.Install.Moving.Volume/1000000;
            
            //var movingVol = 1;
            var movingSpeed = new double();
            //var movingSpeed = 15.0;
            if (movingMass <= 1)
                movingSpeed = 1;
            else if (movingSpeed > 1 && movingSpeed <= 15)
                movingSpeed = movingMass * (-8.0 / 140000.0) + 148.0 / 140.0;
            else movingSpeed = 0.2;
            var traveltime = travelDistance/movingSpeed ;
            var DisTraveltime = 0.05;

            // b. contact info 
            var insertiontime = new double();
            var aligmentime = new double();
            var DisAligmentime = new double();
            if (movingVol <= 0.000001)
            {
                aligmentime = -4008 * movingVol + 5;
                DisAligmentime = 1;
            }
            else if (movingVol <= 0.001)
            {       
                aligmentime = 3;
                DisAligmentime = 1;
            }
                else
            {
                aligmentime = 54.1 * movingVol + 2.95;
                DisAligmentime = movingVol*50+movingMass/100;
            }

            if (insertionDistance != 0)
                insertiontime = (insertionDistance / 1000) / Constants.MaxInsertionSpeed;

            double installationTime = traveltime + aligmentime + insertiontime;
            double Disinstalltime = DisTraveltime + DisAligmentime;

            // c. still need statbility


            //2. Secure:
            var bolttime = new double();
            var DisBolttime = new double();
            foreach (var a in connectingArcs.Where(a=>a.localVariables.Contains(DisConstants.BoltDepth)))
            {
                var boltLength = a.localVariables[a.localVariables.IndexOf(DisConstants.BoltDepth)+1];
                bolttime = bolttime + boltLength / Constants.boltinsertSpeed;
                DisBolttime = DisBolttime + bolttime-boltLength / (Constants.boltinsertSpeed + 0.2);
            }

            installationTime = installationTime + bolttime;
            Disinstalltime = Disinstalltime + DisBolttime;
            TimeAndSD.Add(installationTime);
            TimeAndSD.Add(Disinstalltime);
            return TimeAndSD;
        }


        internal double EvaluateTimeOfLongestBranch(AssemblySequence assemblySequence)
        {
            return assemblySequence.Subassemblies.Max(sub => MaxTimeDownTreeRecurse(sub));
        }

        private double MaxTimeDownTreeRecurse(Part sub)
        {
            if (!(sub is SubAssembly)) return 0.0;
            var refTime =  MaxTimeDownTreeRecurse(((SubAssembly)sub).Install.Reference);
            var movTime = MaxTimeDownTreeRecurse(((SubAssembly)sub).Install.Moving);
            return Math.Max(refTime, movTime) + ((SubAssembly) sub).Install.Time;
        }


    }
}
