using System;
using System.Linq;

namespace AssemblyEvaluation
{
    class TimeEvaluator
    {
        public const double TRANSFERSPEED = 0.8; // Transfer Speed: 0.1 (m/sec)

        public double EvaluateTimeForInstall(int numberOfConnectingArcs, double travelDistance, double insertionDistance, SubAssembly newSubAsm)
        {
            var movingMass = newSubAsm.Install.Moving.Mass;
            if (movingMass == 0.0) movingMass = 10.0;
            // what's the most you can accelerate or decelerate this mass?
            var maxAcceleration = Constants.MaxForce/movingMass;
            // assuming that the moving subassembly should be stoppable within a small distance (e.g. 1mm), we can find the time to stop
            // x - x_0 = (1/2)*a*t^2
            var timeToStop =Math.Sqrt( Constants.StoppingDistance *2.0/maxAcceleration);
            var travelSpeed = maxAcceleration*timeToStop;
            if (travelSpeed > Constants.MaxTravelSpeed) travelSpeed = Constants.MaxTravelSpeed;

            var insertionSpeed = Constants.MaxInsertionSpeed / numberOfConnectingArcs;

            return insertionDistance / insertionSpeed
                + travelDistance / travelSpeed;
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
