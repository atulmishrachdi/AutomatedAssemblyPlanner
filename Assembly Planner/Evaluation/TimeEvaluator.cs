using System;
using System.Collections.Generic;
using System.Linq;
using GraphSynth.Representation;

namespace AssemblyEvaluation
{
    class TimeEvaluator
    {
        public const double TRANSFERSPEED = 0.8; // Transfer Speed: 0.1 (m/sec)

        public double EvaluateTimeForInstall(List<arc> connectingArcs, double travelDistance, double insertionDistance,
            SubAssembly newSubAsm)
        {
            // get travel time
            var movingMass = newSubAsm.Install.Moving.Mass/1000;
            if (movingMass == 0.0) movingMass = 10.0;
            // what's the most you can accelerate or decelerate this mass?
            var maxAcceleration = Constants.MaxForce/movingMass;
            // assuming that the moving subassembly should be stoppable within a small distance (e.g. 10mm), we can find the time to stop
            // x - x_0 = (1/2)*a*t^2
            var timeToStop = Math.Sqrt(Constants.StoppingDistance*2.0/maxAcceleration);
            var travelSpeed = maxAcceleration*timeToStop;
            if (travelSpeed > Constants.MaxTravelSpeed) travelSpeed = Constants.MaxTravelSpeed;
            //return insertionDistance / insertionSpeed
            //    + travelDistance / travelSpeed;

            // weifeng
            var traveltime = travelDistance/travelSpeed + 2*timeToStop;

            // get oriantation time 
            // need works
        





            // get installation time
            double installationTime = 0;
            var listOfbots = new List<object>();
            var boltinsertSpeed = 1; // just a guess. the unit is mm/s
            foreach (var a in connectingArcs)
            {
                var boltInfoIndex = a.localVariables.IndexOf(-12000);
                if (boltInfoIndex != -1)
                    installationTime = installationTime + a.localVariables.IndexOf(boltInfoIndex + 1) / boltinsertSpeed;
            }
            if(installationTime == 0)
            installationTime = (insertionDistance/1000)/Constants.MaxInsertionSpeed;

            // get handing time 
            // Empirical data from: 
            // Z. Yoosufani, M. Ruddy, G. Boothroyd, "Effect of part symmetry on manual assembly times", 
            // Journal of Manufacturing Systems VOL. 2, No. 2
            // Don't know the code for OOBB, I use the covxhul to get the size.
            double handlingTimePenalty = 1;
            var aa = newSubAsm.Install.Moving.CVXHull.Points;
            double maxX = aa.Max(v => v.Position[0]);
            double minX = aa.Min(v => v.Position[0]);
            double maxY = aa.Max(v => v.Position[1]);
            double minY = aa.Min(v => v.Position[1]);
            double maxZ = aa.Max(v => v.Position[2]);
            double minZ = aa.Min(v => v.Position[2]);
            var lenths = new List<double>();
            lenths.Add(Math.Abs(maxX - minX));
            lenths.Add(Math.Abs(maxY - minY)); 
            lenths.Add(Math.Abs(maxZ - minZ));
            var aaa = lenths.Max(); 
            if (lenths.Max() >= 0 && lenths.Max() < 1)
                handlingTimePenalty = 2;
            else if (lenths.Max() >= 1 && lenths.Max() < 5)
                handlingTimePenalty = 1.13;
            else if (lenths.Max() >= 5 && lenths.Max() < 10)
                handlingTimePenalty = 0.3;
            else if (lenths.Max() >= 10 && lenths.Max() < 15)
                handlingTimePenalty = 0.15;
            else if (lenths.Max() >= 15 && lenths.Max() < 20)
                handlingTimePenalty = 0.1;
            else
                handlingTimePenalty = 1;
            var handlingTime = newSubAsm.Install.Moving.Mass * 0.0022 * (0.125 + 0.011 * handlingTimePenalty);//mass unit is in g(?), multiply 0.0022 to lb.
            var totaltime = traveltime + installationTime + handlingTime;
            return totaltime;
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
