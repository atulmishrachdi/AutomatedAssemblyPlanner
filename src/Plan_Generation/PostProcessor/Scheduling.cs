using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseClasses.AssemblyEvaluation;

namespace Plan_Generation
{
    // The implemented worker allocation code is using s heuristic which is not assigning tasks optimally.
    // In other word, it doesn't guarabtee the minimum "Makespan" or "Critical Time".
    // foreach assembly tree (solution) we need to 
    //    1. Find the critical path.     P(infinity)|preec|Cmax
    //        1.1 Calculating the earliest ending time of each task. The end time of the last task is Critical Time
    //        1.2 Calculate the latest ending time of each task.
    //        1.3 Tasks with the same earliest and latest ending times are critical tasks (critical path)
    //    2. Choose a task
    //        2.1 Take a task with no preceding task
    //        2.2 Tasks on critical path always have priority
    //        2.3 For the rest of tasks, consider the following metrics: 
    //             2.2.1 smaller latest ending time is better
    //             2.2.1 smaller difference between latest and earlist ending times is better
    //    3. Assign the task to a machine
    //        3.1 Tasks whose precedings are done can be assigned to any machine
    //        3.2 Gaps are allowed. It doesn't need to be non-delay
    
    internal class Scheduling
    {
        internal static void Run(List<AssemblyCandidate> solutions, Dictionary<string, double> reorientation)
        {
            foreach (var c in solutions.Where(c => c != null))
                Allocation(c);
        }

        private static void Allocation(AssemblyCandidate candidate)
        {
            var earliestEndingTime = EarliestEndingTimeCalculator(candidate);
            var latestEndingTime = LatestEndingTimeCalculator(candidate);
            double criticalTime;
            var criticalPath = CriticalPathFinder(earliestEndingTime, latestEndingTime, out criticalTime);
            var availableTasks = new List<string>();

            while (availableTasks.Count > 0)
            {
                
            }
        }

        private static Dictionary<string, double> EarliestEndingTimeCalculator(AssemblyCandidate candidate)
        {
            throw new NotImplementedException();
        }

        private static Dictionary<string, double> LatestEndingTimeCalculator(AssemblyCandidate candidate)
        {
            throw new NotImplementedException();
        }

        private static List<string> CriticalPathFinder(Dictionary<string, double> earliestEndingTime,
            Dictionary<string, double> latestEndingTime, out double criticalTime)
        {
            throw new NotImplementedException();
        }
    }
}
