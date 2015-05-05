using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using StarMathLib;

namespace Assembly_Planner
{
    internal class OptimalOrientation
    {
        public static Dictionary<string, List<string>> SucTasks;
        public static Dictionary<string, SubAssembly> InstTasks;
        public static List<SubAssembly> RefPrec;
        public static List<SubAssembly> Movings;

        public class PreAndCost
        {
            public SubAssembly SubAssembly;
            public FootprintFace Face;
            public double MinCost;
            public FootprintFace FromFace;
            public SubAssembly FromSubAssembly;
        }

        internal static void Run(List<AssemblyCandidate> solutions)
        {
            foreach (var c in solutions.Where(c => c != null))
                Dijkstra(c);
        }

        public static void Dijkstra(AssemblyCandidate candidate)
        {
            InstTasks = new Dictionary<string, SubAssembly>();
            UpdatePostProcessor.BuildingInstallationTaskDictionary(candidate.Sequence.Subassemblies[0]);

            SucTasks = new Dictionary<string, List<string>>();
            UpdatePostProcessor.BuildSuccedingTaskDictionary(candidate.Sequence.Subassemblies[0], new List<string>());

            var lastTask = InstTasks[SucTasks.Keys.Where(sucT => SucTasks[sucT].Count == 0).ToList()[0]];

            var loopMakingSubAsse = new List<SubAssembly>();
            loopMakingSubAsse.Add(lastTask);

            for (var h = 0; h < loopMakingSubAsse.Count; h++)
            {
                var lastSubAssEachMoving = loopMakingSubAsse[h];
                RefPrec = new List<SubAssembly>();
                Movings = new List<SubAssembly>();
                UpdatePostProcessor.BuildingListOfReferencePreceedings(loopMakingSubAsse[h]);

                var ftask = RefPrec[RefPrec.Count - 1];

                var initialFaces = ftask.Install.Reference.CVXHull.Faces.ToList();
                var fromFaces = AssemblyEvaluator.MergingFaces(initialFaces);

                Console.WriteLine("Which of the following faces is your current footprint face in the subassembly    " + ftask.Name + "   ?");
                Console.WriteLine("Enter the corresponding number to the console:");
                foreach (var f in fromFaces)
                {
                    var index = fromFaces.IndexOf(f);
                    Console.WriteLine(index + ":" + "   " + f.Name);
                }

                var read = Convert.ToInt32(Console.ReadLine());
                var startingFace = fromFaces[read];
                var notAffected = AssemblyEvaluator.UnaffectedRefFacesDuringInstallation(ftask);
                var toFaces = AssemblyEvaluator.MergingFaces(notAffected);

                var precAndMinC = new List<PreAndCost>();

                foreach (var tFace in toFaces)
                {
                    var ini = new PreAndCost();
                    precAndMinC.Add(ini);
                    var last = precAndMinC.Count - 1;

                    precAndMinC[last].SubAssembly = ftask;
                    precAndMinC[last].Face = tFace;
                    precAndMinC[last].MinCost = Double.PositiveInfinity;

                    double stabilityAccessCost = StabilityAndAcccessabilityCostCalcultor(ftask, tFace);

                    /*foreach (var fFace in fromFaces)
                    {
                        double totalCost = RiLiCostCalculator(ftask, fFace, tFace) + stabilityAccessCost;
                        if (!(totalCost < precAndMinC[last].MinCost)) continue;
                        precAndMinC[last].MinCost = totalCost;
                        precAndMinC[last].FromFace = fFace;
                    }*/

                    //For choosen starting face:
                    precAndMinC[last].MinCost = RiLiCostCalculator(ftask, startingFace, tFace) + stabilityAccessCost;
                    precAndMinC[last].FromFace = startingFace;
                }

                if (RefPrec.Count > 1)
                {
                    for (var i = RefPrec.Count - 2; i >= 0; i--)
                    //foreach (var t in RefPrec.Where(t => t.Install.Moving.PartNodes.Count == 1 && t.Install.Reference.PartNodes.Count == 1)) // the last added one or the one with one part in ref and one part in moving
                    {
                        var curSubAsse = RefPrec[i];
                        var preSubAsse = RefPrec[i + 1];
                        AssemblyEvaluator.MergingFaces(initialFaces);
                        fromFaces = toFaces;

                        notAffected = AssemblyEvaluator.UnaffectedRefFacesDuringInstallation(curSubAsse);
                        toFaces = new List<FootprintFace>(AssemblyEvaluator.MergingFaces(notAffected));

                        foreach (var tFace in toFaces)
                        {
                            var ini = new PreAndCost();
                            precAndMinC.Add(ini);
                            var last = precAndMinC.Count - 1;

                            precAndMinC[last].SubAssembly = curSubAsse;
                            precAndMinC[last].FromSubAssembly = preSubAsse;
                            precAndMinC[last].Face = tFace;
                            precAndMinC[last].MinCost = Double.PositiveInfinity;

                            double stabilityAccessCost = StabilityAndAcccessabilityCostCalcultor(curSubAsse, tFace);

                            foreach (var fFace in fromFaces)
                            {
                                var m = precAndMinC.Where(a => a.Face == fFace && a.SubAssembly == preSubAsse).ToList();
                                double totalCost = m[0].MinCost + RiLiCostCalculator(curSubAsse, fFace, tFace) + stabilityAccessCost;
                                if (!(totalCost < precAndMinC[last].MinCost)) continue;
                                precAndMinC[last].MinCost = totalCost;
                                precAndMinC[last].FromFace = fFace;
                            }
                        }
                    }
                }
                Commander(RefPrec, precAndMinC, lastSubAssEachMoving);
                loopMakingSubAsse.Remove(lastSubAssEachMoving);
                h--;
                loopMakingSubAsse.AddRange(Movings);
            }
            Console.ReadLine();
        }



        private static void Commander(List<SubAssembly> RefPrec, List<PreAndCost> precAndMinC, SubAssembly lastSubAssEachMoving)
        {
            var commands = new List<string>();
            foreach (var v in RefPrec)
                commands.Add(null);

            PreAndCost minCostFace = null;
            var min = Double.PositiveInfinity;
            foreach (var v in precAndMinC.Where(a => a.SubAssembly == lastSubAssEachMoving).Where(v => v.MinCost < min))
            {
                minCostFace = v;
                min = v.MinCost;
            }

            commands[commands.Count - 1] = "In Subassembly:  " + minCostFace.SubAssembly.Name + ", face:  " + minCostFace.Face.Name;
            var e = 1;
            var stay = true;
            do
            {
                if (minCostFace.FromSubAssembly == null)
                {

                    commands[0] = "For the first step change your footprint face from:" + minCostFace.FromFace + "  to:  " + minCostFace.Face.Name;
                    stay = false;
                }
                else
                {
                    e++;
                    foreach (var v in precAndMinC.Where(v =>
                                    v.Face == minCostFace.FromFace &&
                                    v.SubAssembly == minCostFace.FromSubAssembly))
                    {
                        minCostFace = v;
                        commands[commands.Count - e] = "In Subassembly:  " + minCostFace.SubAssembly.Name + ", face:  " + minCostFace.Face.Name;
                        break;
                    }
                }
            } while (stay);
            foreach (var c in commands)
                Console.WriteLine(c);
        }



        private static double StabilityAndAcccessabilityCostCalcultor(SubAssembly task, FootprintFace tFace)
        {
            var SI = AssemblyEvaluator.CheckStabilityForReference(task, tFace);
            var AI = AssemblyEvaluator.CheckAccessability(task.Install.InstallDirection, tFace);
            return AI + SI;
        }


        private static double RiLiCostCalculator(SubAssembly task, FootprintFace fFace, FootprintFace tFace)
        {
            // the width of the part is still unknown
            // if the face is adjacent, there is no need to lift it.
            // Maximum Distance is the longest diagonal in the Ref CVH 
            var maxDist = 0.0;
            foreach (var p1 in task.Install.Reference.CVXHull.Points)
            {
                foreach (var p2 in task.Install.Reference.CVXHull.Points.Where(a => a != p1))
                {
                    var xDif = p1.Position[0] - p2.Position[0];
                    var yDif = p1.Position[1] - p2.Position[1];
                    var zDif = p1.Position[2] - p2.Position[2];
                    if (Math.Sqrt((xDif * xDif) + (yDif * yDif) + (zDif * zDif)) > maxDist)
                        maxDist = (Math.Sqrt((xDif * xDif) + (yDif * yDif) + (zDif * zDif)))/10; // since it is in mm
                }
            }
            // if the candidate face is adjacent, do s.th else (do what????), othrwise calculate LI and RI
            var confidenceInterval = 0.1 * maxDist;
            var vertDist = maxDist / 2 + confidenceInterval;

            const double partWidth = 10.0; // this is only an assumption
            var liftingIndices = new PostProcessingConstnts.LiftingIndices
            {
                VM = 1 - (0.0075 * Math.Abs(vertDist - 30)),
                DM = 0.82 + (1.8 / (vertDist)),
                AM = 1,
                FM = 1,
                CM = 1
            };
            var rotatingIndex = new PostProcessingConstnts.RotatingIndices();
            //angle between candidate face and current face
            var angleInRad = Math.Acos(fFace.Normal.normalize().dotProduct(tFace.Normal.normalize()));
            var angleBetweenCurrentAndCandidate = angleInRad * (180 / Math.PI);

            if (fFace.Adjacents.Where(f => f.Name == tFace.Name).ToList().Count>0)
            {
                // It's adjacent, then do s.th ????????????
                // Giving RI and LI some values?
                liftingIndices.LI = 0; // There is no lifting cost
                rotatingIndex.RI = 0; // There is no rotating cost
                //Should I add another cost like pushing or pulling cost?
            }
            else
            {
                rotatingIndex.VM = 1 - (0.0075 * Math.Abs(vertDist - 30));
                // 1-(0.0075*|V-30|) in inch
                double horizontalDistance;
                if (vertDist < 10)
                {
                    // we can assume the width of the object = the smallest side
                    horizontalDistance = 10 + partWidth / 2;
                }
                else
                {
                    horizontalDistance = 8 + partWidth / 2;
                }
                rotatingIndex.HM = 10 / horizontalDistance;
                rotatingIndex.CM = 1;
                liftingIndices.HM = 10 / horizontalDistance;
                // after calculating the angle between current face and candidate face
                // which is s.th between 0 to 180.
                rotatingIndex.RAM = 1 - (0.0044 * angleBetweenCurrentAndCandidate);
                rotatingIndex.RWL = rotatingIndex.LC * rotatingIndex.HM *
                                    rotatingIndex.VM * rotatingIndex.RAM *
                                    rotatingIndex.CM;

                rotatingIndex.RI = task.Mass / rotatingIndex.RWL;
            }


            liftingIndices.RWL = liftingIndices.LC *
                                 liftingIndices.HM *
                                 liftingIndices.VM *
                                 liftingIndices.DM *
                                 liftingIndices.FM *
                                 liftingIndices.AM *
                                 liftingIndices.CM;
            liftingIndices.LI = task.Mass / liftingIndices.RWL;

            return liftingIndices.LI + rotatingIndex.RI;
        }
    }
}
