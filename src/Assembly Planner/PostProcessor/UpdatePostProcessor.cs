using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;

namespace Assembly_Planner
{
    class UpdatePostProcessor
    {

        internal static void BuildSuccedingTaskDictionary(SubAssembly subAssembly, List<string> successors)
        {
            //if (subAssembly == null) return;
            if (subAssembly.PartNames.Count == 1) return;
            OptimalOrientation.SucTasks.Add(subAssembly.Name, successors);

            var subSubAssembly = subAssembly.Install.Moving;
            var subSuccessors = new List<string>(successors);
            subSuccessors.Add(subAssembly.Name);
            BuildSuccedingTaskDictionary(subSubAssembly as SubAssembly, subSuccessors);
            subSubAssembly = subAssembly.Install.Reference;
            subSuccessors = new List<string>(successors);
            subSuccessors.Add(subAssembly.Name);
            BuildSuccedingTaskDictionary(subSubAssembly as SubAssembly, subSuccessors);
        }

        internal static void BuildingInstallationTaskDictionary(SubAssembly subAssembly)
        {
            //if (subAssembly == null) return;
            if (subAssembly.PartNames.Count == 1)
                OptimalOrientation.SubAssemAndParts.Add(subAssembly.PartNames[0], subAssembly);
            else
                OptimalOrientation.SubAssemAndParts.Add(subAssembly.Name, subAssembly);
            //if (subAssembly.Install == null) return;
            if (subAssembly.PartNames.Count == 1) return;
            OptimalOrientation.InstTasks.Add(subAssembly.Name, subAssembly);

            var secureTime = 0.0;
            var movingTime = 0.0;
            var rotationTime = 0.0;
            if (subAssembly.Secure != null) secureTime = 0.2;
            if (subAssembly.Rotate != null) rotationTime = subAssembly.Rotate.Time;
            var taskTime = subAssembly.Install.Time + secureTime + movingTime + rotationTime;
            if (double.IsInfinity(taskTime))
            {
                if (double.IsInfinity(subAssembly.Install.Time))
                    subAssembly.Install.Time = 5.0;
                if (double.IsInfinity(secureTime))
                    subAssembly.Install.Time = 5.0;
                if (double.IsInfinity(movingTime))
                    subAssembly.Install.Time = 5.0;
                if (double.IsInfinity(rotationTime))
                    subAssembly.Install.Time = 5.0;
                taskTime = subAssembly.Install.Time + secureTime + movingTime + rotationTime;
            }
            OptimalOrientation.TaskTime.Add(subAssembly.Name, taskTime);
            BuildingInstallationTaskDictionary(subAssembly.Install.Moving as SubAssembly);
            BuildingInstallationTaskDictionary(subAssembly.Install.Reference as SubAssembly);
        }

        internal static void BuildingListOfReferencePreceedings(SubAssembly subAssembly)
        {
            //if (subAssembly.Install == null) return;
            if (subAssembly.PartNames.Count == 1) return;
            OptimalOrientation.RefPrec.Add(subAssembly);
            //if (subAssembly.Install.Moving.PartNames.Count > 1)
            var moving = subAssembly.Install.Moving as SubAssembly;
            OptimalOrientation.TempSucSubassem.Add(subAssembly.Install.Moving.PartNames.Count == 1
                ? subAssembly.Install.Moving.PartNames[0] : moving.Name);
            if (moving.Install != null)
                OptimalOrientation.Movings.Add(subAssembly.Install.Moving as SubAssembly);
            BuildingListOfReferencePreceedings(subAssembly.Install.Reference as SubAssembly);
        }

    }
}
