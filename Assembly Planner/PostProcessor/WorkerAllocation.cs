using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;

namespace Assembly_Planner
{
    internal class WorkerAllocation
    {
        static Dictionary<string, List<string>> SuccedingTasks;
        static Dictionary<string, List<string>> PrecedingTasks;
        static Dictionary<string, double> FollowingTime;
        static Dictionary<string, double> TaskTime;

        internal static void Run(List<AssemblyCandidate> solutions, Dictionary<string, double> reorientation)
        {
            foreach (var c in solutions.Where(c => c != null))
                Allocation(c, reorientation);
        }

        private static void Allocation(AssemblyCandidate candidate, Dictionary<string, double> reorientation)
        {
            var WorkTimes = new List<double>();
            var prevOverallTime = double.NaN;
            var maxWorkers = new WorkersClass[]{};
            for (int numberofWorkers = 1; ; numberofWorkers++)
            {
                TaskTime = new Dictionary<string, double>();
                BuildingTaskTimeDictionary(candidate.Sequence.Subassemblies[0], 0.0);

                FollowingTime = new Dictionary<string, double>();
                BuildingFollowingTimeDictionary(candidate.Sequence.Subassemblies[0], 0.0);


                SuccedingTasks = new Dictionary<string, List<string>>();
                BuildSuccedingTaskDictionary(candidate.Sequence.Subassemblies[0], new List<string>());

                PrecedingTasks = BuildPrecedingTaskDictionary(SuccedingTasks);

                var tasks2 = new List<TasksClass>();
                foreach (var taskname in TaskTime.Keys)
                {
                    var task = new TasksClass
                    {
                        Name = taskname,
                        PrecedingTask = PrecedingTasks[taskname],
                        SucceedingTask = SuccedingTasks[taskname],
                        FollowingTime = FollowingTime[taskname],
                        TaskTime = TaskTime[taskname]
                    };
                    tasks2.Add(task);
                }
                //tasks = tasks.OrderByDescending(t => t.FollowingTime).ToList();

                var worker = new WorkersClass[numberofWorkers];
                for (var i = 0; i < numberofWorkers; i++)
                    worker[i] = new WorkersClass();

                var counter = 0;
                while (counter < tasks2.Count) //checkPrecedingTasks(tasks)  /*tasks.Last().PrecedingTask.Count != 0*/)
                {
                    worker = worker.OrderBy(t => t.Time).ToArray();
                    tasks2 = tasks2.OrderBy(t => t.StartTime).ToList();
                    foreach (var sortedtask in tasks2)
                    {
                        if (sortedtask.PrecedingTask.Count == 0)
                        {
                            worker[0].Tasks.Add(sortedtask.Name);
                            worker[0].Time = sortedtask.TaskTime + Math.Max(sortedtask.StartTime, worker[0].Time); //sortedtask.StartTime; //instead of starttime, I must write maximum of start time or rorker time
                            worker[0].TasksTimes.Add(sortedtask.StartTime);

                            counter++;
                            sortedtask.StartTime = double.PositiveInfinity;
                            foreach (var tobedeleted in tasks2)
                            {
                                if (tobedeleted.PrecedingTask.Contains(sortedtask.Name))
                                {
                                    if (tobedeleted.StartTime < worker[0].Time)
                                        tobedeleted.StartTime = worker[0].Time;
                                    tobedeleted.PrecedingTask.Remove(sortedtask.Name);

                                }
                            }
                            break;
                        }
                    }
                }
                var OverallTime = worker.Max(w => w.Time);
                if (prevOverallTime != OverallTime)
                {
                    prevOverallTime = OverallTime;
                    WorkTimes.Add(OverallTime);
                }
                else
                {
                    maxWorkers = worker;
                    break;
                }
            }
            
            // Instruction Manual
            for (var i = 0; i < maxWorkers.Count(); i++)
            {
                var t = i + 1;
                var worker = maxWorkers[i];
                Console.WriteLine(" Worker " + t+" :");
                for (var j = 0; j < worker.Tasks.Count; j++)
                {
                    Console.WriteLine("     At the time: " + worker.TasksTimes[j]); 
                    Console.WriteLine("         Task: " + worker.Tasks[j]);
                    Console.WriteLine("         Footprint Face: " + reorientation[worker.Tasks[j]]);
                }
            }
            CheckValuesWithCandidate(candidate.f3, WorkTimes[0], candidate.f4, WorkTimes.Last());
            while (candidate.performanceParams.Count > 3) candidate.performanceParams.RemoveAt(3);
            candidate.performanceParams.AddRange(WorkTimes);
        }

        private static void CheckValuesWithCandidate(double totalTime1, double totalTime2,
            double makeSpan1, double makeSpan2)
        {
            if (Math.Abs(totalTime1 - totalTime2) > Constants.SameWithinError || Math.Abs(makeSpan1 - makeSpan2) > Constants.SameWithinError)
                throw new Exception("Times do not match.");
        }



        public static Dictionary<string, List<string>> BuildPrecedingTaskDictionary(Dictionary<string, List<string>> SuccedingTasks)
        {
            var precedingDic = new Dictionary<string, List<string>>();
            foreach (var chosentask in SuccedingTasks.Keys)
            {
                var PrecedingTasksForChosenTask = SuccedingTasks.Keys.Where(a => SuccedingTasks[a].Contains(chosentask)).ToList();
                precedingDic.Add(chosentask, PrecedingTasksForChosenTask);
            }
            return precedingDic;
        }



        private static void BuildingTaskTimeDictionary(SubAssembly subAssembly, double p)
        {
            if (subAssembly == null) return;
            TaskTime.Add(subAssembly.Name, subAssembly.Install.Time);

            BuildingTaskTimeDictionary(subAssembly.Install.Moving as SubAssembly, p);
            BuildingTaskTimeDictionary(subAssembly.Install.Reference as SubAssembly, p);
        }

        private static void BuildingFollowingTimeDictionary(SubAssembly subAssembly, double p)
        {
            if (subAssembly == null) return;
            FollowingTime.Add(subAssembly.Name, p);
            p = p + subAssembly.Install.Time;

            BuildingFollowingTimeDictionary(subAssembly.Install.Moving as SubAssembly, p);
            BuildingFollowingTimeDictionary(subAssembly.Install.Reference as SubAssembly, p);


        }

        public static void BuildSuccedingTaskDictionary(SubAssembly subAssembly, List<string> successors)
        {
            if (subAssembly == null) return;
            SuccedingTasks.Add(subAssembly.Name, successors);
            //BestFace.SucTasks.Add(subAssembly.Name, successors);

            var subSubAssembly = subAssembly.Install.Moving;
            var subSuccessors = new List<string>(successors);
            subSuccessors.Add(subAssembly.Name);
            BuildSuccedingTaskDictionary(subSubAssembly as SubAssembly, subSuccessors);


            subSubAssembly = subAssembly.Install.Reference;
            subSuccessors = new List<string>(successors);
            subSuccessors.Add(subAssembly.Name);
            BuildSuccedingTaskDictionary(subSubAssembly as SubAssembly, subSuccessors);

        }

    }
}
