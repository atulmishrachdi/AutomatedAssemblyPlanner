using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using Assembly_Planner.GraphSynth.BaseClasses;
using GraphSynth.Representation;
using StarMathLib;
using TVGL;
using RandomGen;
using Component = Assembly_Planner.GraphSynth.BaseClasses.Component;


namespace Assembly_Planner
{
    public class Stabilityfunctions
    {
        public static Dictionary<string, List<string>> SucTasks;
        public static Dictionary<string, SubAssembly> InstTasks;
        public static List<SubAssembly> RefPrec;
        public static List<SubAssembly> Movings;
        public static readonly double[] GravityVector = { 0.0, -1.0, 0.0 };
        public static double Frictioncoe = 0.7;

        public class PreAndCost
        {
            public SubAssembly SubAssembly;
            public FootprintFace Face;
            public double MinCost;
            public FootprintFace FromFace;
            public SubAssembly FromSubAssembly;
        }



        //internal static Dictionary<string, double> Run(AssemblySequence solution, ReportStatus statusReporter, designGraph AssemblyGraph)
        //{
        //    var a = 1;
        //    return Dijkstra2(solution, AssemblyGraph);
        //}
        internal static Dictionary<string, double> Run(designGraph AssemblyGraph)
        {
            var a = 1;
            return Dijkstra3(AssemblyGraph);
        }

        private static Dictionary<string, double> Dijkstra3(designGraph AssemblyGraph)
        {
            GenerateReactionForceInfo(AssemblyGraph);
            var face = new PolygonalFace();
            var toFaces = new List<AssemblyEvaluator>();
            var tofacenormals = new List<double[]>
            {
                new []{0.0, 0.0, 0.0}
            };

            var refnodenames = new List<string>
            {
               //  "Assem1 - Part4",
               //"Assem1 - Part2",
               // "Assem1 - Part1",
               //  "Assem1 - b-1",
              //  "Assem1 - a-1",
                  "sim1 - L",
                "sim1 - P",
               "sim1 - R",
               "sim1 - S",
               
            };
            var movnodenames = new List<string>
            {
                // "Assem1 - Part3",
                  
                   // "Assem1 - c-1",
                //"Assem1 - Part4",
                //  "Assem1 - Part3",
               "sim1 - B",
            };

            var refnodes = new List<node>();
            var movnodes = new List<node>();
            var refarcs = new List<arc>();
            foreach (var refname in refnodenames)
            {
                refnodes.Add(AssemblyGraph.nodes.Find(a => a.name.StartsWith(refname)));
            }

            foreach (var movename in movnodenames)
            {
                movnodes.Add(AssemblyGraph.nodes.Find(a => a.name.StartsWith(movename)));
            }

            foreach (var arc in AssemblyGraph.arcs.Where(a => a is arc))
            {
                if (movnodes.Exists(a => a.name.Equals(arc.From.name) || a.name.Equals(arc.To.name)))
                {
                    continue;
                }
                refarcs.Add(arc);
            }
            var listofcomponent = new List<Component>();
            foreach (Component com in refnodes.Where(n => n is Component))
            {
                listofcomponent.Add(com);
            }
            var ss = EvaluationForBinaryTree.CreateCombinedConvexHull2(listofcomponent);

            var reductedfaces = AssemblyEvaluator.MergingFaces(ss.Faces.ToList());

            foreach (var reduceface in reductedfaces)
            {
                var tofaceNormal = reduceface.Normal;
                foreach (Component refnode in refnodes)
                {
                    //check DOF
                    var checkarcs = refarcs.FindAll(a => (a.From.name.Equals(refnode.name) && a is Connection) || (a.To.name.Equals(refnode.name) && a is Connection));
                    var alldof = GetDOF(refnode, checkarcs);
                    //cheke stability
                    var mindir = new double[3];
                    var totallinear = alldof[0] + alldof[1] + alldof[2];
                    var totalrotate = alldof[3] + alldof[4] + alldof[5];
                    double stability = 0;
                    if (totallinear <= 1 || totalrotate <= 1)
                    {
                        stability = 50;//50??
                    }
                    else
                    {
                        stability = Getstabilityscore(refnode, refarcs, tofaceNormal, out mindir);//minimum acceleration to tip a part
                    }

                    Console.WriteLine(stability);
                    Console.WriteLine("{0} {1} {2}", mindir[0], mindir[1], mindir[2]);
                }
                var rotateangles = Math.Acos(tofaceNormal.dotProduct(GravityVector)) / Math.PI * 180;
                //////check rotate////////                          

                foreach (var checknode in refnodes)
                {

                }

                ////////////////

                foreach (var checknode in refnodes)
                {
                    ///
                    /// stability score
                    ///
                    /// 

                    //  var sss = GetSubPartRemovealDirectionIndexs(checknode, refarcs);
                    var newindex = GetSubPartRemovealDirectionIndexs(checknode, refarcs);
                    var nn = Getcurrentremovaldirection(checknode, refarcs);
                    var rotatematrix = RotaMatrix(GravityVector, tofaceNormal);
                    var angles = new List<double[]>();
                    var rotatedangles = new List<double[]>();
                    foreach (var index in newindex)
                    {
                        var a = DisassemblyDirections.Directions[index];
                        var removalangles = new double[] { a[0], a[1], a[2] };
                        angles.Add(removalangles);
                        rotatedangles.Add(rotatematrix.multiply(a));
                    }
                    foreach (var rotateangle in rotatedangles)
                    {
                        var anglebetween = Math.Acos(rotateangle.dotProduct(GravityVector)) / Math.PI * 180;
                        var frictionangle = Math.Atan(Frictioncoe) / Math.PI * 180;
                        if (90 - anglebetween > frictionangle)
                        {
                            var sfd = 1;
                        }
                    }

                    ////////////////////////rotate//

                    //(reguardless moving or refence now for debuging)
                    var Spherefaces = new List<TVGL.PrimitiveSurface>();
                    var Conefaces = new List<TVGL.PrimitiveSurface>();
                    var Flatfaces = new List<TVGL.PrimitiveSurface>();
                    var cylinderfaces = new List<TVGL.PrimitiveSurface>();
                }
            }
            var taskCommands = new Dictionary<string, double>();
            return taskCommands;

        }



        public static double Getstabilityscore(node refnode, List<arc> refarcs, double[] tofaceNormal, out double[] mindir)
        {
            var score = 100000.1;
            var scoredir = new double[3];
            var compnode = (Component)refnode;
            var COM = compnode.CenterOfMass;
            var Gvector = tofaceNormal.multiply(9.8);
            var checkarcs = refarcs.FindAll(a => a.From.name.Equals(refnode.name) || a.To.name.Equals(refnode.name));
            var allpoints = new List<double[]>();

            foreach (Connection checkarc in checkarcs.Where(a => a is Connection))
            {
                foreach (var points in checkarc.UnionAreaPoints)
                {
                    foreach (var point in points)
                    {
                        //allpoints.Add(point.subtract(COM));
                        allpoints.Add(point);
                    }
                }
            }
            var bestful = new double[3];
            var trueful = new double[3];
            foreach (var dir in DisassemblyDirections.Directions)
            {
                //if (dir.dotProduct(tofaceNormal) > 0)
                //    continue;
                //var accdirection = new double[] {-0.6364, 0.7068, -0.309 };
                var accdirection = dir;
                var accvector = accdirection;
                var maxdotvalue = -1.0;
                var fulcrum = new double[3];
                var projectnormal = tofaceNormal.crossProduct(accdirection).normalize();
                foreach (var point in allpoints)
                {

                    var dotvalue = (ProjectToFace((point.subtract(COM)).normalize(), projectnormal)).dotProduct(ProjectToFace(accdirection, projectnormal));
                    //var dotvalue = ((point.subtract(COM)).normalize()).dotProduct(accdirection);
                    if (dotvalue > maxdotvalue)
                    {
                        maxdotvalue = dotvalue;
                        trueful = point;
                        fulcrum = point.subtract(COM);
                    }
                }
                var pjcom = new double[] { 0, 0, 0 };
                var pjfulcrum = ProjectToFace(fulcrum, projectnormal);
                var pjGvector = ProjectToFace(Gvector, projectnormal);
                var pjaccvector = ProjectToFace(accvector, projectnormal);
                var vm = pjGvector.subtract(pjcom);
                var lm = GeometryFunctions.DistanceBetweenLineAndVertex(vm.normalize(), pjcom, pjfulcrum);
                var va = pjaccvector.subtract(pjcom);
                var la = GeometryFunctions.DistanceBetweenLineAndVertex(va.normalize(), pjcom, pjfulcrum);
                if (pjGvector.dotProduct(pjaccvector) < pjGvector.dotProduct(pjfulcrum.normalize()))
                {
                    if (pjaccvector.crossProduct(pjfulcrum.normalize()).dotProduct(pjGvector.crossProduct(pjfulcrum.normalize())) < 0)
                    {
                        var ss = StarMath.norm1(vm) * lm / la;

                        if (ss < score)
                        {
                            score = ss;
                            scoredir = dir;
                            bestful = fulcrum.add(COM);
                        }
                    }
                }
            }
            mindir = scoredir;
            return score;
        }
        public static double Getstabilityscore(node refnode, List<arc> refarcs, double[] tofaceNormal, out double[] mindir, out double[] selected)
        {
            var totalscore = 0.0;
            var score = 100000.1;
            var scoredir = new double[3];
            var scorenormal = new double[3];
            var compnode = (Component)refnode;
            var COM = compnode.CenterOfMass;
            var checkarcs = refarcs.FindAll(a => a.From.name.Equals(refnode.name) || a.To.name.Equals(refnode.name));
            var allpoints = new List<double[]>();
            foreach (Connection checkarc in checkarcs.Where(a => a is Connection))
            {
                foreach (var points in checkarc.UnionAreaPoints)
                {
                    foreach (var point in points)
                    {
                        //allpoints.Add(point.subtract(COM));
                        allpoints.Add(point);
                    }
                }
            }
            //foreach (var tf in tofaces)
            //{
            //   var tofaceNormal = tf.Normal;

            //////need to be in a function rotate  no axis 

            var newforcedirections = new List<double[]>();
            var newforcelocations = new List<double[]>();
            var newforcepoints = new List<List<double[]>>();
            GetnewforcedirectionAndlocation(checkarcs, refnode, out newforcedirections, out newforcelocations, out newforcepoints);

            var upforcepionts = new List<List<double[]>>();
            for (int i = 0; i < newforcedirections.Count; i++)
            {
                if (newforcedirections[i].dotProduct(tofaceNormal) < 0)
                {
                    upforcepionts.Add(newforcepoints[i]);
                }
            }
            if (upforcepionts.Count == 0)
            {
                mindir = tofaceNormal;
                selected = tofaceNormal;
                return 1;//ne check
            }

            var allupforcepionts = new List<Vertex>();
            foreach (var listpoints in upforcepionts)
            {
                foreach (var point in listpoints)
                {
                    var ss = new Vertex(point);
                    allupforcepionts.Add(new Vertex(point));
                }

            }
            var nodecomponent = (Component)refnode;
            var forceCVH = MinimumEnclosure.ConvexHull2D(allupforcepionts, tofaceNormal);
            allupforcepionts.Add(new Vertex(nodecomponent.CenterOfMass));
            var forceandCMCVH = MinimumEnclosure.ConvexHull2D(allupforcepionts, tofaceNormal);

            var comlist1 = new List<double[]>();
            var comlist2 = new List<double[]>();
            foreach (var p in forceCVH)
            {
                comlist1.Add(p.Position2D);
            }
            foreach (var p in forceandCMCVH)
            {
                comlist2.Add(p.Position2D);
            }

            var sssf = comlist1.Count();
            var sssss = comlist2.Count;
            var sqer = comlist1.Except(comlist2).ToList();



            for (int i = 0; i < comlist1.Count; i++)
            {
                if (comlist1[i][0] != comlist2[i][0] || comlist1[i][1] != comlist2[i][1])
                {
                    mindir = tofaceNormal;
                    selected = tofaceNormal;

                    return -9.8;////need check
                }
            }

            //if (comlist1.Intersect(comlist2).ToList().Count() != comlist1.Count)
            //{
            //    mindir = tofaceNormal;
            //    selected = tofaceNormal;
            //    return 0;//ne check

            //} //need check
            //////////
            var Gvector = tofaceNormal.multiply(9.8);
            foreach (var dir in DisassemblyDirections.Directions)
            {
                var bestful = new double[3];
                var trueful = new double[3];
                //if (dir.dotProduct(tofaceNormal) > 0)
                //    continue;
                //var accdirection = new double[] {-0.6364, 0.7068, -0.309 };
                var accdirection = dir;
                var accvector = accdirection;
                var maxdotvalue = -1.0;
                var fulcrum = new double[3];
                var projectnormal = tofaceNormal.crossProduct(accdirection).normalize();
                foreach (var point in allpoints)
                {

                    var dotvalue = (ProjectToFace((point.subtract(COM)).normalize(), projectnormal)).dotProduct(ProjectToFace(accdirection, projectnormal));
                    //var dotvalue = ((point.subtract(COM)).normalize()).dotProduct(accdirection);
                    if (dotvalue > maxdotvalue)
                    {
                        maxdotvalue = dotvalue;
                        trueful = point;
                        fulcrum = point.subtract(COM);
                    }
                }
                var pjcom = new double[] { 0, 0, 0 };
                var pjfulcrum = ProjectToFace(fulcrum, projectnormal);
                var pjGvector = ProjectToFace(Gvector, projectnormal);
                var pjaccvector = ProjectToFace(accvector, projectnormal);
                var vm = pjGvector.subtract(pjcom);
                var lm = GeometryFunctions.DistanceBetweenLineAndVertex(vm.normalize(), pjcom, pjfulcrum);
                var va = pjaccvector.subtract(pjcom);
                var la = GeometryFunctions.DistanceBetweenLineAndVertex(va.normalize(), pjcom, pjfulcrum);
                if (pjGvector.dotProduct(pjaccvector) < pjGvector.dotProduct(pjfulcrum.normalize()))
                {
                    if (pjaccvector.crossProduct(pjfulcrum.normalize()).dotProduct(pjGvector.crossProduct(pjfulcrum.normalize())) < 0)
                    {
                        var ss = StarMath.norm1(vm) * lm / la;
                        totalscore += ss;
                        if (ss < score)
                        {
                            score = ss;
                            scoredir = dir;
                            bestful = fulcrum.add(COM);
                            //   scorenormal = tf.Normal;
                        }
                    }
                }
            }
            //}
            selected = scorenormal;
            mindir = scoredir;
            //return totalscore;
            return score;

        }

        private static double[] ProjectToFace(double[] COM, double[] projectnormal)
        {
            var d = COM.dotProduct(projectnormal);
            var nd = projectnormal.multiply(d);
            return COM.subtract(nd);
        }
        private static Dictionary<string, List<int>> GetSubPartRemovealDirectionIndexs(node checknode, List<arc> refarcs, bool s)
        {
            var removedirsbetweeneveryparts = new Dictionary<string, List<int>>();
            var checkarcs = refarcs.FindAll(a => a.From.name.Equals(checknode.name) || a.To.name.Equals(checknode.name));

            foreach (Connection arc in checkarcs.Where(a => a is Connection))
            {
                var currentindex = new List<int>();
                var othernodes = new List<node>();
                if (arc.From.name == checknode.name)
                {
                    othernodes.Add(arc.To);
                    foreach (var dir in arc.InfiniteDirections)
                    {
                        currentindex.Add(dir);
                    }
                    removedirsbetweeneveryparts.Add(arc.XmlTo, currentindex);
                }
                else
                {
                    othernodes.Add(arc.From);
                    foreach (var dir in arc.InfiniteDirections)
                    {
                        var a = DisassemblyDirections.Directions[dir];
                        var b = a.multiply(-1);
                        var bb =
                            DisassemblyDirections.Directions.First(
                                d => d[0] == b[0] && d[1] == b[1] && d[2] == b[2]);
                        var tureindex2 = DisassemblyDirections.Directions.IndexOf(bb);
                        currentindex.Add(tureindex2);
                    }
                    removedirsbetweeneveryparts.Add(arc.XmlFrom, currentindex);
                }
            }
            return removedirsbetweeneveryparts;
        }

        public static List<int> GetSubPartRemovealDirectionIndexs(node checknode, List<arc> refarcs)
        {
            var othernodes = new List<node>();
            var angleindexs = new List<int>();
            var newindex = new List<int>();
            var checkarcs = refarcs.FindAll(a => a.From.name.Equals(checknode.name) || a.To.name.Equals(checknode.name));

            foreach (Connection arc in checkarcs.Where(a => a is Connection))
            {
                if (arc.From.name == checknode.name)
                {
                    othernodes.Add(arc.To);
                    foreach (var dir in arc.InfiniteDirections)
                    {
                        angleindexs.Add(dir);
                    }
                }
                else
                {
                    othernodes.Add(arc.From);
                    foreach (var dir in arc.InfiniteDirections)
                    {
                        var a = DisassemblyDirections.Directions[dir];
                        var b = a.multiply(-1);
                        var bb =
                            DisassemblyDirections.Directions.First(
                                d => d[0] == b[0] && d[1] == b[1] && d[2] == b[2]);
                        var tureindex2 = DisassemblyDirections.Directions.IndexOf(bb);
                        angleindexs.Add(tureindex2);
                    }
                }
            }
            if (checkarcs.Where(a => a is Connection).Count() == 1)
            {
                return angleindexs;
            }
            var redumdentlindex = new List<int>();

            foreach (var angleindex in angleindexs)
            {
                var aaa = angleindexs.FindAll(a => a.Equals(angleindex));
                if (angleindexs.FindAll(a => a.Equals(angleindex)).Count() > 1)
                {
                    redumdentlindex.Add(angleindex);
                }
            }

            if (redumdentlindex.Count() == 0)
            {
                newindex = redumdentlindex;
                // newindex.Sort();
            }
            else
            {
                newindex = redumdentlindex.Distinct().ToList();
                newindex.Sort();
            }
            return newindex;
        }

        //public static Dictionary<string, double> Dijkstra2(AssemblySequence candidate, designGraph AssemblyGraph)
        //{

        //    Bridge.StatusReporter.ReportStatusMessage("Generating the Assembly Plan - Optimal orientation search ...", 1);
        //    Bridge.StatusReporter.ReportProgress(0);
        //    var taskCommands = new Dictionary<string, double>();

        //    InstTasks = new Dictionary<string, SubAssembly>();
        //    UpdatePostProcessor.BuildingInstallationTaskDictionary(candidate.Subassemblies[0]);

        //    SucTasks = new Dictionary<string, List<string>>();
        //    UpdatePostProcessor.BuildSuccedingTaskDictionary(candidate.Subassemblies[0], new List<string>());

        //    var lastTask = InstTasks[SucTasks.Keys.Where(sucT => SucTasks[sucT].Count == 0).ToList()[0]];
        //    var loopMakingSubAsse = new List<SubAssembly> { lastTask };
        //    var counter = 0;



        //    ///////////////////////////////////////////
        //    GenerateReactionForceInfo(AssemblyGraph);
        //    ////////////////////

        //    for (var h = 0; h < loopMakingSubAsse.Count; h++)
        //    {
        //        var lastSubAssEachMoving = loopMakingSubAsse[h];
        //        RefPrec = new List<SubAssembly>();
        //        Movings = new List<SubAssembly>();
        //        UpdatePostProcessor.BuildingListOfReferencePreceedings(loopMakingSubAsse[h]);

        //        // var ftask = RefPrec[RefPrec.Count - 1];
        //        var ftask = candidate.Subassemblies[0];
        //        var initialFaces = ftask.Install.Reference.CVXHull.Faces.ToList();

        //        var fromFaces = AssemblyEvaluator.MergingFaces(initialFaces);

        //        //Console.WriteLine("Which of the following faces is your current footprint face in the subassembly    " + ftask.Name + "   ?");
        //        //foreach (var f in fromFaces)
        //        //{
        //        //    var index = fromFaces.IndexOf(f);
        //        //    Console.WriteLine(index + ":" + "   " + f.Name);
        //        //}
        //        // I dont need to ask because I know the gravity (therefore I know the normal of the ground)
        //        //    Using this gravity, I will take the footprintface that has the closest normal to 
        //        //    the gravity
        //        //var read = Convert.ToInt32(Console.ReadLine());
        //        var startingFace = new FootprintFace(GravityVector);
        //        var notAffected = AssemblyEvaluator.UnaffectedRefFacesDuringInstallation(ftask);
        //        var toFaces = AssemblyEvaluator.MergingFaces(notAffected);
        //        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //        var refnodenames = ftask.Install.Reference.PartNames;
        //        var movnodenames = ftask.Install.Moving.PartNames;
        //        var refnodes = new List<node>();
        //        var movnodes = new List<node>();
        //        var refarcs = new List<arc>();
        //        foreach (var n in AssemblyGraph.nodes)
        //        {
        //            if (refnodenames.Contains(n.name))
        //            {
        //                refnodes.Add(n);
        //            }
        //            else
        //            {
        //                movnodes.Add(n);
        //            }
        //        }
        //        foreach (var arc in AssemblyGraph.arcs)
        //        {
        //            if (movnodes.Exists(a => a.name.Equals(arc.From.name) || a.name.Equals(arc.To.name)))
        //            {
        //                continue;
        //            }
        //            refarcs.Add(arc);
        //        }

        //        foreach (var toface in toFaces)
        //        {


        //            var rotateangles = Math.Acos(toface.Normal.dotProduct(GravityVector)) / Math.PI * 180;
        //            //////check rotate////////

        //            foreach (var checknode in refnodes)
        //            {
        //                var checkarcs = refarcs.FindAll(a => a.From.name.Equals(checknode.name) || a.To.name.Equals(checknode.name));
        //                //  bool anyrotate = Cananypartrotate(checknode, checkarcs, toface.Normal);

        //            }

        //            ////////////////

        //            foreach (var checknode in refnodes)
        //            {
        //                var othernodes = new List<node>();
        //                var angleindexs = new List<int>();

        //                var checkarcs = refarcs.FindAll(a => a.From.name.Equals(checknode.name) || a.To.name.Equals(checknode.name));

        //                foreach (Connection arc in checkarcs.Where(a => a is Connection))
        //                {
        //                    if (arc.From.name == checknode.name)
        //                    {
        //                        othernodes.Add(arc.To);
        //                        foreach (var dir in arc.InfiniteDirections)
        //                        {
        //                            angleindexs.Add(dir);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        othernodes.Add(arc.From);
        //                        foreach (var dir in arc.InfiniteDirections)
        //                        {
        //                            var a = DisassemblyDirections.Directions[dir];
        //                            var b = a.multiply(-1);
        //                            var bb =
        //                                DisassemblyDirections.Directions.First(
        //                                    d => d[0] == b[0] && d[1] == b[1] && d[2] == b[2]);
        //                            var tureindex2 = DisassemblyDirections.Directions.IndexOf(bb);
        //                            angleindexs.Add(tureindex2);
        //                        }
        //                    }
        //                }
        //                var redumdentlindex = new List<int>();
        //                foreach (var angleindex in angleindexs)
        //                {
        //                    var aaa = angleindexs.FindAll(a => a.Equals(angleindex));
        //                    if (angleindexs.FindAll(a => a.Equals(angleindex)).Count() > 1)
        //                    {
        //                        redumdentlindex.Add(angleindex);
        //                    }
        //                }
        //                var newindex = new List<int>();
        //                if (redumdentlindex.Count() == 0)
        //                {
        //                    newindex = angleindexs;
        //                    newindex.Sort();
        //                }
        //                else
        //                {
        //                    newindex = redumdentlindex.Distinct().ToList();
        //                    newindex.Sort();
        //                }
        //                var rotatematrix = RotaMatrix(GravityVector, toface.Normal);
        //                var angles = new List<double[]>();
        //                var rotatedangles = new List<double[]>();
        //                foreach (var index in newindex)
        //                {
        //                    var a = DisassemblyDirections.Directions[index];
        //                    var removalangles = new double[] { a[0], a[1], a[2] };
        //                    angles.Add(removalangles);
        //                    rotatedangles.Add(rotatematrix.multiply(a));
        //                }
        //                foreach (var rotateangle in rotatedangles)
        //                {
        //                    var anglebetween = Math.Acos(rotateangle.dotProduct(GravityVector)) / Math.PI * 180;
        //                    var frictionangle = Math.Atan(Frictioncoe) / Math.PI * 180;
        //                    if (90 - anglebetween > frictionangle)
        //                    {
        //                        var sfd = 1;
        //                    }
        //                }
        //                ////////////////////////rotate//



        //                //(reguardless moving or refence now for debuging)
        //                var Spherefaces = new List<TVGL.PrimitiveSurface>();
        //                var Conefaces = new List<TVGL.PrimitiveSurface>();
        //                var Flatfaces = new List<TVGL.PrimitiveSurface>();
        //                var cylinderfaces = new List<TVGL.PrimitiveSurface>();


        //            }
        //        }


        //        //////////////////////////////////////////////////////////////////////
        //        var precAndMinC = new List<PreAndCost>();

        //        foreach (var tFace in toFaces)
        //        {
        //            var stabilityAccessCost = StabilityAndAcccessabilityCostCalcultor(ftask, tFace);
        //            var rICost = RiLiCostCalculator(ftask, startingFace, tFace);
        //            var preCost = new PreAndCost
        //            {
        //                SubAssembly = ftask,
        //                Face = tFace,
        //                MinCost = rICost + stabilityAccessCost,
        //                FromFace = startingFace
        //            };
        //            precAndMinC.Add(preCost);
        //        }
        //        counter++;
        //        Bridge.StatusReporter.ReportProgress(counter / (float)(InstTasks.Count + 1));

        //        if (RefPrec.Count > 1)
        //        {
        //            for (var i = RefPrec.Count - 2; i >= 0; i--)
        //            {
        //                var curSubAsse = RefPrec[i];
        //                var preSubAsse = RefPrec[i + 1];
        //                AssemblyEvaluator.MergingFaces(initialFaces);
        //                fromFaces = toFaces;

        //                notAffected = AssemblyEvaluator.UnaffectedRefFacesDuringInstallation(curSubAsse);
        //                toFaces = new List<FootprintFace>(AssemblyEvaluator.MergingFaces(notAffected));

        //                foreach (var tFace in toFaces)
        //                {
        //                    var preCost = new PreAndCost
        //                    {
        //                        SubAssembly = curSubAsse,
        //                        FromSubAssembly = preSubAsse,
        //                        Face = tFace,
        //                        MinCost = double.PositiveInfinity
        //                    };

        //                    var stabilityAccessCost = StabilityAndAcccessabilityCostCalcultor(curSubAsse, tFace);

        //                    foreach (var fFace in fromFaces)
        //                    {
        //                        var m = precAndMinC.Where(a => a.Face == fFace && a.SubAssembly == preSubAsse).ToList();
        //                        var totalCost = m[0].MinCost + RiLiCostCalculator(curSubAsse, fFace, tFace) +
        //                                        stabilityAccessCost;
        //                        if (!(totalCost < preCost.MinCost)) continue;
        //                        preCost.MinCost = totalCost;
        //                        preCost.FromFace = fFace;
        //                    }
        //                    precAndMinC.Add(preCost);
        //                }
        //                counter++;
        //                Bridge.StatusReporter.ReportProgress(counter / (float)(InstTasks.Count + 1));
        //            }
        //        }
        //        Commander(RefPrec, precAndMinC, lastSubAssEachMoving, taskCommands);
        //        loopMakingSubAsse.Remove(lastSubAssEachMoving);
        //        h--;
        //        loopMakingSubAsse.AddRange(Movings);
        //    }
        //    Bridge.StatusReporter.ReportProgress(1);
        //    Bridge.StatusReporter.PrintMessage("AN ASSEMBLY PLAN IS SUCCESSFULLY GENERATED", 1);
        //    Bridge.StatusReporter.PrintMessage("   - NUMBER OF REQUIRED INSTALL ACTIONS:                             " + InstTasks.Count, 0.7F);
        //    return taskCommands;
        //}


        //, 
        //double[] tofacenormal
        public static double[] GetDOF(Component checknode, List<arc> checkarcs)
        {
           
            int DOF = 12;
          
            //tofacenormal = new double[] {0, -1, 0};
            //var x = new double[] { 1, 0, 0 };
            //var z = new double[] { 0, 0, 1 };

            // tofacenormal = new double[] { 0, -0.7071, 0.7071 };
            // var x = new double[] { 1, 0, 0 };
            // var z = new double[] { 0, 0.7071, 0.7071 };
            // var checkaxes = new List<double[]> { x, z };

            var nodecomponent = (Component)checknode;
            var cylindars = new List<Cylinder>();
            var cones = new List<Cone>();
            var flats = new List<Flat>();
            var allolpfs =
           BlockingDetermination.OverlappingSurfaces.FindAll(
               s =>
                   (s.Solid1.Name == checknode.name) ||
                   (s.Solid2.Name == checknode.name));

            var newolpfs = new List<OverlappedSurfaces>();
            foreach (Connection arc in checkarcs.Where(a => a is Connection))
            {
                foreach (var olp in allolpfs)
                {
                    if ((olp.Solid1.Name.Equals(arc.XmlFrom) && olp.Solid2.Name.Equals(arc.XmlTo))
                        || (olp.Solid2.Name.Equals(arc.XmlFrom) && olp.Solid1.Name.Equals(arc.XmlTo)))
                        newolpfs.Add(olp);
                }
            }
            ////liner movement
            var newforcedirections = new List<double[]>();
            var newforcelocations = new List<double[]>();
            var newforcepoints = new List<List<double[]>>();
            var crossproducts = new List<double[]>();
            var linearDOF = new double[] { 0, 0, 0, 0, 0, 0 };//x -x y -y z -z // 1 can move
            GetnewforcedirectionAndlocation(checkarcs, checknode, out newforcedirections, out newforcelocations, out newforcepoints);
            // if (newforcedirections.Count > 1)
            var currentremovaldirindexs = GetSubPartRemovealDirectionIndexs(checknode, checkarcs);
            //  var currentremovaldirindexs = Getcurrentremovaldirection(checknode, checkarcs);
            //   if (newforcedirections.Count > 1)
            var dirs = new List<double[]>();
            foreach (var dirindex in currentremovaldirindexs)
            {
                dirs.Add(DisassemblyDirections.Directions[dirindex]);
            }

            //liner DOF

            var allforcevector = new List<double[]>();
            var para = new double[4];
            var zaxis = new double[3];
            var xaxis = new double[3];
            var yaxis = new double[3];
            if (dirs.Count == 1)
            {
                linearDOF[0] = 0.5;
            }
            else
                if (dirs.Count != 0) // generate coordinate regardless of the footprint
                {
                    var c = (Connection)checkarcs.First(a => a is Connection);
                    var d0 = c.ToPartReactionForeceDirections[0];
                    var d1 = new double[3];
                    for (int i = 0; i < c.ToPartReactionForeceDirections.Count; i++)
                    {
                        if (c.ToPartReactionForeceDirections[i].dotProduct(d0) == 0)
                        {
                            d1 = c.ToPartReactionForeceDirections[i];
                            break;
                        }
                        else
                        {
                            d1 = c.UnionAreaPoints[i][0].subtract(c.UnionAreaPointsCenter[i]).normalize();
                        }
                    }
                    //if (c.ToPartReactionForeceDirections.First(v => v.dotProduct(d0).Equals(0))!=null)
                    //{
                    //    d1 = c.ToPartReactionForeceDirections.First(v => v.dotProduct(d0).Equals(0));
                    //}
                    //allforcevector = new List<double[]> { dirs[0], dirs[1], new double[3] { 0.0, 0.0, 0.0 } }; not always perpendicular or paralla to groud;

                    //   allforcevector = new List<double[]> { d0, d1, new double[3] { 0.0, 0.0, 0.0 } }; 
                    // para = Getplaneparameter(allforcevector);
                    zaxis = d1;
                    xaxis = d0;
                    yaxis = zaxis.crossProduct(xaxis).normalize();
                }


            if (currentremovaldirindexs.Count > 1)
            {
                foreach (var fdir in dirs)
                {
                    var xxx = Math.Round(fdir.dotProduct(xaxis), 8);
                    var yyy = Math.Round(fdir.dotProduct(yaxis), 8);
                    var zzz = Math.Round(fdir.dotProduct(zaxis), 8);
                    if (Math.Round(fdir.dotProduct(xaxis), 8) > 0)
                    {
                        linearDOF[0] = 0.5;
                    }
                    var wer = fdir.dotProduct(xaxis);
                    if (Math.Round(fdir.dotProduct(xaxis), 8) < 0)
                    {
                        linearDOF[1] = 0.5;
                    }
                    if (Math.Round(fdir.dotProduct(yaxis), 8) > 0)
                    {
                        linearDOF[2] = 0.5;
                    }
                    if (Math.Round(fdir.dotProduct(yaxis), 8) < 0)
                    {
                        linearDOF[3] = 0.5;
                    }
                    if (Math.Round(fdir.dotProduct(zaxis), 8) > 0)
                    {
                        linearDOF[4] = 0.5;
                    }
                    var ss = fdir.dotProduct(zaxis);
                    if (Math.Round(fdir.dotProduct(zaxis), 8) < 0)
                    {
                        linearDOF[5] = 0.5;
                    }
                }
            }

            var cyaxesanchor = new List<double[]>();
            var cnaxesanchor = new List<double[]>();
            ////Check # of cylindars

            foreach (var olps in newolpfs)
            {
                foreach (var OL in olps.Overlappings)
                {
                    if (OL[0] is Cylinder && OL[1] is Cylinder)
                    {
                        var cy = (Cylinder)OL[0];
                        cylindars.Add(cy);
                        cyaxesanchor.Add(cy.Anchor);
                    }
                }
            }

            foreach (var olps in newolpfs)
            {
                foreach (var OL in olps.Overlappings)
                {
                    if (OL[0] is Cone && OL[1] is Cone)
                    {
                        var cone = (Cone)OL[0];
                        cones.Add(cone);
                        cnaxesanchor.Add(cone.Axis);//TBD
                    }
                }
            }

            var rotateDOF = new double[] { 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, };
            //   int diffcyln = 0;
            int diffcyln = cyaxesanchor.Union(cyaxesanchor).ToList().Count;
            int diffcon = cnaxesanchor.Union(cnaxesanchor).ToList().Count;
            var iscone = false;
            var iscyln = false;
            //for cyln
            if (diffcyln == 1 || diffcyln == 2) // old one is bad
            {
                iscyln = true;
                if (
                    (newforcedirections.Count == 0
                    || newforcedirections.All(f => f.dotProduct(cylindars[0].Axis).Equals(-1)
                        || f.dotProduct(cylindars[0].Axis).Equals(1))))
                {
                    if (linearDOF[0] == 0.5 || linearDOF[1] == 0.5)
                    {
                        rotateDOF[0] = 0.5;
                        rotateDOF[1] = 0.5;
                    }
                    else if (linearDOF[2] == 0.5 || linearDOF[3] == 0.5)
                    {
                        rotateDOF[2] = 0.5;
                        rotateDOF[3] = 0.5;
                    }

                    else if (linearDOF[4] == 0.5 || linearDOF[5] == 0.5)
                    {
                        rotateDOF[4] = 0.5;
                        rotateDOF[5] = 0.5;
                    }
                    else
                    {
                        rotateDOF[0] = 0.5;
                        rotateDOF[1] = 0.5;
                    }
                }

                //cylinder with blockingface
                var cannotrotate = false;
                // var sss = cylindars[0].Axis.normalize();
                var checkslop = false;
                var pjaxle = new double[3];
                if (iscyln)
                {
                    pjaxle = ProjectToFace(cyaxesanchor[0], cylindars[0].Axis.normalize());
                }
                if (iscone)
                {
                    pjaxle = ProjectToFace(cnaxesanchor[0], cones[0].Axis.normalize());
                }
                for (int i = 0; i < newforcedirections.Count; i++)
                {

                    var fd = newforcedirections[i].dotProduct(cylindars[0].Axis.normalize());
                    if (fd < 0.99 && fd > -0.99)
                    {
                        var pjdir = ProjectToFace(newforcedirections[i], cylindars[0].Axis);
                        var pjanchor = ProjectToFace(cylindars[0].Anchor, cylindars[0].Axis);
                        var saverefcross = false;
                        var refcross = new double[3];
                        var currencross = new double[3];

                        foreach (var point in newforcepoints[i])
                        {
                            if (saverefcross == false)
                            {
                                refcross = ProjectToFace(point, cylindars[0].Axis).subtract(pjanchor).normalize().crossProduct(pjdir.normalize()).normalize();
                                saverefcross = true;
                                continue;
                            }
                            currencross = ProjectToFace(point, cylindars[0].Axis).subtract(pjanchor).normalize().crossProduct(pjdir.normalize()).normalize();
                            if (cannotrotate == false)
                            {
                                var sasdf = refcross.dotProduct(currencross);
                                if (refcross.dotProduct(currencross) > 0)
                                {
                                    continue;
                                }
                                cannotrotate = true;
                                //  checkslop = true;
                            }
                        }
                    }
                    if (!cannotrotate)
                    {
                        rotateDOF[0] = 0.5;
                    }
                    else
                    {
                        rotateDOF[0] = 0;
                    }
                }
            }


            //for cone
            if (diffcon == 1 || diffcon == 2) // old one is bad
            {
                iscone = true;
                if (

                    (newforcedirections.Count == 0
                    || newforcedirections.All(f => Math.Round(f.dotProduct(cones[0].Axis), 8).Equals(-1)
                        || Math.Round(f.dotProduct(cones[0].Axis), 8).Equals(1)))
                    )
                //for cone
                {
                    if (linearDOF[0] == 0 || linearDOF[1] == 0)
                    {
                        rotateDOF[0] = 0.5;
                        rotateDOF[1] = 0.5;
                    }
                    else if (linearDOF[2] == 0 || linearDOF[3] == 0)
                    {
                        rotateDOF[2] = 0.5;
                        rotateDOF[3] = 0.5;
                    }

                    else if (linearDOF[4] == 0 || linearDOF[5] == 0)
                    {
                        rotateDOF[4] = 0.5;
                        rotateDOF[5] = 0.5;
                    }
                    else
                    {
                        rotateDOF[0] = 0.5;
                        rotateDOF[1] = 0.5;
                    }
                }
                //cone with blockingface
                //temp
                var cannotrotate = false;
                // var sss = cylindars[0].Axis.normalize();
                var checkslop = false;
                var pjaxle = new double[3];
                if (iscyln)
                {
                    pjaxle = ProjectToFace(cyaxesanchor[0], cylindars[0].Axis.normalize());
                }
                if (iscone)
                {
                    pjaxle = ProjectToFace(cnaxesanchor[0], cones[0].Axis.normalize());
                }
                for (int i = 1; i < newforcedirections.Count; i++)
                {

                    var fd = newforcedirections[i].dotProduct(cones[0].Axis.normalize());
                    if (fd < 0.99 && fd > -0.99)
                    {
                        var pjdir = ProjectToFace(newforcedirections[i], cones[0].Axis);
                        var pjanchor = ProjectToFace(cones[0].Apex, cones[0].Axis);
                        var saverefcross = false;
                        var refcross = new double[3];
                        var currencross = new double[3];

                        foreach (var point in newforcepoints[i])
                        {
                            if (saverefcross == false)
                            {
                                refcross = ProjectToFace(point, cones[0].Axis).subtract(pjanchor).normalize().crossProduct(pjdir.normalize()).normalize();
                                saverefcross = true;
                                continue;
                            }
                            currencross = ProjectToFace(point, cones[0].Axis).subtract(pjanchor).normalize().crossProduct(pjdir.normalize()).normalize();
                            if (cannotrotate == false)
                            {
                                var sasdf = refcross.dotProduct(currencross);
                                if (refcross.dotProduct(currencross) > 0)
                                {
                                    continue;
                                }
                                cannotrotate = true;
                                //  checkslop = true;
                            }
                        }
                    }
                    if (!cannotrotate)
                    {
                        rotateDOF[0] = 0.5;
                    }
                    else
                    {
                        rotateDOF[0] = 0;
                    }
                }

            }


            //no cylindar rotateion
            var removalfacecounter = 0;
            var foundpara = false;
            var rotateflatpara = new double[4];
            var candir = new List<double[]>();

            foreach (var dir in dirs)
            {
                var product = new List<double>();
                for (int i = 0; i < dirs.Count; i++)
                {
                    var ss = dir.dotProduct(dirs[i]);
                    if (ss < -0.9)
                    {
                        var qwer = 1;
                    }

                }
                if (dirs.Any(d => Math.Round(d.dotProduct(dir), 8) == -1))
                {
                    candir.Add(dir);
                }
            }
            //temp comment
            if (candir.Count >= 3)
            {
                for (int i = 0; i < candir.Count - 2; i++)
                {
                    if (foundpara == true)
                        break;
                    for (int j = i + 1; j < candir.Count - 1; j++)
                    {
                        if (foundpara == true)
                            break;
                        if (Math.Round(candir[i].dotProduct(candir[j]), 8) == -1)
                            continue;
                        for (int k = j + 1; k < candir.Count; k++)
                        {
                            if (foundpara == true)
                                break;
                            rotateflatpara = Getplaneparameter(new List<double[]> { candir[i], candir[j], new double[] { 0, 0, 0 } });
                            var ff = rotateflatpara[0] * candir[k][0] + rotateflatpara[1] * candir[k][1] + rotateflatpara[2] * candir[k][2] + rotateflatpara[3];
                            if (ff == 0)
                            {
                                foundpara = true;
                                break;
                            }
                        }
                    }
                }
            }


            if (foundpara == true)
            {
                if (linearDOF[0] == 0 || linearDOF[1] == 0)
                {
                    rotateDOF[0] = 0.5;
                    rotateDOF[1] = 0.5;
                }
                else if (linearDOF[2] == 0 || linearDOF[3] == 0)
                {
                    rotateDOF[2] = 0.5;
                    rotateDOF[3] = 0.5;
                }

                else if (linearDOF[4] == 0 || linearDOF[5] == 0)
                {
                    rotateDOF[4] = 0.5;
                    rotateDOF[5] = 0.5;
                }
                else
                {
                    rotateDOF[0] = 0.5;
                    rotateDOF[1] = 0.5;
                }
            }
            //no cylindar rotateion
            Console.WriteLine("linearDOF");
            //Console.WriteLine("X, -X, Y, -Y, Z, -Z");
            Console.WriteLine("X:{0}, -X:{1}, Y:{2}, -Y:{3}, Z:{4}, -Z:{5}", linearDOF[0], linearDOF[1], linearDOF[2], linearDOF[3], linearDOF[4], linearDOF[5]);
            Console.WriteLine("rotateDOF");

            Console.WriteLine("X:{0}, -X:{1}, Y:{2}, -Y:{3}, Z:{4}, -Z:{5}", rotateDOF[0], rotateDOF[1], rotateDOF[2], rotateDOF[3], rotateDOF[4], rotateDOF[5]);
            var alldof = new double[12];
            for (int i = 0; i < 6; i++)
            {
                alldof[i] = linearDOF[i];
                alldof[i + 6] = rotateDOF[i];
            }
            //   var sameplatvet = new List<double[]>();
            //foreach (var dir in dirs)
            //{
            //    if (rotateflatpara[0] * dir[0] + rotateflatpara[1] * dir[1] + rotateflatpara[0] * dir[2] + rotateflatpara[3] == 0)
            //    {
            //        sameplatvet.Add(dir);
            //    }
            //}
            //var canrotate = false;
            //foreach (var dir in sameplatvet)
            //{
            //    if (!sameplatvet.Any(v => Math.Round(v.dotProduct(dir)) == -1))
            //    {
            //        canrotate = false;
            //    }
            //}

            //foreach (var olps in newolpfs)
            //{
            //    foreach (var OL in olps.Overlappings)
            //    {
            //        if (OL[0] is Sphere && OL[1] is Sphere)
            //        {
            //            var sphere = (Sphere)OL[0];

            //        }
            //    }
            //}






            //if (diffcyln > 1)
            //    return alldof;
            //else if (diffcyln == 1)
            //{
            //    //if (cylindars[0].Axis[0] != 0 || cylindars[0].Axis[2] != 0)// axe is not horizontal
            //    //{
            //    //     var newforcedirections = new List<double[]>();
            //    //    var newforcelocations = new List<double[]>();
            //    //   var crossproducts = new List<double[]>();
            //    //      GetnewforcedirectionAndlocation(checkarcs, checknode, out newforcedirections, out newforcelocations);
            //    for (int i = 0; i < newforcelocations.Count; i++)
            //    {
            //        var crossproduct = newforcelocations[i].subtract(cylindars[0].Anchor).crossProduct(
            //            newforcelocations[i].add(newforcedirections[i]).subtract(cylindars[0].Anchor));
            //        crossproducts.Add(crossproduct.normalize());
            //    }
            //    crossproducts.Add((nodecomponent.CenterOfMass.subtract(cylindars[0].Anchor).crossProduct(
            //               nodecomponent.CenterOfMass.add(tofacenormal).subtract(cylindars[0].Anchor))).normalize());
            //    var dotproducts = new List<double>();
            //    foreach (var cp in crossproducts)
            //    {
            //        var dot = cp.dotProduct(cylindars[0].Axis);
            //        if (dot > -0.01 && dot < 0.01)
            //            dot = 0;
            //        dotproducts.Add(dot);
            //    }
            //    if (dotproducts.Any(a => a > 0) && dotproducts.Any(a => a < 0)) //no rotation
            //    {
            //        DOF -= 6;

            //    }
            //    return alldof;
            //    //}
            //}
            //else
            //{
            //    // var anchor = axe.add(nodecomponent.CenterOfMass);
            //    //    var newforcedirections = new List<double[]>();
            //    //   var newforcelocations = new List<double[]>();
            //    //       var newforcepoints = new List<List<double[]>>();
            //    //  var crossproducts = new List<double[]>();
            //    //  GetnewforcedirectionAndlocation(checkarcs, checknode, out newforcedirections, out newforcelocations, out newforcepoints);
            //    var twoDconvexhull = new List<List<Point>>();


            //    /////////////////check all axes
            //    var anchor = nodecomponent.CenterOfMass;
            //    for (int i = 0; i < newforcelocations.Count; i++)
            //    {
            //        var crossproduct = newforcelocations[i].subtract(anchor).crossProduct(
            //            newforcelocations[i].add(newforcedirections[i]).subtract(anchor));
            //        crossproducts.Add(crossproduct.normalize());
            //    }
            //    int passednumber = 0;
            //    foreach (var axe in checkaxes)
            //    {
            //        //crossproducts.Add((nodecomponent.CenterOfMass.subtract(anchor).crossProduct(
            //        //           nodecomponent.CenterOfMass.add(tofacenormal).subtract(anchor))).normalize());
            //        var dotproducts = new List<double>();
            //        foreach (var cp in crossproducts)
            //        {
            //            var dot = cp.dotProduct(axe);
            //            if (dot > -0.05 && dot < 0.05)
            //                dot = 0;
            //            dotproducts.Add(dot);
            //        }
            //        if (dotproducts.All(a => a > 0) || dotproducts.All(a => a < 0))
            //            continue;
            //        else if (dotproducts.All(a => a.Equals(0)))
            //        {
            //            passednumber += 1;
            //        }
            //        else if (dotproducts.Any(a => a > 0) && dotproducts.Any(a => a < 0))
            //        {
            //            passednumber += 1;
            //        }
            //    }
            //    if (passednumber == 2)
            //    return alldof;

            ////////////////////////check bottom face
            //var upforcepionts = new List<List<double[]>>();
            //for (int i = 0; i < newforcedirections.Count; i++)
            //{
            //    if (newforcedirections[i].dotProduct(tofacenormal) < 0)
            //    {
            //        upforcepionts.Add(newforcepoints[i]);
            //    }
            //}
            //if (upforcepionts.Count == 0)
            //    return alldof;//ne check
            //var allupforcepionts = new List<Vertex>();
            //foreach (var listpoints in upforcepionts)
            //{
            //    foreach (var point in listpoints)
            //    {
            //        var ss = new Vertex(point);
            //        allupforcepionts.Add(new Vertex(point));
            //    }

            //}
            //var forceCVH = MinimumEnclosure.ConvexHull2D(allupforcepionts, tofacenormal);
            //allupforcepionts.Add(new Vertex(nodecomponent.CenterOfMass));
            //var forceandCMCVH = MinimumEnclosure.ConvexHull2D(allupforcepionts, tofacenormal);

            //var comlist1 = new List<double[]>();
            //var comlist2 = new List<double[]>();
            //foreach (var p in forceCVH)
            //{
            //    comlist1.Add(p.Position2D);
            //}
            //foreach (var p in forceandCMCVH)
            //{
            //    comlist2.Add(p.Position2D);
            //}

            //var sssf = comlist1.Count();
            //var sssss = comlist2.Count;
            //if (comlist1.Intersect(comlist2).Count() == comlist1.Count)
            //    return alldof;//need check


            return alldof;//need check

        }

        private static List<int> Getcurrentremovaldirection(node checknode, List<arc> checkarcs)
        {

            var n = (Component)checknode;
            var currentdirs = new List<int>();
            var otherpoartnames = new List<string>();
            var newdirindexs = new List<int>();
            var redumdentlindex = new List<int>();

            foreach (var arc in checkarcs.Where(c => c is Connection))
            {
                if (arc.XmlFrom == checknode.name)
                {
                    otherpoartnames.Add(arc.XmlTo);
                }
                else
                {
                    otherpoartnames.Add(arc.XmlFrom);
                }
            }
            foreach (var othername in otherpoartnames)
            {
                var dirindexs = n.RemovealDirectionsforEachPart[othername];
                foreach (var dirindex in dirindexs)
                {
                    currentdirs.Add(dirindex);
                }
            }

            foreach (var angleindex in currentdirs)
            {
                var aaa = currentdirs.FindAll(a => a.Equals(angleindex));
                if (currentdirs.FindAll(a => a.Equals(angleindex)).Count() > 1)
                {
                    redumdentlindex.Add(angleindex);
                }
            }

            if (redumdentlindex.Count() == 0)
            {
                newdirindexs = currentdirs;
                newdirindexs.Sort();
            }
            else
            {
                newdirindexs = redumdentlindex.Distinct().ToList();
                newdirindexs.Sort();
            }

            var turedir = new List<double[]>();
            foreach (var ind in newdirindexs)
            {
                turedir.Add(DisassemblyDirections.Directions[ind]);
            }
            return newdirindexs;
            //   return currentdirs;
        }

        private static void GetnewforcedirectionAndlocation(List<arc> checkarcs, node checknode, out List<double[]> newforcedirections, out List<double[]> newforcelocations, out List<List<double[]>> newforcepoints)
        {
            newforcedirections = new List<double[]>();
            newforcelocations = new List<double[]>();
            newforcepoints = new List<List<double[]>>();

            foreach (Connection connectedarc in checkarcs.Where(a => a is Connection))
            {
                var rotatereactionforceIndexs = new List<int>();
                if (connectedarc.From.name == checknode.name)
                {


                    foreach (var dir in connectedarc.FromPartReactionForeceDirections)
                    {
                        //if (dir.dotProduct(cylindars[0].Axis) == -1 || dir.dotProduct(cylindars[0].Axis) == 1)
                        //    continue;
                        rotatereactionforceIndexs.Add(connectedarc.FromPartReactionForeceDirections.IndexOf(dir));

                    }
                    foreach (var index in rotatereactionforceIndexs)
                    {
                        newforcedirections.Add(connectedarc.FromPartReactionForeceDirections[index]);
                        newforcelocations.Add(connectedarc.UnionAreaPointsCenter[index]);
                        newforcepoints.Add(connectedarc.UnionAreaPoints[index]);
                    }
                }
                else
                {
                    foreach (var dir in connectedarc.ToPartReactionForeceDirections)
                    {
                        //if (dir.dotProduct(cylindars[0].Axis) == -1 || dir.dotProduct(cylindars[0].Axis) == 1)
                        //    continue;
                        rotatereactionforceIndexs.Add(connectedarc.ToPartReactionForeceDirections.IndexOf(dir));
                    }
                    foreach (var index in rotatereactionforceIndexs)
                    {
                        newforcedirections.Add(connectedarc.ToPartReactionForeceDirections[index]);
                        newforcelocations.Add(connectedarc.UnionAreaPointsCenter[index]);
                        newforcepoints.Add(connectedarc.UnionAreaPoints[index]);
                    }
                }

            }
        }

        private static void GetnewforcedirectionAndlocation(List<arc> checkarcs, node checknode, out List<double[]> newforcedirections, out List<double[]> newforcelocations)
        {
            newforcedirections = new List<double[]>();
            newforcelocations = new List<double[]>();
            foreach (Connection connectedarc in checkarcs.Where(a => a is Connection))
            {
                var rotatereactionforceIndexs = new List<int>();
                if (connectedarc.From.name == checknode.name)
                {
                    foreach (var dir in connectedarc.FromPartReactionForeceDirections)
                    {
                        //if (dir.dotProduct(cylindars[0].Axis) == -1 || dir.dotProduct(cylindars[0].Axis) == 1)
                        //    continue;
                        rotatereactionforceIndexs.Add(connectedarc.FromPartReactionForeceDirections.IndexOf(dir));

                    }
                    foreach (var index in rotatereactionforceIndexs)
                    {
                        newforcedirections.Add(connectedarc.FromPartReactionForeceDirections[index]);
                        newforcelocations.Add(connectedarc.UnionAreaPointsCenter[index]);
                    }
                }
                else
                {
                    foreach (var dir in connectedarc.ToPartReactionForeceDirections)
                    {
                        //if (dir.dotProduct(cylindars[0].Axis) == -1 || dir.dotProduct(cylindars[0].Axis) == 1)
                        //    continue;
                        rotatereactionforceIndexs.Add(connectedarc.ToPartReactionForeceDirections.IndexOf(dir));
                    }
                    foreach (var index in rotatereactionforceIndexs)
                    {
                        newforcedirections.Add(connectedarc.ToPartReactionForeceDirections[index]);
                        newforcelocations.Add(connectedarc.UnionAreaPointsCenter[index]);
                    }
                }
            }
        }



        public static void GenerateReactionForceInfo(designGraph AssemblyGraph)
        {

            foreach (Component n in AssemblyGraph.nodes.Where(n => n is Component))
            {
                var la = new List<arc>();
                n.RemovealDirectionsforEachPart = GetSubPartRemovealDirectionIndexs(n, AssemblyGraph.arcs.FindAll(a => a.XmlTo.Equals(n.name) || a.XmlFrom.Equals(n.name)), true);
            }

            foreach (Connection arc in AssemblyGraph.arcs.Where(a => a is Connection))
            {
                var topartname = arc.To.name;
                var frompartname = arc.From.name;
                //   var topartname = AssemblyGraph.nodes.Find(a => a.name.StartsWith("Assem2.1")).name;
                //   var frompartname = AssemblyGraph.nodes.Find(a => a.name.StartsWith("Assem2.3")).name;
                var a2 =
               BlockingDetermination.OverlappingSurfaces.First(
                   s =>
                       (s.Solid1.Name == topartname && s.Solid2.Name == frompartname) ||
                       (s.Solid1.Name == frompartname && s.Solid2.Name == topartname));
                int checkindex;

                if (topartname == a2.Solid1.Name)
                {
                    checkindex = 0;
                }
                else
                {
                    checkindex = 1;
                }
                int otherindex = 1 - checkindex;
                var OLPS = a2.Overlappings;
                //foreach (var faces in OLPS)
                //{
                //    foreach (var face in faces)
                //    {
                //        if (face.GetType().FullName == "TVGL.Sphere")
                //        {
                //            Spherefaces.Add(face);
                //        }
                //        else if (face.GetType().FullName == "TVGL.Cone")
                //        {
                //            Conefaces.Add(face);
                //        }
                //        else if (face.GetType().FullName == "TVGL.Flat")
                //        {
                //            Flatfaces.Add(face);
                //        }
                //        else
                //            cylinderfaces.Add(face);
                //    }
                //};
                //   if (cylinderfaces[0].Faces[0].)
                var checkredum = new List<PrimitiveSurface[]>();
                //  var currentbestcylindar = new Cylinder();
                foreach (var pairsurfaces in OLPS)
                {
                    if (pairsurfaces[0] is Cylinder & pairsurfaces[1] is Cylinder)
                        if (OLPS.Count == 18)
                        {
                            var w = 1;
                        }
                    bool abnormalpermitive = filter(pairsurfaces);
                    if (!abnormalpermitive)
                    {
                        checkredum.Add(pairsurfaces);
                    }
                    var arctoadd =
                        (Connection)AssemblyGraph.arcs.Find(
                            a => a is Connection &&
                                (a.From.name.Equals(frompartname) && a.To.name.Equals(topartname)) ||
                                (a.To.name.Equals(frompartname) && a.From.name.Equals(topartname)));
                    if (pairsurfaces[0] is Cylinder & pairsurfaces[1] is Cylinder)
                    {
                        var cy = (Cylinder)pairsurfaces[0];
                        arctoadd.Axes.Add(GetCylinderAxes(cy));
                        continue;
                    }
                    if (pairsurfaces[0] is Cone & pairsurfaces[1] is Cone)
                    {
                        var cy = (Cone)pairsurfaces[0];
                        arctoadd.Axes.Add(GetConeAxes(cy));
                        continue;
                    }


                    var t = pairsurfaces[0].GetType().ToString();
                    var projectedcheckfaces = new List<TVGL.PolygonalFace>();
                    var projectedotherfaces = new List<TVGL.PolygonalFace>();
                    var olpPolygonalFaces = new List<TVGL.PolygonalFace>();
                    foreach (var cface in pairsurfaces[checkindex].Faces)
                    {
                        foreach (var oface in pairsurfaces[otherindex].Faces)
                        {
                            if (Istwofacesoverlapping(cface, oface))
                            {
                                if (projectedotherfaces.Contains(oface) == false)
                                    projectedotherfaces.Add(oface);
                                if (projectedcheckfaces.Contains(cface) == false)
                                    projectedcheckfaces.Add(cface);
                                if (olpPolygonalFaces.Contains(oface) == false)
                                    olpPolygonalFaces.Add(oface);
                                if (olpPolygonalFaces.Contains(cface) == false)
                                    olpPolygonalFaces.Add(cface);
                            }
                        }
                    }
                    var allvert = new List<Vertex>();
                    foreach (var olpface in olpPolygonalFaces)
                    {
                        foreach (var vert in olpface.Vertices)
                        {
                            if (allvert.Contains(vert) == false)
                                allvert.Add(vert);
                        }
                    }

                    //var ver =
                    //    allvert.Where(
                    //        v =>
                    //            (v.X > 27.4 && v.X < 27.6) && (v.Y > 95.5 && v.Y < 95.7) &&
                    //            (v.Z > 42.7 && v.Z < 42.8)).ToList();
                    // toface.Normal= new double[]{0,0,-1};
                    // toface.Normal = new double[] { 0, 0, 1 };
                    // var projectedfacesCVH = MinimumEnclosure.ConvexHull2D(allvert, toface.Normal);
                    double maxX = double.NegativeInfinity;
                    double minX = double.PositiveInfinity;
                    double maxY = double.NegativeInfinity;
                    double minY = double.PositiveInfinity;
                    double maxZ = double.NegativeInfinity;
                    double minZ = double.PositiveInfinity;
                    foreach (var vert in allvert)
                    {
                        if (vert.X > maxX)
                            maxX = vert.X;
                        if (vert.X < minX)
                            minX = vert.X;
                        if (vert.Y > maxY)
                            maxY = vert.Y;
                        if (vert.Y < minY)
                            minY = vert.Y;
                        if (vert.Z > maxZ)
                            maxZ = vert.Z;
                        if (vert.Z < minZ)
                            minZ = vert.Z;
                    }

                    var bound = new List<double>();
                    bound.Add(maxX - minX);
                    bound.Add(maxY - minY);
                    bound.Add(maxZ - minZ);
                    if (bound.Any(b => b < -0.000001))
                    {
                        var see = 1;
                    }
                    int minlenghtindx = bound.IndexOf(bound.Min());
                    int numberofdots = Convert.ToInt32(bound.Max() / 0.1);

                    var randompioints = GetRandomPoints(maxX, minX, maxY, minY, maxZ, minZ, olpPolygonalFaces[0], numberofdots, minlenghtindx);
                    // var randompioints = ProjectPointoFace(Spacerandompioints, olpPolygonalFaces[0]);
                    // var randompioints = new double[numberofdots, 3];


                    ///add dots

                    var face1pints = new List<List<Vertex>>();
                    var face2pints = new List<List<Vertex>>();
                    foreach (var face1 in projectedcheckfaces)
                    {
                        //  face1pints.Add(MinimumEnclosure.ConvexHull2D(face1.Vertices, toface.Normal));
                        face1pints.Add(face1.Vertices);
                    }
                    foreach (var face2 in projectedotherfaces)
                    {
                        //face2pints.Add(MinimumEnclosure.ConvexHull2D(face2.Vertices, toface.Normal));
                        face2pints.Add(face2.Vertices);
                    }

                    var OLPpioints = new List<double[]>();


                    //for (int i = 0; i < randompioints.GetLength(0); i++)
                    //{
                    //    var randompoint = new double[] { randompioints[i, 0], randompioints[i, 1], randompioints[i, 2] };

                    //    foreach (var points1 in face1pints)
                    //    {
                    //        foreach (var points2 in face2pints)
                    //        {

                    //            if (Ispointwithinthreetrianglepoints(randompoint, points1) &&
                    //                   Ispointwithinthreetrianglepoints(randompoint, points2))
                    //            {
                    //                OLPpioints.Add(randompoint);
                    //            }
                    //        }
                    //    }
                    //}
                    var pointsinface1 = new List<double[]>();
                    var pointsinface2 = new List<double[]>();
                    for (int i = 0; i < randompioints.GetLength(0); i++)
                    {
                        var randompoint = new double[] { randompioints[i, 0], randompioints[i, 1], randompioints[i, 2] };

                        foreach (var points1 in face1pints)
                        {


                            if (Ispointwithinthreetrianglepoints(randompoint, points1))
                            {
                                pointsinface1.Add(randompoint);
                            }

                        }


                        foreach (var points2 in face2pints)
                        {

                            if (Ispointwithinthreetrianglepoints(randompoint, points2))
                            {
                                pointsinface2.Add(randompoint);
                            }
                        }

                    }
                    OLPpioints = pointsinface1.Intersect(pointsinface2).ToList();
                    var dotx = new List<double>();
                    var doty = new List<double>();
                    var dotz = new List<double>();
                    foreach (var OLPpioint in OLPpioints)
                    {
                        dotx.Add(OLPpioint[0]);
                        doty.Add(OLPpioint[1]);
                        dotz.Add(OLPpioint[2]);
                    }



                    //AssemblyGraph.arcs.Find(a=>a is Connection).
                    arctoadd.UnionAreaPoints.Add(OLPpioints);
                    arctoadd.UnionAreaPointsCenter.Add(GetPointsCenter(OLPpioints));
                    //if (checkindex == 0)
                    //{
                    //    arctoadd.FromPartReactionForeceDirectons.Add(a2.Solid2.Faces[0].Normal);
                    //    arctoadd.ToPartReactionForeceDirecton.Add(a2.Solid1.Faces[0].Normal);
                    //}
                    //else
                    //{
                    arctoadd.ToPartReactionForeceDirections.Add(pairsurfaces[otherindex].Faces[0].Normal);
                    arctoadd.FromPartReactionForeceDirections.Add(pairsurfaces[checkindex].Faces[0].Normal);
                    //}

                    //arctoadd.FromPartReactionForeceDirectons(pairsurfaces[]);


                    var xmax = dotx.Max();
                    var xmin = dotx.Min();
                    var ymax = doty.Max();
                    var ymin = doty.Min();
                    var zmax = dotz.Max();
                    var zmin = dotz.Min();

                }
                var sss = checkredum;
            }
        }

        private static bool filter(PrimitiveSurface[] pairsurfaces)
        {
            if (pairsurfaces.Any(f => f is Cone) && pairsurfaces.Any(f => f is Flat))
            {
                return true;
            }
            if (pairsurfaces.Any(f => f is Cone) && pairsurfaces.Any(f => f is Cylinder))
            {
                return true;
            }
            if (pairsurfaces.Any(f => f is Cone) && pairsurfaces.Any(f => f is Sphere))
            {
                return true;
            }
            if (pairsurfaces.Any(f => f is Flat) && pairsurfaces.Any(f => f is Cylinder))
            {
                return true;
            }
            if (pairsurfaces.Any(f => f is Flat) && pairsurfaces.Any(f => f is Sphere))
            {
                return true;
            }

            if (pairsurfaces.Any(f => f is Cylinder) && pairsurfaces.Any(f => f is Sphere))
            {
                return true;
            }


            if (pairsurfaces.Any(f => f is Cylinder) && pairsurfaces.Any(f => f is Cylinder))
            {
                double vertex1 = pairsurfaces[0].Vertices.Count;
                double vertex2 = pairsurfaces[1].Vertices.Count;
                double sumv = vertex1 + vertex2;
                double sss = vertex1 / sumv;

                if (sss < 0.4 || sss > 0.6)
                    return true;
            }

            return false;
        }


        private static double[,] GetCylinderAxes(Cylinder cy)
        {
            var newaxex = new double[2, 3];

            newaxex[0, 0] = cy.Anchor[0];
            newaxex[0, 1] = cy.Anchor[1];
            newaxex[0, 2] = cy.Anchor[2];
            newaxex[1, 0] = (cy.Anchor.add(cy.Axis))[0];
            newaxex[1, 1] = (cy.Anchor.add(cy.Axis))[1];
            newaxex[1, 2] = (cy.Anchor.add(cy.Axis))[2];
            return newaxex;
        }
        private static double[,] GetConeAxes(Cone cy)
        {
            var newaxex = new double[2, 3];

            newaxex[0, 0] = cy.Apex[0];
            newaxex[0, 1] = cy.Apex[1];
            newaxex[0, 2] = cy.Apex[2];
            newaxex[1, 0] = (cy.Apex.add(cy.Axis))[0];
            newaxex[1, 1] = (cy.Apex.add(cy.Axis))[1];
            newaxex[1, 2] = (cy.Apex.add(cy.Axis))[2];
            return newaxex;
        }

        private static double[,] GetRandomPoints(double maxX, double minX, double maxY, double minY, double maxZ,
            double minZ, PolygonalFace olpPolygonalFaces, int numofpoints, int minlengthindex)
        {
            var planepara = Getplaneparameter(olpPolygonalFaces);
            var randompoints = new double[numofpoints, 3];
            for (var i = 0; i < numofpoints; i++)
            {
                if (maxX == minX)
                {
                    var y = GetRandomNumBetween(minY, maxY);
                    var z = GetRandomNumBetween(minZ, maxZ);
                    randompoints[i, 0] = maxX;
                    randompoints[i, 1] = y;
                    randompoints[i, 2] = z;
                }
                else if (maxY == minY)
                {
                    var x = GetRandomNumBetween(minX, maxX);
                    var z = GetRandomNumBetween(minZ, maxZ);
                    randompoints[i, 0] = x;
                    randompoints[i, 1] = maxY;
                    randompoints[i, 2] = z;
                }
                else if (maxZ == minZ)
                {
                    var x = GetRandomNumBetween(minX, maxX);
                    var y = GetRandomNumBetween(minY, maxY);
                    randompoints[i, 0] = x;
                    randompoints[i, 1] = y;
                    randompoints[i, 2] = maxZ;
                }
                else if (minlengthindex == 0)
                {

                    var y = GetRandomNumBetween(minY, maxY);
                    var z = GetRandomNumBetween(minZ, maxZ);
                    var x = -(planepara[2] * z + planepara[1] * y + planepara[3]) / planepara[0];
                    randompoints[i, 0] = x;
                    randompoints[i, 1] = y;
                    randompoints[i, 2] = z;
                }
                else if (minlengthindex == 1)
                {
                    var x = GetRandomNumBetween(minX, maxX);
                    var z = GetRandomNumBetween(minZ, maxZ);
                    var y = -(planepara[0] * x + planepara[2] * z + planepara[3]) / planepara[1];
                    randompoints[i, 0] = x;
                    randompoints[i, 1] = y;
                    randompoints[i, 2] = z;
                }
                else if (minlengthindex == 2)
                {
                    var x = GetRandomNumBetween(minX, maxX);
                    var y = GetRandomNumBetween(minY, maxY);
                    var z = -(planepara[0] * x + planepara[1] * y + planepara[3]) / planepara[2];
                    randompoints[i, 0] = x;
                    randompoints[i, 1] = y;
                    randompoints[i, 2] = z;
                }
            }
            return randompoints;
        }

        private static double[] GetPointsCenter(List<double[]> OLPpioints)
        {
            var x = 0.0;
            var y = 0.0;
            var z = 0.0;
            var n = OLPpioints.Count();
            foreach (var point in OLPpioints)
            {
                x += point[0];
                y += point[1];
                z += point[2];
            }
            return new double[] { x / n, y / n, z / n };
        }

        private static double[,] ProjectPointoFace(double[,] Spacerandompioints, PolygonalFace polygonalFace)
        {
            var planepara = Getplaneparameter(polygonalFace);
            var newpoiont = new double[Spacerandompioints.GetLength(0), 3];

            for (var i = 0; i < Spacerandompioints.GetLength(0); i++)
            {
                double t = (planepara[0] * Spacerandompioints[i, 0] + planepara[1] * Spacerandompioints[i, 1] + planepara[2] * Spacerandompioints[i, 2] + planepara[3]) /
                 (planepara[0] * planepara[0]
                 + planepara[1] * planepara[1] + planepara[2] * planepara[2]);

                for (var j = 0; j < 3; j++)
                {
                    newpoiont[i, j] = Spacerandompioints[i, j] - planepara[j] * t;
                }
            }
            return newpoiont;

        }


        private static double[] Getplaneparameter(TVGL.PolygonalFace face)
        {
            var p1 = face.Vertices[0];
            var p2 = face.Vertices[1];
            var p3 = face.Vertices[2];
            var a = ((p2.Y - p1.Y) * (p3.Z - p1.Z) - (p2.Z - p1.Z) * (p3.Y - p1.Y));
            var b = ((p2.Z - p1.Z) * (p3.X - p1.X) - (p2.X - p1.X) * (p3.Z - p1.Z));
            var c = ((p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X));
            var d = (0 - (a * p1.X + b * p1.Y + c * p1.Z));
            return new double[] { a, b, c, d };
        }
        private static double[] Getplaneparameter(List<double[]> threepoint)
        {
            var p1 = threepoint[0];
            var p2 = threepoint[1];
            var p3 = threepoint[2];
            var a = ((p2[1] - p1[1]) * (p3[2] - p1[2]) - (p2[2] - p1[2]) * (p3[1] - p1[1]));
            var b = ((p2[2] - p1[2]) * (p3[0] - p1[0]) - (p2[0] - p1[0]) * (p3[2] - p1[2]));
            var c = ((p2[0] - p1[0]) * (p3[1] - p1[1]) - (p2[1] - p1[1]) * (p3[0] - p1[0]));
            var d = (0 - (a * p1[0] + b * p1[1] + c * p1[2]));
            return new double[] { a, b, c, d };
        }

        private static bool Ispointwithinthreetrianglepoints(double[] randompoint, List<Vertex> oface)
        {

            double area1 = Trianglearea(randompoint, oface[0].Position, oface[1].Position);
            double area2 = Trianglearea(randompoint, oface[0].Position, oface[2].Position);
            double area3 = Trianglearea(randompoint, oface[2].Position, oface[1].Position);
            var truearea = Trianglearea(oface[0].Position, oface[1].Position, oface[2].Position);
            var totalarea = area1 + area2 + area3;
            if (Math.Abs(truearea - totalarea) < 0.01)
                return true;
            return false;
        }


        private static double GetRandomNumBetween(double range1, double range2)
        {

            if (range1 == range2)
            { return range1; }
            else
            {
                var obdata = Gen.Random.Numbers.Doubles(range1, range2)();

                return obdata;
            }

        }
        private static bool Istwofacesoverlapping(TVGL.PolygonalFace cface, TVGL.PolygonalFace oface)
        {
            foreach (var vert in cface.Vertices)
            {
                if (Ispointwithinthreetrianglepoints(vert, oface))
                {
                    return true;
                }
            }
            foreach (var vert in oface.Vertices)
            {
                if (Ispointwithinthreetrianglepoints(vert, cface))
                {
                    return true;
                }
            }
            return false;
        }
        private static bool Ispointwithinthreetrianglepoints(TVGL.Vertex vert, TVGL.PolygonalFace oface)
        {
            double area1 = Trianglearea(vert.Position, oface.Vertices[0].Position, oface.Vertices[1].Position);

            double area2 = Trianglearea(vert.Position, oface.Vertices[0].Position, oface.Vertices[2].Position);
            double area3 = Trianglearea(vert.Position, oface.Vertices[2].Position, oface.Vertices[1].Position);
            var truearea = Trianglearea(oface.Vertices[0].Position, oface.Vertices[1].Position, oface.Vertices[2].Position);
            var totalarea = area1 + area2 + area3;
            if (Math.Abs(truearea - totalarea) < 0.1)
                return true;
            return false;
        }

        private static bool Ispointwithinthreetrianglepoints(double[] testpoint, double[] A, double[] B, double[] C)
        {
            double area1 = Trianglearea(testpoint, A, B);
            double area2 = Trianglearea(testpoint, A, C);
            double area3 = Trianglearea(testpoint, C, B);
            var truearea = Trianglearea(A, B, C);
            var totalarea = area1 + area2 + area3;
            if (Math.Abs(truearea - totalarea) < 0.01)
                return true;
            return false;
        }

        private static double Trianglearea(double[] A, double[] B, double[] C)
        {
            var a = distancebetween(A, B);
            var b = distancebetween(C, B);
            var c = distancebetween(A, C);
            var s = (a + b + c) / 2;
            return Math.Sqrt(s * (s - a) * (s - b) * (s - c));
        }

        private static double distancebetween(double[] A, double[] B)
        {
            var d = Math.Sqrt((A[0] - B[0]) * (A[0] - B[0]) + (A[1] - B[1]) * (A[1] - B[1]) + (A[2] - B[2]) * (A[2] - B[2]));
            return d;
        }
        internal static double[,] RotaMatrix(double[] from, double[] to)
        {
            var I = new[,] { { 1.0, 0, 0 }, { 0, 1.0, 0 }, { 0, 0, 1.0 } };
            var cross = to.crossProduct(from);
            var vx = SkewSymmetricCrossProduct(cross[0], cross[1], cross[2]);
            var vx2 = SquareSkewSymmetricCrossProduct(cross[0], cross[1], cross[2]);
            var cosine = to.dotProduct(from);
            var sine = Math.Sqrt(cross[0] * cross[0] + cross[1] * cross[1] + cross[2] * cross[2]);
            var c = (1 - cosine) / ((double)Math.Pow(sine, 2));
            var vx2C = ConstantTimesMatrix(c, vx2);
            var rotMatr = AddMetrices(I, AddMetrices(vx, vx2C));
            //return new[,]
            //{
            //    {rotMatr[0, 0], rotMatr[0, 1], rotMatr[0, 2], 0.0},
            //    {rotMatr[1, 0], rotMatr[1, 1], rotMatr[1, 2], 0.0},
            //    {rotMatr[2, 0], rotMatr[2, 1], rotMatr[2, 2], 0.0},
            //    {0.0, 0.0, 0.0, 1.0}
            //};
            return new[,]
            {
                {rotMatr[0, 0], rotMatr[0, 1], rotMatr[0, 2]},
                {rotMatr[1, 0], rotMatr[1, 1], rotMatr[1, 2]},
                {rotMatr[2, 0], rotMatr[2, 1], rotMatr[2, 2]},              
            };
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
            foreach (var p1 in task.Install.Reference.CVXHull.Vertices)
            {
                foreach (var p2 in task.Install.Reference.CVXHull.Vertices.Where(a => a != p1))
                {
                    var xDif = p1.Position[0] - p2.Position[0];
                    var yDif = p1.Position[1] - p2.Position[1];
                    var zDif = p1.Position[2] - p2.Position[2];
                    if (Math.Sqrt((xDif * xDif) + (yDif * yDif) + (zDif * zDif)) > maxDist)
                        maxDist = (Math.Sqrt((xDif * xDif) + (yDif * yDif) + (zDif * zDif)));
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
            var dot = fFace.Normal.normalize().dotProduct(tFace.Normal.normalize());
            if (dot > 1) dot = 1;
            if (dot < -1) dot = -1;
            var angleInRad = Math.Acos(dot);
            var angleBetweenCurrentAndCandidate = angleInRad * (180 / Math.PI);

            // 1-(0.0075*|V-30|) in inch
            double horizontalDistance;
            if (vertDist < 10)
                // we can assume the width of the object = the smallest side
                horizontalDistance = 10 + partWidth / 2;
            else
                horizontalDistance = 8 + partWidth / 2;

            if (fFace.Adjacents == null ||
                fFace.Adjacents.Where(f => f.Name == tFace.Name).ToList().Count > 0)
            {
                liftingIndices.LI = 0; // There is no lifting cost
            }
            else
            {
                liftingIndices.HM = 10 / horizontalDistance;
                // after calculating the angle between current face and candidate face
                // which is s.th between 0 to 180.
                liftingIndices.RWL = liftingIndices.LC *
                                     liftingIndices.HM *
                                     liftingIndices.VM *
                                     liftingIndices.DM *
                                     liftingIndices.FM *
                                     liftingIndices.AM *
                                     liftingIndices.CM;
                liftingIndices.LI = task.Mass / liftingIndices.RWL;
            }
            rotatingIndex.VM = 1 - (0.0075 * Math.Abs(vertDist - 30));
            rotatingIndex.HM = 10 / horizontalDistance;
            rotatingIndex.CM = 1;
            rotatingIndex.RAM = 1 - (0.0044 * angleBetweenCurrentAndCandidate);
            rotatingIndex.RWL = rotatingIndex.LC * rotatingIndex.HM *
                                rotatingIndex.VM * rotatingIndex.RAM *
                                rotatingIndex.CM;

            rotatingIndex.RI = task.Mass / rotatingIndex.RWL;

            return liftingIndices.LI + rotatingIndex.RI;
        }

        private static double RiLiCostCalculatorForFinalRotation(SubAssembly task, FootprintFace fFace, FootprintFace tFace)
        {
            // the width of the part is still unknown
            // if the face is adjacent, there is no need to lift it.
            // Maximum Distance is the longest diagonal in the Ref CVH 
            var maxDist = 0.0;
            foreach (var p1 in task.CVXHull.Vertices)
            {
                foreach (var p2 in task.CVXHull.Vertices.Where(a => a != p1))
                {
                    var xDif = p1.Position[0] - p2.Position[0];
                    var yDif = p1.Position[1] - p2.Position[1];
                    var zDif = p1.Position[2] - p2.Position[2];
                    if (Math.Sqrt((xDif * xDif) + (yDif * yDif) + (zDif * zDif)) > maxDist)
                        maxDist = (Math.Sqrt((xDif * xDif) + (yDif * yDif) + (zDif * zDif)));
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
            var dot = fFace.Normal.normalize().dotProduct(tFace.Normal.normalize());
            if (dot > 1) dot = 1;
            if (dot < -1) dot = -1;
            var angleInRad = Math.Acos(dot);
            var angleBetweenCurrentAndCandidate = angleInRad * (180 / Math.PI);

            // 1-(0.0075*|V-30|) in inch
            double horizontalDistance;
            if (vertDist < 10)
                // we can assume the width of the object = the smallest side
                horizontalDistance = 10 + partWidth / 2;
            else
                horizontalDistance = 8 + partWidth / 2;

            if (fFace.Adjacents == null ||
                fFace.Adjacents.Where(f => f.Name == tFace.Name).ToList().Count > 0)
            {
                liftingIndices.LI = 0; // There is no lifting cost
            }
            else
            {
                liftingIndices.HM = 10 / horizontalDistance;
                // after calculating the angle between current face and candidate face
                // which is s.th between 0 to 180.
                liftingIndices.RWL = liftingIndices.LC *
                                     liftingIndices.HM *
                                     liftingIndices.VM *
                                     liftingIndices.DM *
                                     liftingIndices.FM *
                                     liftingIndices.AM *
                                     liftingIndices.CM;
                liftingIndices.LI = task.Mass / liftingIndices.RWL;
            }
            rotatingIndex.VM = 1 - (0.0075 * Math.Abs(vertDist - 30));
            rotatingIndex.HM = 10 / horizontalDistance;
            rotatingIndex.CM = 1;
            rotatingIndex.RAM = 1 - (0.0044 * angleBetweenCurrentAndCandidate);
            rotatingIndex.RWL = rotatingIndex.LC * rotatingIndex.HM *
                                rotatingIndex.VM * rotatingIndex.RAM *
                                rotatingIndex.CM;

            rotatingIndex.RI = task.Mass / rotatingIndex.RWL;

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

        internal static double[,] TransformationMatrix(double[] from, double[] to)
        {
            var I = new[,] { { 1.0, 0, 0 }, { 0, 1.0, 0 }, { 0, 0, 1.0 } };
            var cross = to.crossProduct(from);
            var vx = SkewSymmetricCrossProduct(cross[0], cross[1], cross[2]);
            var vx2 = SquareSkewSymmetricCrossProduct(cross[0], cross[1], cross[2]);
            var cosine = to.dotProduct(from);
            var sine = Math.Sqrt(cross[0] * cross[0] + cross[1] * cross[1] + cross[2] * cross[2]);
            var c = (1 - cosine) / ((double)Math.Pow(sine, 2));
            var vx2C = ConstantTimesMatrix(c, vx2);
            var rotMatr = AddMetrices(I, AddMetrices(vx, vx2C));
            return new[,]
            {
                {rotMatr[0, 0], rotMatr[0, 1], rotMatr[0, 2], 0.0},
                {rotMatr[1, 0], rotMatr[1, 1], rotMatr[1, 2], 0.0},
                {rotMatr[2, 0], rotMatr[2, 1], rotMatr[2, 2], 0.0},
                {0.0, 0.0, 0.0, 1.0}
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

        internal static double[,] MatrixTimesMatrix(double[,] m, double[,] n)
        {
            var multMatrix = new double[4, 4];
            for (var i = 0; i < 4; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    var e = 0.0;
                    for (var k = 0; k < 4; k++)
                    {
                        e += m[i, k] * n[k, j];
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
                    multiplied[i] += m[i, j] * n[j];
                }
            }
            return multiplied;
        }
    }
}
