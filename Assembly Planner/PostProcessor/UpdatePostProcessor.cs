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
            if (subAssembly == null) return;
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
            if (subAssembly == null) return;
            OptimalOrientation.InstTasks.Add(subAssembly.Name, subAssembly);
            BuildingInstallationTaskDictionary(subAssembly.Install.Moving as SubAssembly);
            BuildingInstallationTaskDictionary(subAssembly.Install.Reference as SubAssembly);
        }

        internal static void BuildingListOfReferencePreceedings(SubAssembly subAssembly)
        {
            if (subAssembly == null) return;
            OptimalOrientation.RefPrec.Add(subAssembly);
            if (subAssembly.Install.Moving.PartNodes.Count > 1)
                OptimalOrientation.Movings.Add(subAssembly.Install.Moving as SubAssembly);
            BuildingListOfReferencePreceedings(subAssembly.Install.Reference as SubAssembly);
        }

    }
}
