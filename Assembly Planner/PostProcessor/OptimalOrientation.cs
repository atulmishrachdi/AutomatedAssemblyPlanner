using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using StarMathLib;
using TVGL;

namespace Assembly_Planner
{
    public class OptimalOrientation
    {
        public static Dictionary<string, List<string>> SucTasks;
        public static Dictionary<string, SubAssembly> InstTasks;
        public static List<Part> RefWithOneNode;
        public static List<SubAssembly> RefPrec;
        public static List<SubAssembly> Movings;
        public static Dictionary<string, double[,]> TranslateToMagicBoxDic;
        //public static readonly double[] GravityVector = {0.0, -1.0, 0.0};
        public static readonly double[] GravityVector = { 0.0, 0.0, -1.0 };
        public static readonly double[] Ground = {0, 0, -0.25533295 };//{0, 0, 0};
        private const int GravityAxis = 3; // 1:x, 2:y, 3:z
        private static List<double[]> VertsOnCircle;

        public class PreAndCost
        {
            public SubAssembly SubAssembly;
            public FootprintFace Face;
            public double MinCost;
            public FootprintFace FromFace;
            public SubAssembly FromSubAssembly;
        }

        internal static void Run(AssemblySequence solution)//, ReportStatus statusReporter)
        {
            TranslateToMagicBoxDic = new Dictionary<string, double[,]>();
            TranslateToMagicBox();
            Dijkstra2(solution);
        }

        public static void Dijkstra2(AssemblySequence candidate)
        {
            //Bridge.StatusReporter.ReportStatusMessage("Generating the Assembly Plan - Optimal orientation search ...", 1);
            //Bridge.StatusReporter.ReportProgress(0);
            var taskCommands = new Dictionary<string, double>();

            InstTasks = new Dictionary<string, SubAssembly>();
            UpdatePostProcessor.BuildingInstallationTaskDictionary(candidate.Subassemblies[0]);

            SucTasks = new Dictionary<string, List<string>>();
            UpdatePostProcessor.BuildSuccedingTaskDictionary(candidate.Subassemblies[0], new List<string>());

            // Here I want to position the starting ref parts in a right initial position
            RefWithOneNode = new List<Part>();
            var initialTasks =
                InstTasks.Values.Where(
                    t => t.Install.Reference.PartNames.Count == 1 && t.Install.Moving.PartNames.Count == 1).ToList();
            foreach (var task in InstTasks.Where(task => task.Value.Install.Reference.PartNames.Count == 1))
                RefWithOneNode.Add(task.Value.Install.Reference);
            //var refrencesToStartWith =
            //    RefWithOneNode.Where(r => initialTasks.Any(t => t.Install.Reference == r)).ToList();
            // Now I need to position RefWithOneNode
            // first define a direction for each RefWithOneNode (instead, I define points on a circle to position them)
            //     the y value of these directions is 0 because we want to put them on the ground
            // A point at angle theta on the circle whose centre is (x0,y0) and whose radius is r is (x0 + r cos theta, y0 + r sin theta).
            // The center of the circle can be anything. I will take the projected center of the whole AABB
            VertsOnCircle = new List<double[]>();
            var allVertcs = Program.Solids.SelectMany(s => s.Value.SelectMany(g => g.Vertices)).ToList();
            var x = new[] {allVertcs.Min(v => v.X), allVertcs.Max(v => v.X)};
            var y = new[] {allVertcs.Min(v => v.Y), allVertcs.Max(v => v.Y)};
            var z = new[] {allVertcs.Min(v => v.Z), allVertcs.Max(v => v.Z)};
            var xEl = (x[0] + x[1])/2.0;
            var yEl = (y[0] + y[1])/2.0;
            var zEl = (z[0] + z[1])/2.0;
            double[] circleCntr;
            if (GravityAxis == 1) circleCntr = new[] { 0.0, yEl, zEl};
            if (GravityAxis == 2) circleCntr = new[] { xEl, 0.0, zEl };
            if (GravityAxis == 3) circleCntr = new[] { xEl, yEl, 0.0 };

            var radius = Math.Max(Math.Max(x[1] - x[0], y[1] - y[0]), z[1] - z[0]);
            var angle = (2*Math.PI)/(double) RefWithOneNode.Count; // in radian
            for (var i = 0; i < RefWithOneNode.Count; i++)
            {
                var theta = angle*i;
                if (GravityAxis == 1)
                    VertsOnCircle.Add(new[]
                    {0.0, circleCntr[1] + radius*Math.Cos(theta), circleCntr[2] + radius*Math.Sin(theta)});
                if (GravityAxis == 2)
                    VertsOnCircle.Add(new[]
                    {circleCntr[0] + radius*Math.Cos(theta), 0.0, circleCntr[2] + radius*Math.Sin(theta)});
                if (GravityAxis == 3)
                    VertsOnCircle.Add(new[]
                    {circleCntr[0] + radius*Math.Cos(theta), circleCntr[1] + radius*Math.Sin(theta),  0.0});
            }

            var lastTask = InstTasks[SucTasks.Keys.Where(sucT => SucTasks[sucT].Count == 0).ToList()[0]];
            var loopMakingSubAsse = new List<SubAssembly> {lastTask};
            var counter = 0;

            for (var h = 0; h < loopMakingSubAsse.Count; h++)
            {
                var lastSubAssEachMoving = loopMakingSubAsse[h];
                RefPrec = new List<SubAssembly>();
                Movings = new List<SubAssembly>();
                UpdatePostProcessor.BuildingListOfReferencePreceedings(loopMakingSubAsse[h]);

                var ftask = RefPrec[RefPrec.Count - 1];

                var initialFaces = ftask.Install.Reference.CVXHull.Faces.ToList();
                var fromFaces = AssemblyEvaluator.MergingFaces(initialFaces);

                //Console.WriteLine("Which of the following faces is your current footprint face in the subassembly    " + ftask.Name + "   ?");
                //foreach (var f in fromFaces)
                //{
                //    var index = fromFaces.IndexOf(f);
                //    Console.WriteLine(index + ":" + "   " + f.Name);
                //}
                // I dont need to ask because I know the gravity (therefore I know the normal of the ground)
                //    Using this gravity, I will take the footprintface that has the closest normal to 
                //    the gravity
                //var read = Convert.ToInt32(Console.ReadLine());
                var startingFace = new FootprintFace(GravityVector);
                var notAffected = AssemblyEvaluator.UnaffectedRefFacesDuringInstallation(ftask);
                var toFaces = AssemblyEvaluator.MergingFaces(notAffected);
                 //toFaces.Add(startingFace);
                var precAndMinC = new List<PreAndCost>();
                foreach (var tFace in toFaces)
                {
                    var stabilityAccessCost = StabilityAndAcccessabilityCostCalcultor(ftask, tFace);
                    var rICost = RiLiCostCalculator(ftask, startingFace, tFace);
                    var preCost = new PreAndCost
                    {
                        SubAssembly = ftask,
                        Face = tFace,
                        MinCost = rICost + stabilityAccessCost,
                        FromFace = startingFace
                    };
                    precAndMinC.Add(preCost);
                }
                counter++;
                //Bridge.StatusReporter.ReportProgress(counter/(float) (InstTasks.Count + 1));

                if (RefPrec.Count > 1)
                {
                    for (var i = RefPrec.Count - 2; i >= 0; i--)
                    {
                        var samefromandtoface = false;
                        var totalCost = double.PositiveInfinity;
                        var curSubAsse = RefPrec[i];
                        var preSubAsse = RefPrec[i + 1];
                        var refvertex = new List<Vertex>();
                        var refsolids = new List<TessellatedSolid>();
                     
                   
                        foreach (var v in curSubAsse.Install.Reference.CVXHull.Vertices)
                        {
                            refvertex.Add(v);
                        }

                       var refOBB = OBB.BuildUsingPoints(refvertex);
                        AssemblyEvaluator.MergingFaces(initialFaces);
                        fromFaces = toFaces;
                        notAffected = AssemblyEvaluator.UnaffectedRefFacesDuringInstallation(curSubAsse);
                        toFaces = new List<FootprintFace>(AssemblyEvaluator.MergingFaces(notAffected));
                        var notAffectedfromface = Getnotaffectedfromface(fromFaces, notAffected);
                        foreach (var f in notAffectedfromface)
                        {
                            toFaces.Add(f);
                        }
                        foreach (var tFace in toFaces)
                        {
                            var preCost = new PreAndCost
                            {
                                SubAssembly = curSubAsse,
                                FromSubAssembly = preSubAsse,
                                Face = tFace,
                                MinCost = double.PositiveInfinity
                            };
                            var stabilityAccessCost = StabilityAndAcccessabilityCostCalcultor(curSubAsse, tFace);

                            foreach (var fFace in fromFaces)
                            {
                                samefromandtoface = false;
                                if (Math.Abs(1 - fFace.Normal.dotProduct(tFace.Normal)) < 0.005)
                                    samefromandtoface = true;

                                var m = precAndMinC.Where(a => a.Face == fFace && a.SubAssembly == preSubAsse).ToList();

                                if (samefromandtoface == false)
                                {
                                    var lengths = new double[]
                                    {
                                        refOBB.CornerVertices[0].Position.subtract(refOBB.CornerVertices[1].Position).norm1(),
                                        refOBB.CornerVertices[2].Position.subtract(refOBB.CornerVertices[1].Position).norm1(),
                                        refOBB.CornerVertices[0].Position.subtract(refOBB.CornerVertices[4].Position).norm1()
                                    };
                                    var lmax = lengths.Max();
                                    var lmin = lengths.Min();
                                    double lmid = lengths.Average()*3 - lmax - lmin;
                                    var input = new double[5]
                                    {
                                        Math.Log10(curSubAsse.Install.Reference.Mass/1000),
                                        Math.Log10(refOBB.Volume/1000),
                                        Math.Log10(lmax*lmid/1000),
                                        Math.Log10(lmin*lmid/1000),
                                        fFace.Normal.dotProduct(tFace.Normal)
                                    };
                                    double rotatetime, rotateSD;
                                    CalculateAssemblyTimeAndSD.GetTimeAndSD(input, "rotate",
                                        out rotatetime, out rotateSD);
                                    //var c1 = m[0].MinCost; //transition cost
                                    //var c2 = RiLiCostCalculator(curSubAsse, fFace, tFace); //0-2
                                    //var c3 = stabilityAccessCost //0-2
                                    //         + rotatetime;
                                    totalCost = m[0].MinCost + RiLiCostCalculator(curSubAsse, fFace, tFace) +
                                                stabilityAccessCost + rotatetime;
                                }
                                else
                                {
                                    //totalCost = m[0].MinCost +
                                    //            stabilityAccessCost;
                                    totalCost = 0;
                                }
                                if (totalCost > preCost.MinCost) continue;
                                preCost.MinCost = totalCost;
                                preCost.FromFace = fFace;
                            }
                            if (double.IsPositiveInfinity(preCost.MinCost)) continue;
                            precAndMinC.Add(preCost);
                        }
                        counter++;
                        //Bridge.StatusReporter.ReportProgress(counter/(float) (InstTasks.Count + 1));
                    }
                }
                Commander(RefPrec, precAndMinC, lastSubAssEachMoving, taskCommands);
                loopMakingSubAsse.Remove(lastSubAssEachMoving);
                h--;
                loopMakingSubAsse.AddRange(Movings);
            }
            //Bridge.StatusReporter.ReportProgress(1);
            //Bridge.StatusReporter.PrintMessage("AN ASSEMBLY PLAN IS SUCCESSFULLY GENERATED", 1);
            //Bridge.StatusReporter.PrintMessage(
                //"   - NUMBER OF REQUIRED INSTALL ACTIONS:                             " + InstTasks.Count, 0.7F);
            //return taskCommands;
        }

        private static List<FootprintFace> Getnotaffectedfromface(List<FootprintFace> fromFaces,
            List<PolygonalFace> notAffected)
        {
            var newfooptrintface = new List<FootprintFace>();
            foreach (var ff in fromFaces)
            {
                foreach (var naf in notAffected)
                {
                    if (Math.Abs(naf.Normal.dotProduct(ff.Normal) - 1) < 0.001)
                    {
                        newfooptrintface.Add(ff);
                        break;
                    }
                }
            }
            return newfooptrintface;
        }

        private static void Commander(List<SubAssembly> RefPrec, List<PreAndCost> precAndMinC,
            SubAssembly lastSubAssEachMoving, Dictionary<string, double> taskCommands)
        {
            // take the faces of the final assembly. calculate the cost of 
            if (lastSubAssEachMoving.PartNames.Count == Program.SolidsNoFastener.Count)
            {
                // This is where we want our final assembly to be in a specific orientation
                // for example we dont want our car to be flipped! :|
                // Therefore, we need to add another cost. The cost of final rotation from
                // found footprint faces to the final position. 
                // First: normal of the final footprint face in the current orientation
                //        How? It is always the gravity vector

                var toFace = new FootprintFace(GravityVector);
                foreach (var v in precAndMinC.Where(a => a.SubAssembly == lastSubAssEachMoving))
                {
                    var rICost = RiLiCostCalculator(lastSubAssEachMoving, v.Face, toFace);
                    v.MinCost += rICost;
                }
            }
            //var commands = new string[RefPrec.Count];
            var subAssems = new PreAndCost[RefPrec.Count];
            PreAndCost minCostFace = null;
            var min = double.PositiveInfinity;
            foreach (var v in precAndMinC.Where(a => a.SubAssembly == lastSubAssEachMoving).Where(v => v.MinCost < min))
            {
                minCostFace = v;
                min = v.MinCost;
            }
            subAssems[subAssems.Length - 1] = minCostFace;
            taskCommands.Add(minCostFace.SubAssembly.Name, minCostFace.Face.Name);
            var e = 1;
            var stay = true;
            do
            {
                if (minCostFace.FromSubAssembly == null)
                    stay = false;
                else
                {
                    e++;
                    foreach (var v in precAndMinC.Where(v =>
                        v.Face.Name == minCostFace.FromFace.Name &&
                        v.SubAssembly.Name == minCostFace.FromSubAssembly.Name))
                    {
                        minCostFace = v;
                        subAssems[subAssems.Length - e] = minCostFace;
                        break;
                    }
                }
            } while (stay);

            // now find the transformation matrices:
            //var uniquePosition = new[] { VertsOnCircle[0][0], VertsOnCircle[0][1], VertsOnCircle[0][2] };
            //Vertex initialCOM = null;
            for (var i = 0; i < subAssems.Length; i++)
            {
                double[,] tM;
                if (RefWithOneNode.Contains(subAssems[i].SubAssembly.Install.Reference))
                {
                    tM = TransformationMatrixForStartingReferenceNewApproach(subAssems[i].FromFace, subAssems[i].Face,
                        subAssems[i].SubAssembly.Install.Reference.CenterOfMass);
                    subAssems[i].SubAssembly.Rotate.TransformationMatrix = tM;
                    //initialCOM = subAssems[i].SubAssembly.CenterOfMass;
                }
                else
                {
                    var rotatedCOM = MatrixTimesMatrix(subAssems[i].FromSubAssembly.Rotate.TransformationMatrix,
                        new[]
                        {
                            subAssems[i].SubAssembly.Install.Reference.CenterOfMass.Position[0],
                            subAssems[i].SubAssembly.Install.Reference.CenterOfMass.Position[1],
                            subAssems[i].SubAssembly.Install.Reference.CenterOfMass.Position[2], 1
                        });
                    tM = TransformationMatrixNewApproach(subAssems[i].FromFace, subAssems[i].Face,
                        subAssems[i].SubAssembly.Install.Reference.CenterOfMass);  //new Vertex(rotatedCOM)
                }
                if (i == 0)
                    subAssems[i].SubAssembly.Rotate.TransformationMatrix = tM;
                else
                    subAssems[i].SubAssembly.Rotate.TransformationMatrix =
                        MatrixTimesMatrix(subAssems[i].FromSubAssembly.Rotate.TransformationMatrix,tM );
                var transMatr = subAssems[i].SubAssembly.Rotate.TransformationMatrix;
                var toGroundTranslation = TranslationToGroundFinder(subAssems[i]);
                subAssems[i].SubAssembly.Rotate.TransformationMatrix = MatrixTimesMatrix(toGroundTranslation,
                    subAssems[i].SubAssembly.Rotate.TransformationMatrix);
                var rotationMatrix = new[,]
                {
                    {transMatr[0, 0], transMatr[0, 1], transMatr[0, 2], 0.0},
                    {transMatr[1, 0], transMatr[1, 1], transMatr[1, 2], 0.0},
                    {transMatr[2, 0], transMatr[2, 1], transMatr[2, 2], 0.0},
                    {0.0, 0.0, 0.0, 1.0}
                };
                subAssems[i].SubAssembly.Install.InstallDirectionRotated =
                    RotateInstallDirection(subAssems[i].SubAssembly.Install.InstallDirection,
                        rotationMatrix);

                // now if there is any fastener in this assembly operation, create their install direction
                if (!subAssems[i].SubAssembly.Secure.Fasteners.Any()) continue;
                foreach (var fastener in subAssems[i].SubAssembly.Secure.Fasteners)
                    CreateFastenerInstallDirectionAndDistance(subAssems[i].SubAssembly, fastener, transMatr,
                        rotationMatrix);
            }
            //foreach (var c in commands)
            //    Console.WriteLine(c);
        }


        private static void Commander2(List<SubAssembly> RefPrec, List<PreAndCost> precAndMinC,
            SubAssembly lastSubAssEachMoving, Dictionary<string, double> taskCommands)
        {
            // take the faces of the final assembly. calculate the cost of 
            if (lastSubAssEachMoving.PartNames.Count == Program.SolidsNoFastener.Count)
            {
                // This is where we want our final assembly to be in a specific orientation
                // for example we dont want our car to be flipped! :|
                // Therefore, we need to add another cost. The cost of final rotation from
                // found footprint faces to the final position. 
                // First: normal of the final footprint face in the current orientation
                //        How? It is always the gravity vector

                var toFace = new FootprintFace(GravityVector);
                foreach (var v in precAndMinC.Where(a => a.SubAssembly == lastSubAssEachMoving))
                {
                    var rICost = RiLiCostCalculator(lastSubAssEachMoving, v.Face, toFace);
                    v.MinCost += rICost;
                }
            }
            //var commands = new string[RefPrec.Count];
            var subAssems = new SubAssembly[RefPrec.Count];
            PreAndCost minCostFace = null;
            var min = double.PositiveInfinity;
            foreach (var v in precAndMinC.Where(a => a.SubAssembly == lastSubAssEachMoving).Where(v => v.MinCost < min))
            {
                minCostFace = v;
                min = v.MinCost;
            }
            var tM = TransformationMatrix(minCostFace.FromFace, minCostFace.Face, minCostFace.SubAssembly.CenterOfMass);
            minCostFace.SubAssembly.Rotate.TransformationMatrix = tM;
            subAssems[subAssems.Length - 1] = minCostFace.SubAssembly;
            //commands[commands.Length - 1] = "In Subassembly:  " + minCostFace.SubAssembly.Name + ", face:  " + minCostFace.Face.Name;
            taskCommands.Add(minCostFace.SubAssembly.Name, minCostFace.Face.Name);
            var e = 1;
            var stay = true;
            do
            {
                if (minCostFace.FromSubAssembly == null)
                    stay = false;
                else
                {
                    e++;
                    foreach (var v in precAndMinC.Where(v =>
                        v.Face.Name == minCostFace.FromFace.Name &&
                        v.SubAssembly.Name == minCostFace.FromSubAssembly.Name))
                    {
                        minCostFace = v;
                        var tMatrix = TransformationMatrix(minCostFace.FromFace, minCostFace.Face,
                            minCostFace.SubAssembly.CenterOfMass);
                        minCostFace.SubAssembly.Rotate.TransformationMatrix = tMatrix;
                        subAssems[subAssems.Length - e] = minCostFace.SubAssembly;
                        //commands[commands.Length - e] = "In Subassembly:  " + minCostFace.SubAssembly.Name + ", face:  " + minCostFace.Face.Name;
                        //taskCommands.Add(minCostFace.SubAssembly.Name, minCostFace.Face.Name);
                        break;
                    }
                }
            } while (stay);
            for (var i = 1; i < subAssems.Length; i++)
            {
                subAssems[i].Rotate.TransformationMatrix = MatrixTimesMatrix(subAssems[i].Rotate.TransformationMatrix,
                    subAssems[i - 1].Rotate.TransformationMatrix);
            }
            foreach (var subAssem in subAssems)
            {
                var transMatr = subAssem.Rotate.TransformationMatrix;
                var rotationMatrix = new[,]
                {
                    {transMatr[0, 0], transMatr[0, 1], transMatr[0, 2], 0.0},
                    {transMatr[1, 0], transMatr[1, 1], transMatr[1, 2], 0.0},
                    {transMatr[2, 0], transMatr[2, 1], transMatr[2, 2], 0.0},
                    {0.0, 0.0, 0.0, 1.0}
                };
                subAssem.Install.InstallDirectionRotated =
                    RotateInstallDirection(subAssem.Install.InstallDirection,
                        rotationMatrix);
            }
            //foreach (var c in commands)
            //    Console.WriteLine(c);
        }

        private static void CreateFastenerInstallDirectionAndDistance(SubAssembly subAssembly, Fastener fastener,
            double[,] transMatr, double[,] rotationMatrix)
        {
            fastener.InstallDirection = DisassemblyDirections.Directions[fastener.RemovalDirection].multiply(-1.0);
            fastener.InstallDirectionRotated = RotateInstallDirection(fastener.InstallDirection, rotationMatrix);
            fastener.InstallDistance =
                DetermineDistanceToSeparateHull(subAssembly.CVXHull, fastener.Solid.ConvexHull,
                    fastener.InstallDirection)/Program.MeshMagnifier;
            if (fastener.Nuts == null) fastener.Nuts = new List<Nut>();
            if (fastener.Washer == null) fastener.Washer = new List<Washer>();
            foreach (var nut in fastener.Nuts)
            {
                nut.InstallDirection = DisassemblyDirections.Directions[nut.RemovalDirection].multiply(-1.0);
                nut.InstallDirectionRotated = RotateInstallDirection(nut.InstallDirection, rotationMatrix);
                fastener.InstallDistance =
                    DetermineDistanceToSeparateHull(subAssembly.CVXHull, nut.Solid.ConvexHull, nut.InstallDirection)/
                    Program.MeshMagnifier;
            }
            foreach (var washer in fastener.Washer)
            {
                washer.InstallDirection = DisassemblyDirections.Directions[washer.RemovalDirection].multiply(-1.0);
                washer.InstallDirectionRotated = RotateInstallDirection(washer.InstallDirection, rotationMatrix);
                fastener.InstallDistance =
                    DetermineDistanceToSeparateHull(subAssembly.CVXHull, washer.Solid.ConvexHull,
                        washer.InstallDirection)/ Program.MeshMagnifier;
            }
        }


        private static void TranslateToMagicBox()
        {
            foreach (var solid in Program.Solids)
            {
                var allVertcs = solid.Value.SelectMany(g => g.Vertices).ToList();
                var x = new[] {allVertcs.Min(v => v.X), allVertcs.Max(v => v.X)};
                var y = new[] {allVertcs.Min(v => v.Y), allVertcs.Max(v => v.Y)};
                var z = new[] {allVertcs.Min(v => v.Z), allVertcs.Max(v => v.Z)};
                var midBox = new[]
                {
                    (x[0] + x[1])/(2.0*Program.MeshMagnifier),
                    (y[0] + y[1])/(2.0*Program.MeshMagnifier),
                    (z[0] + z[1])/(2.0*Program.MeshMagnifier)
                };
                // now translate midBox to Bridge.PointInMagicBox
                var matrix = new[,]
                {
                    {1.0, 0, 0, Program.PointInMagicBox[0] - midBox[0]},
                    {0, 1.0, 0, Program.PointInMagicBox[1] - midBox[1]},
                    {0, 0, 1.0, Program.PointInMagicBox[2] - midBox[2]},
                    {0, 0, 0.0, 1.0}
                };
                TranslateToMagicBoxDic.Add(solid.Key, matrix);
            }
            // This is temporary
            /*foreach (var spring in SpringDetector.Springs)
            {
                var solid = spring.Solid;
                var allVertcs = solid.Vertices;
                var x = new[] { allVertcs.Min(v => v.X), allVertcs.Max(v => v.X) };
                var y = new[] { allVertcs.Min(v => v.Y), allVertcs.Max(v => v.Y) };
                var z = new[] { allVertcs.Min(v => v.Z), allVertcs.Max(v => v.Z) };
                var midBox = new[]
                {
                    (x[0] + x[1])/(2.0*Bridge.MeshMagnifier),
                    (y[0] + y[1])/(2.0*Bridge.MeshMagnifier),
                    (z[0] + z[1])/(2.0*Bridge.MeshMagnifier)
                };
                // now translate midBox to Bridge.PointInMagicBox
                var matrix = new[,]
                {
                    {1.0, 0, 0, Bridge.PointInMagicBox[0] - midBox[0]},
                    {0, 1.0, 0, Bridge.PointInMagicBox[1] - midBox[1]},
                    {0, 0, 1.0, Bridge.PointInMagicBox[2] - midBox[2]},
                    {0, 0, 0.0, 1.0}
                };
                TranslateToMagicBoxDic.Add(solid.Name, matrix);
            }*/
        }

        private static double StabilityAndAcccessabilityCostCalcultor(SubAssembly task, FootprintFace tFace)
        {
            var SI = AssemblyEvaluator.CheckStabilityForReference(task, tFace);
            var AI = AssemblyEvaluator.CheckAccessability(task.Install.InstallDirection, tFace);
            return AI + SI;
        }


        private static double RiLiCostCalculator(SubAssembly task, FootprintFace fFace, FootprintFace tFace)
        {
            // This is what I need to assume:
            //     The input units are m. I multiplied them by 1000, so they are now mm. 
            // For NIOSH, they are all cm.
            var toCentimeter = (1/ Program.MeshMagnifier)*100;
            var workbenchHeight = 88.9; // in cm
            var liftingIndices = new LiftingIndices();

            var coun = double.PositiveInfinity;
            Vertex verOnFromFace = null;
            Vertex verOnToFace = null;
            if (fFace.Faces == null)
            {
                foreach (var ver in task.Install.Reference.CVXHull.Vertices.Where(ver => ver.Position[GravityAxis-1] < coun))
                {
                    verOnFromFace = ver;
                    coun = ver.Position[GravityAxis - 1];
                }
            }
            else
                verOnFromFace = fFace.Faces[0].Vertices[0];

            coun = double.PositiveInfinity;
            if (tFace.Faces == null)
            {
                foreach (var ver in task.Install.Reference.CVXHull.Vertices.Where(ver => ver.Position[GravityAxis - 1] < coun))
                {
                    verOnToFace = ver;
                    coun = ver.Position[GravityAxis - 1];
                }
            }
            else
                verOnToFace = tFace.Faces[0].Vertices[0];
            // How much we need to lift our object?
            // The maximum distance between vertices to the tFace.
            var maxDistAfterRotation = double.NegativeInfinity;
            foreach (var ver in task.Install.Reference.CVXHull.Vertices)
            {
                var dis = Math.Abs(GeometryFunctions.DistanceBetweenVertexAndPlane(ver.Position, tFace.Normal,
                    verOnToFace.Position));
                if (dis > maxDistAfterRotation) maxDistAfterRotation = dis;
            }
            var maxDistBeforeRotation = double.NegativeInfinity;
            foreach (var ver in task.Install.Reference.CVXHull.Vertices)
            {
                var dis = Math.Abs(GeometryFunctions.DistanceBetweenVertexAndPlane(ver.Position, fFace.Normal,
                    verOnFromFace.Position));
                if (dis > maxDistBeforeRotation) maxDistBeforeRotation = dis;
            }

            var v = (maxDistBeforeRotation/2.0)*toCentimeter + workbenchHeight;

            var partWidth = maxDistAfterRotation*toCentimeter;

            double horizontalDistance;
            if (v < 25)
                // I will assume the width of the object = the smallest side
                horizontalDistance = 20 + partWidth/2;
            else
                horizontalDistance = 25 + partWidth/2;

            // for V which is the distance from were hands are to the floor, since we don't know where the ground is
            // we can assume:
            //           On most benches, the working surface is somewhere between 33" and 36" high.
            // I will take 35" or 88.9cm
            // but V itself is workbench height plus the half of the height of the subassembly before move

            var confidenceInterval = 0.1*maxDistAfterRotation;

            // if the face is adjacent, there is no need to lift it.
            if (fFace.Adjacents == null ||
                fFace.Adjacents.Where(f => f.Name == tFace.Name).ToList().Count > 0)
            {
                liftingIndices.LI = 0; // There is no lifting cost
            }
            else
            {
                var liftingAmount = ((maxDistAfterRotation/2.0) + confidenceInterval)*toCentimeter;
                liftingIndices = new LiftingIndices
                {
                    HM = 25/horizontalDistance,
                    VM = 1 - 0.003*Math.Abs(v - 75),
                    DM = 0.82 + (4.5/liftingAmount),
                    AM = 1,
                    FM = 1,
                    CM = 1
                };
                liftingIndices.RWL = liftingIndices.LC*
                                     liftingIndices.HM*
                                     liftingIndices.VM*
                                     liftingIndices.DM*
                                     liftingIndices.FM*
                                     liftingIndices.AM*
                                     liftingIndices.CM;
                liftingIndices.LI = task.Mass/liftingIndices.RWL;
            }


            var rotatingIndex = new RotatingIndices();
            //angle between candidate face and current face
            var dot = fFace.Normal.dotProduct(tFace.Normal);
            if (dot > 1) dot = 1;
            if (dot < -1) dot = -1;
            var angleInRad = Math.Acos(dot);
            var angleBetweenCurrentAndCandidate = angleInRad*(180/Math.PI);

            rotatingIndex.VM = 1 - 0.003*Math.Abs(v - 75);
            rotatingIndex.HM = 25/horizontalDistance;
            rotatingIndex.CM = 1;
            rotatingIndex.RAM = 1 - (0.0044*angleBetweenCurrentAndCandidate);
            rotatingIndex.RWL = rotatingIndex.LC*rotatingIndex.HM*
                                rotatingIndex.VM*rotatingIndex.RAM*
                                rotatingIndex.CM;

            rotatingIndex.RI = task.Mass/rotatingIndex.RWL;

            return liftingIndices.LI + rotatingIndex.RI;
        }

        internal static double[] TransformADirection(double[] dir, double[,] tM)
        {
            return new[]
            {
                tM[0, 0]*dir[0] + tM[0, 1]*dir[1] + tM[0, 2]*dir[2],
                tM[1, 0]*dir[0] + tM[1, 1]*dir[1] + tM[1, 2]*dir[2],
                tM[2, 0]*dir[0] + tM[2, 1]*dir[1] + tM[2, 2]*dir[2]
            };
        }

        internal static double[,] TransformationMatrix(FootprintFace from, FootprintFace to, Vertex COM)
        {
            // before this rotation, I need to first translate to the origin and rotate it
            // there, then trandslate it back. 
            if (Math.Abs(from.Normal.dotProduct(to.Normal) - 1) <= 0.2)
            {
                to.Normal = from.Normal;
                return new[,]
                {
                    {1, 0, 0, 0},
                    {0, 1, 0, 0},
                    {0, 0, 1, 0},
                    {0, 0, 0, 1.0}
                };
            }
            var I = new[,] { { 1.0, 0, 0 }, { 0, 1.0, 0 }, { 0, 0, 1.0 } };
            var cross = to.Normal.crossProduct(from.Normal);
            var vx = SkewSymmetricCrossProduct(cross[0], cross[1], cross[2]);
            var vx2 = SquareSkewSymmetricCrossProduct(cross[0], cross[1], cross[2]);
            var cosine = to.Normal.dotProduct(from.Normal);
            var sine = Math.Sqrt(cross[0]*cross[0] + cross[1]*cross[1] + cross[2]*cross[2]);
            if (Math.Abs(sine) < 0.01) // meaning that the angle is almost 0, and To and From faces are the same.
            {
                return new[,]
                {
                    {1, 0, 0, 0},
                    {0, 1, 0, 0},
                    {0, 0, 1, 0},
                    {0, 0, 0, 1.0}
                };
            }
            var c = (1 - cosine)/((double) Math.Pow(sine, 2));
            var vx2C = ConstantTimesMatrix(c, vx2);
            var rotMatr = AddMetrices(I, AddMetrices(vx, vx2C));
            var rotationMatrix = new[,]
            {
                {rotMatr[0, 0], rotMatr[0, 1], rotMatr[0, 2], 0.0},
                {rotMatr[1, 0], rotMatr[1, 1], rotMatr[1, 2], 0.0},
                {rotMatr[2, 0], rotMatr[2, 1], rotMatr[2, 2], 0.0},
                {0.0, 0.0, 0.0, 1.0}
            };
            var translateToOrigin = new[,]
            {
                {1, 0, 0, (-1)*COM.Position[0]/Program.MeshMagnifier},
                {0, 1, 0, (-1)*COM.Position[1]/Program.MeshMagnifier},
                {0, 0, 1, (-1)*COM.Position[2]/Program.MeshMagnifier},
                {0, 0, 0, 1.0}
            };
            var translateBackToOriginal = new[,]
            {
                {1, 0, 0, COM.Position[0]/Program.MeshMagnifier},
                {0, 1, 0, COM.Position[1]/Program.MeshMagnifier},
                {0, 0, 1, COM.Position[2]/Program.MeshMagnifier},
                {0, 0, 0, 1.0}
            };
            var first = MatrixTimesMatrix(translateBackToOriginal, rotationMatrix);
            var transformMatrix = MatrixTimesMatrix(first, translateToOrigin);
            return transformMatrix;
            // Now, the part will be translated to the ground.
            //var translationToGround = TranslationToGroundFinder(to, transformMatrix);
            //return MatrixTimesMatrix(translationToGround, transformMatrix);
        }

        internal static double[,] TransformationMatrixTwoStep(FootprintFace from, FootprintFace to, Vertex COM)
        {
            // before this rotation, I need to first translate to the origin and rotate it
            // there, then trandslate it back. 
            if (Math.Abs(from.Normal.dotProduct(to.Normal) - 1) <= 0.2)
            {
                to.Normal = from.Normal;
                return new[,]
                {
                    {1, 0, 0, 0},
                    {0, 1, 0, 0},
                    {0, 0, 1, 0},
                    {0, 0, 0, 1.0}
                };
            }
            var I = new[,] { { 1.0, 0, 0 }, { 0, 1.0, 0 }, { 0, 0, 1.0 } };
            var cross = to.Normal.crossProduct(from.Normal);
            var vx = SkewSymmetricCrossProduct(cross[0], cross[1], cross[2]);
            var vx2 = SquareSkewSymmetricCrossProduct(cross[0], cross[1], cross[2]);
            var cosine = to.Normal.dotProduct(from.Normal);
            var sine = Math.Sqrt(cross[0] * cross[0] + cross[1] * cross[1] + cross[2] * cross[2]);
            if (Math.Abs(sine) < 0.01) // meaning that the angle is almost 0, and To and From faces are the same.
            {
                return new[,]
                {
                    {1, 0, 0, 0},
                    {0, 1, 0, 0},
                    {0, 0, 1, 0},
                    {0, 0, 0, 1.0}
                };
            }
            var c = (1 - cosine) / ((double)Math.Pow(sine, 2));
            var vx2C = ConstantTimesMatrix(c, vx2);
            var rotMatr = AddMetrices(I, AddMetrices(vx, vx2C));
            var rotationMatrix = new[,]
            {
                {rotMatr[0, 0], rotMatr[0, 1], rotMatr[0, 2], 0.0},
                {rotMatr[1, 0], rotMatr[1, 1], rotMatr[1, 2], 0.0},
                {rotMatr[2, 0], rotMatr[2, 1], rotMatr[2, 2], 0.0},
                {0.0, 0.0, 0.0, 1.0}
            };
            var translateToOrigin = new[,]
            {
                {1, 0, 0, (-1)*COM.Position[0]/Program.MeshMagnifier},
                {0, 1, 0, (-1)*COM.Position[1]/Program.MeshMagnifier},
                {0, 0, 1, (-1)*COM.Position[2]/Program.MeshMagnifier},
                {0, 0, 0, 1.0}
            };
            var translateBackToOriginal = new[,]
            {
                {1, 0, 0, COM.Position[0]/Program.MeshMagnifier},
                {0, 1, 0, COM.Position[1]/Program.MeshMagnifier},
                {0, 0, 1, COM.Position[2]/Program.MeshMagnifier},
                {0, 0, 0, 1.0}
            };
            var transformMatrix = MatrixTimesMatrix(rotationMatrix, translateToOrigin);
            return transformMatrix;
            // Now, the part will be translated to the ground.
            //var translationToGround = TranslationToGroundFinder(to, transformMatrix);
            //return MatrixTimesMatrix(translationToGround, transformMatrix);
        }

        private static double[,] TransformationMatrixForStartingReference(FootprintFace from, FootprintFace to,
            Vertex COM)
        {
            // before this rotation, I need to first translate to the origin and rotate it
            // there, then trandslate it back. 
            var I = new[,] {{1.0, 0, 0}, {0, 1.0, 0}, {0, 0, 1.0}};
            var cross = to.Normal.crossProduct(from.Normal);
            var vx = SkewSymmetricCrossProduct(cross[0], cross[1], cross[2]);
            var vx2 = SquareSkewSymmetricCrossProduct(cross[0], cross[1], cross[2]);
            var cosine = to.Normal.dotProduct(from.Normal);
            var sine = Math.Sqrt(cross[0]*cross[0] + cross[1]*cross[1] + cross[2]*cross[2]);
            if (Math.Abs(sine) < 0.01) // meaning that the angle is almost 0, and To and From faces are the same.
            {
                var uniquePositionini = VertsOnCircle[0];
                VertsOnCircle.RemoveAt(0);
                return new[,]
                {
                    {1, 0, 0, uniquePositionini[0]/Program.MeshMagnifier},
                    {0, 1, 0, uniquePositionini[1]/Program.MeshMagnifier},
                    {0, 0, 1, uniquePositionini[2]/Program.MeshMagnifier},
                    {0, 0, 0, 1.0}
                };
            }
            var c = (1 - cosine)/((double) Math.Pow(sine, 2));
            var vx2C = ConstantTimesMatrix(c, vx2);
            var rotMatr = AddMetrices(I, AddMetrices(vx, vx2C));
            var rotationMatrix = new[,]
            {
                {rotMatr[0, 0], rotMatr[0, 1], rotMatr[0, 2], 0.0},
                {rotMatr[1, 0], rotMatr[1, 1], rotMatr[1, 2], 0.0},
                {rotMatr[2, 0], rotMatr[2, 1], rotMatr[2, 2], 0.0},
                {0.0, 0.0, 0.0, 1.0}
            };
            var translateToOrigin = new[,]
            {
                {1, 0, 0, (-1)*COM.Position[0]/Program.MeshMagnifier},
                {0, 1, 0, (-1)*COM.Position[1]/Program.MeshMagnifier},
                {0, 0, 1, (-1)*COM.Position[2]/Program.MeshMagnifier},
                {0, 0, 0, 1.0}
            };
            var uniquePosition = VertsOnCircle[0];
            var translateToUniquePosition = new[,]
            {
                {1, 0, 0, uniquePosition[0]/Program.MeshMagnifier},
                {0, 1, 0, uniquePosition[1]/Program.MeshMagnifier},
                {0, 0, 1, uniquePosition[2]/Program.MeshMagnifier},
                {0, 0, 0, 1.0}
            };
            var first = MatrixTimesMatrix(translateToUniquePosition, rotationMatrix);
            var transformMatrix = MatrixTimesMatrix(first, translateToOrigin);
            VertsOnCircle.RemoveAt(0);
            return transformMatrix;
            // Now, the part will be translated to the ground.
            //var translationToGround = TranslationToGroundFinder(to, transformMatrix);
            //return MatrixTimesMatrix(translationToGround, transformMatrix);
        }

        internal static double[,] TransformationMatrixNewApproach(FootprintFace from, FootprintFace to, Vertex COM)
        {
            // Taken from: math.kennesaw.edu/~plaval/math4490/rotgen.pdf
            if (Math.Abs(from.Normal.dotProduct(to.Normal) - 1) <= 0.2)
            {
                to.Normal = from.Normal;
                return new[,]
                {
                    {1, 0, 0, 0},
                    {0, 1, 0, 0},
                    {0, 0, 1, 0},
                    {0, 0, 0, 1.0}
                };
            }
            // r is the arbitrary axis
            var r = to.Normal.crossProduct(from.Normal).normalize();
            var c = to.Normal.dotProduct(from.Normal); // cosine
            var s = Math.Sqrt(r[0] * r[0] + r[1] * r[1] + r[2] * r[2]); // sine
            var t = 1 - c;
            var rotationMatrix = new[,]
{
                {t*r[0]*r[0] + c, t*r[0]*r[1] - s*r[2], t*r[0]*r[2] + s*r[1], 0.0},
                {t*r[0]*r[1] + s*r[2], t*r[1]*r[1] + c, t*r[1]*r[2] - s*r[0], 0.0},
                {t*r[0]*r[2] - s*r[1], t*r[1]*r[2] + s*r[0], t*r[2]*r[2] + c, 0.0},
                {0.0, 0.0, 0.0, 1.0}
            };
            
            var translateToOrigin = new[,]
            {
                {1, 0, 0, (-1)*COM.Position[0]/Program.MeshMagnifier},
                {0, 1, 0, (-1)*COM.Position[1]/Program.MeshMagnifier},
                {0, 0, 1, (-1)*COM.Position[2]/Program.MeshMagnifier},
                {0, 0, 0, 1.0}
            };
            var translateBackToOriginal = new[,]
            {
                {1, 0, 0, COM.Position[0]/Program.MeshMagnifier},
                {0, 1, 0, COM.Position[1]/Program.MeshMagnifier},
                {0, 0, 1, COM.Position[2]/Program.MeshMagnifier},
                {0, 0, 0, 1.0}
            };
            var first = MatrixTimesMatrix(translateBackToOriginal, rotationMatrix);
            var transformMatrix = MatrixTimesMatrix(first, translateToOrigin);
            return transformMatrix;
            // Now, the part will be translated to the ground.
            //var translationToGround = TranslationToGroundFinder(to, transformMatrix);
            //return MatrixTimesMatrix(translationToGround, transformMatrix);
        }

        private static double[,] TransformationMatrixForStartingReferenceNewApproach(FootprintFace from,
            FootprintFace to,Vertex COM)
        {
            // Taken from: math.kennesaw.edu/~plaval/math4490/rotgen.pdf
            if (Math.Abs(from.Normal.dotProduct(to.Normal) - 1) <= 0.2)
            {
                to.Normal = from.Normal;
                var uniquePositionini = VertsOnCircle[0];
                VertsOnCircle.RemoveAt(0);
                return new[,]
                {
                    {1, 0, 0, uniquePositionini[0]/Program.MeshMagnifier},
                    {0, 1, 0, uniquePositionini[1]/Program.MeshMagnifier},
                    {0, 0, 1, uniquePositionini[2]/Program.MeshMagnifier},
                    {0, 0, 0, 1.0}
                };
            }
            // r is the arbitrary axis
            var r = to.Normal.crossProduct(from.Normal).normalize();
            var c = to.Normal.dotProduct(from.Normal); // cosine
            var t = 1 - c;
            var s =  Math.Sqrt(r[0] * r[0] + r[1] * r[1] + r[2] * r[2]); // sine
            var rotationMatrix = new[,]
{
                {t*r[0]*r[0] + c, t*r[0]*r[1] - s*r[2], t*r[0]*r[2] + s*r[1], 0.0},
                {t*r[0]*r[1] + s*r[2], t*r[1]*r[1] + c, t*r[1]*r[2] - s*r[0], 0.0},
                {t*r[0]*r[2] - s*r[1], t*r[1]*r[2] + s*r[0], t*r[2]*r[2] + c, 0.0},
                {0.0, 0.0, 0.0, 1.0}
            };

            var translateToOrigin = new[,]
            {
                {1, 0, 0, (-1)*COM.Position[0]/Program.MeshMagnifier},
                {0, 1, 0, (-1)*COM.Position[1]/Program.MeshMagnifier},
                {0, 0, 1, (-1)*COM.Position[2]/Program.MeshMagnifier},
                {0, 0, 0, 1.0}
            };
            var uniquePosition = VertsOnCircle[0];
            var translateToUniquePosition = new[,]
            {
                {1, 0, 0, uniquePosition[0]/Program.MeshMagnifier},
                {0, 1, 0, uniquePosition[1]/Program.MeshMagnifier},
                {0, 0, 1, uniquePosition[2]/Program.MeshMagnifier},
                {0, 0, 0, 1.0}
            };
            var first = MatrixTimesMatrix(translateToUniquePosition, rotationMatrix);
            var transformMatrix = MatrixTimesMatrix(first, translateToOrigin);
            VertsOnCircle.RemoveAt(0);
            return transformMatrix;
            // Now, the part will be translated to the ground.
            //var translationToGround = TranslationToGroundFinder(to, transformMatrix);
            //return MatrixTimesMatrix(translationToGround, transformMatrix);
        }

        private static double[,] TranslationToGroundFinder(PreAndCost subAssem)
        {
            var min = double.PositiveInfinity;
            PolygonalFace face = null;
            //foreach (var f in subAssem.Face.Faces)
            //{
            //    var dot = f.Normal.dotProduct(subAssem.Face.Normal);
            //    if (dot < min) min = dot;
            //    face = f;
            //}

            //var newVer = MatrixTimesMatrix(subAssem.SubAssembly.Rotate.TransformationMatrix,
            //    new[]
            //    {
            //        face.Vertices[0].Position.divide(Bridge.MeshMagnifier)[0],
            //        face.Vertices[0].Position.divide(Bridge.MeshMagnifier)[1],
            //        face.Vertices[0].Position.divide(Bridge.MeshMagnifier)[2], 1
            //    });

            foreach (var f in subAssem.Face.Faces)
            {
                var ver0 = f.Vertices[0].Position.divide(Program.MeshMagnifier);
                var ver1 = f.Vertices[1].Position.divide(Program.MeshMagnifier);
                var ver2 = f.Vertices[2].Position.divide(Program.MeshMagnifier);
                var newVer0 = MatrixTimesMatrix(subAssem.SubAssembly.Rotate.TransformationMatrix,
                    new[] {ver0[0], ver0[1], ver0[2], 1.0});
                var newVer1 = MatrixTimesMatrix(subAssem.SubAssembly.Rotate.TransformationMatrix,
                    new[] {ver1[0], ver1[1], ver1[2], 1.0});
                var newVer2 = MatrixTimesMatrix(subAssem.SubAssembly.Rotate.TransformationMatrix,
                    new[] {ver2[0], ver2[1], ver2[2], 1.0});
                var disToZero = Math.Min(Math.Min(newVer0[GravityAxis - 1], newVer1[GravityAxis - 1]),
                    newVer2[GravityAxis - 1]);
                if (disToZero < min)
                {
                    min = disToZero;
                }
            }
            //var disToZero = newVer[GravityAxis - 1];
            if (GravityAxis == 1) // it's x
            {
                return new[,]
                {
                    {1, 0, 0, -(min - Ground[GravityAxis - 1])},
                    {0, 1, 0, 0},
                    {0, 0, 1, 0},
                    {0, 0, 0, 1.0}
                };
            }
            if (GravityAxis == 2) //it's y
            {
                return new[,]
                {
                    {1, 0, 0, 0},
                    {0, 1, 0, -(min- Ground[GravityAxis - 1])},
                    {0, 0, 1, 0},
                    {0, 0, 0, 1.0}
                };
            }
            // it's z
            return new[,]
            {
                {1, 0, 0, 0},
                {0, 1, 0, 0},
                {0, 0, 1, -(min- Ground[GravityAxis - 1])},
                {0, 0, 0, 1.0}
            };
        }

        private static double[,] TranslationToGroundFinder(FootprintFace toFace, double[,] transformMatrix)
        {
            var min = double.PositiveInfinity;
            PolygonalFace face = null;
            foreach (var f in toFace.Faces)
            {
                var dot = f.Normal.dotProduct(toFace.Normal);
                if (dot < min) min = dot;
                face = f;
            }
            // then take a vertex from this face, multiply it by tansformation matrix,
            // and see how far it is from the ground
            var newVer = MatrixTimesMatrix(transformMatrix, face.Vertices[0].Position.divide(Program.MeshMagnifier));
            var disToGround = newVer[GravityAxis - 1];
            //var check1 = MatrixTimesMatrix(transformMatrix, face.Vertices[1].Position.divide(Bridge.MeshMagnifier))[1];
            //var check2 = MatrixTimesMatrix(transformMatrix, face.Vertices[2].Position.divide(Bridge.MeshMagnifier))[1];
            if (GravityAxis == 1) // it's x
            {
                return new[,]
                {
                    {1, 0, 0, -disToGround},
                    {0, 1, 0, 0},
                    {0, 0, 1, 0},
                    {0, 0, 0, 1.0}
                };
            }
            if (GravityAxis == 2) //it's y
            {
                return new[,]
                {
                    {1, 0, 0, 0},
                    {0, 1, 0, -disToGround},
                    {0, 0, 1, 0},
                    {0, 0, 0, 1.0}
                };
            }
            // it's z
            return new[,]
            {
                {1, 0, 0, 0},
                {0, 1, 0, 0},
                {0, 0, 1, -disToGround},
                {0, 0, 0, 1.0}
            };
        }

        internal static double[,] SkewSymmetricCrossProduct(double v1, double v2, double v3)
        {
            return new[,]
            {
                {0, -v3, v2},
                {v3, 0, -v1},
                {-v2, v1, 0}
            };
        }

        internal static double[,] SquareSkewSymmetricCrossProduct(double v1, double v2, double v3)
        {
            return new[,]
            {
                {-(v3*v3) - (v2*v2), v1*v2, v1*v3},
                {v1*v2, -(v3*v3) - (v1*v1), v2*v3},
                {v1*v3, v2*v3, -(v2*v2) - (v1*v1)}
            };
        }


        private static double[] RotateInstallDirection(double[] installDirection, double[,] rotationTransform)
        {
            return MatrixTimesMatrix(rotationTransform, installDirection);
        }

        internal static double[,] ConstantTimesMatrix(double c, double[,] m)
        {
            return new[,]
            {
                {c*m[0, 0], c*m[0, 1], c*m[0, 2]},
                {c*m[1, 0], c*m[1, 1], c*m[1, 2]},
                {c*m[2, 0], c*m[2, 1], c*m[2, 2]}
            };
        }

        internal static double[,] AddMetrices(double[,] n, double[,] m)
        {
            return new[,]
            {
                {n[0, 0] + m[0, 0], n[0, 1] + m[0, 1], n[0, 2] + m[0, 2]},
                {n[1, 0] + m[1, 0], n[1, 1] + m[1, 1], n[1, 2] + m[1, 2]},
                {n[2, 0] + m[2, 0], n[2, 1] + m[2, 1], n[2, 2] + m[2, 2]}
            };
        }

        public static double[,] MatrixTimesMatrix(double[,] m, double[,] n)
        {
            var multMatrix = new double[4, 4];
            for (var i = 0; i < 4; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    var e = 0.0;
                    for (var k = 0; k < 4; k++)
                    {
                        e += m[i, k]*n[k, j];
                    }
                    multMatrix[i, j] = e;
                }
            }
            return multMatrix;
        }

        private static double[] MatrixTimesMatrix(double[,] m, double[] n)
        {
            var multiplied = new double[n.Length];
            for (var i = 0; i < n.Length; i++)
            {
                multiplied[i] = 0.0;
                for (var j = 0; j < n.Length; j++)
                {
                    multiplied[i] += m[i, j]*n[j];
                }
            }
            return multiplied;
        }

        private static double DetermineDistanceToSeparateHull(TVGLConvexHull subAssemCVH, TVGLConvexHull fastenerCVH,
            Double[] insertionDirection)
        {
            var refMaxValue = GeometryFunctions.FindMaxPlaneHeightInDirection(subAssemCVH.Vertices, insertionDirection);
            var refMinValue = GeometryFunctions.FindMinPlaneHeightInDirection(subAssemCVH.Vertices, insertionDirection);

            var movingMaxValue = GeometryFunctions.FindMaxPlaneHeightInDirection(fastenerCVH.Vertices,
                insertionDirection);
            var movingMinValue = GeometryFunctions.FindMinPlaneHeightInDirection(fastenerCVH.Vertices,
                insertionDirection);

            var distanceToFree = Math.Abs(refMaxValue - movingMinValue);
            if (distanceToFree < 0)
            {
                distanceToFree = 0;
                throw new Exception("How is distance to free less than zero?");
            }
            return distanceToFree + (movingMaxValue - movingMinValue) + (refMaxValue - refMinValue);
        }
    }
}
