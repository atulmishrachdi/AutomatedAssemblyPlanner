using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plan_Generation
{
    internal class TasksClass
    {
        public string Name;
        public List<string> PrecedingTask;
        public List<string> SucceedingTask;
        public double TaskTime;
        public double FollowingTime;
        public double StartTime;
        public WorkersClass worker;
        public Boolean Assigned;

        public double EndTime
        {
            get
            {
                return StartTime + TaskTime;
            }
        }
    }
}
