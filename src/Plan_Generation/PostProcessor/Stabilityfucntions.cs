using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assembly_Planner;
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

        /*public static double Getstabilityscore(node refnode, List<arc> refarcs, double[] tofaceNormal,
            out double[] mindir)
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
            foreach (var ind in Bridge.globalDirPool)
            {
                var dir = DisassemblyDirections.Directions[ind];
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
                    var dotvalue =
                        (ProjectToFace((point.subtract(COM)).normalize(), projectnormal)).dotProduct(
                            ProjectToFace(accdirection, projectnormal));
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
                    if (
                        pjaccvector.crossProduct(pjfulcrum.normalize())
                            .dotProduct(pjGvector.crossProduct(pjfulcrum.normalize())) < 0)
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
        }*/


        //here
        public static double Getstabilityscore(node refnode, List<Connection> refarcs, double[] tofaceNormal,
            out double[] mindir, out double[] selected)
        {
            if (refnode.name.StartsWith("PumpAssembly.15") || refnode.name.StartsWith("PumpAssembly.21"))
            {
                var ss1 = 1;
            }
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


            var newforcedirections = new List<double[]>();
            var newforcelocations = new List<double[]>();
            var newforcepoints = new List<List<double[]>>();
            GetnewforcedirectionAndlocation(checkarcs, refnode, out newforcedirections, out newforcelocations,
                out newforcepoints);

            var upforcepionts = new List<List<double[]>>();
            for (int i = 0; i < newforcedirections.Count; i++)
            {
                if (newforcedirections[i].dotProduct(tofaceNormal) < 0)
                {
                    upforcepionts.Add(newforcepoints[i]);
                }
            }

            /// if no support force. part will fall. need more work.

            if (upforcepionts.Count == 0)
            {
                mindir = tofaceNormal;
                selected = tofaceNormal;
                return 0; //need check
            }
            /// if no support force. part will fall. need more work.

            var allupforceVertices = new List<Vertex>();
            foreach (var listpoints in upforcepionts)
            {
                foreach (var point in listpoints)
                {
                    var ss = new Vertex(point);
                    allupforceVertices.Add(new Vertex(point));
                }
            }
            var allupforcePoints = MiscFunctions.Get2DProjectionPoints(allupforceVertices, tofaceNormal);
            var nodecomponent = (Component)refnode;
            var forceCVH = MinimumEnclosure.ConvexHull2D(allupforcePoints);
            var allWithCOM = MiscFunctions.Get2DProjectionPoints(new List<Vertex> { new Vertex(nodecomponent.CenterOfMass) }, tofaceNormal).ToList();
            allWithCOM.AddRange(allupforcePoints);
            var forceandCMCVH = MinimumEnclosure.ConvexHull2D(allWithCOM);

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

            if (comlist1.Count != comlist2.Count)
            {
                mindir = tofaceNormal;
                selected = tofaceNormal;
                return 0;
            }
            else
            {
                for (int i = 0; i < comlist1.Count; i++)
                {
                    if (Math.Abs(comlist1[i][0] - comlist2[i][0]) > 0.0001 ||
                         Math.Abs(comlist1[i][1] - comlist2[i][1]) > 0.0001)
                    {
                        mindir = tofaceNormal;
                        selected = tofaceNormal;
                        return 0; ////need check
                    }
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
            foreach (var ind in Program.globalDirPool)
            {
                var dir = DisassemblyDirections.Directions[ind];
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
                    var dotvalue =
                        (ProjectToFace((point.subtract(COM)).normalize(), projectnormal)).dotProduct(
                            ProjectToFace(accdirection, projectnormal));
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
                    if (
                        pjaccvector.crossProduct(pjfulcrum.normalize())
                            .dotProduct(pjGvector.crossProduct(pjfulcrum.normalize())) < 0)
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

        private static Dictionary<string, List<int>> GetSubPartRemovealDirectionIndexs(node checknode, List<arc> refarcs,
            bool s)
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
                        var b = DisassemblyDirections.Directions[DisassemblyDirections.DirectionsAndOppositsForGlobalpool[dir]];
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

        public static List<int> GetSubPartRemovealDirectionIndexs(node checknode, List<Connection> refarcs)
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
                        var b = DisassemblyDirections.Directions[DisassemblyDirections.DirectionsAndOppositsForGlobalpool[dir]];
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


        public static double[] GetDOF(Component checknode, List<Connection> checkarcs)
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
            var linearDOF = new double[] { 0, 0, 0, 0, 0, 0 }; //x -x y -y z -z // 1 can move
            GetnewforcedirectionAndlocation(checkarcs, checknode, out newforcedirections, out newforcelocations,
                out newforcepoints);
            // if (newforcedirections.Count > 1)
            var currentremovaldirindexs = GetSubPartRemovealDirectionIndexs(checknode, checkarcs);
            //  var currentremovaldirindexs = Getcurrentremovaldirection(checknode, checkarcs);
            //   if (newforcedirections.Count > 1)
            var alldirs = new List<double[]>();
            foreach (var dirindex in currentremovaldirindexs)
            {
                alldirs.Add(DisassemblyDirections.Directions[dirindex]);
            }
            var dirs = new List<double[]>();
            var facenormals = new List<double[]>();
            //wait for nima to fix bug
            foreach (var f in alldirs)
            {
                if (newforcedirections.Any(d => d.dotProduct(f) < -0.001))
                {
                    continue;
                }
                else
                {
                    dirs.Add(f);
                }
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
            else if (dirs.Count == 2)
            {
                if (dirs[0].dotProduct(dirs[1]) < -0.98)
                {
                    linearDOF[0] = 0.5;
                    linearDOF[1] = 0.5;
                }
            }

            var c = (Connection)checkarcs.First(a => a is Connection);
            if (dirs.Count > 2 && c.ToPartReactionForeceDirections.Count > 0 && c.UnionAreaPointsCenter.Count != 0)
            // generate coordinate regardless of the footprint
            {
                // var c = (Connection)checkarcs.First(a => a is Connection);
                var d0 = c.ToPartReactionForeceDirections[0];
                var d1 = new double[3];
                var allreactionidirs = new HashSet<double[]> { };
                foreach (var carc in checkarcs)
                {
                    for (int j = 0; j < carc.FromPartReactionForeceDirections.Count; j++)
                    {
                        allreactionidirs.Add(carc.FromPartReactionForeceDirections[j]);
                    }
                    for (int k = 0; k < carc.ToPartReactionForeceDirections.Count; k++)
                    {
                        allreactionidirs.Add(carc.ToPartReactionForeceDirections[k]);
                    }
                }

                if (allreactionidirs.Any(td => Math.Abs(td.dotProduct(d0)) < 0.0001))
                {
                    d1 = allreactionidirs.First(td => Math.Abs(td.dotProduct(d0)) < 0.0001);
                }
                else
                {
                    d1 = c.UnionAreaPoints[0][0].subtract(c.UnionAreaPointsCenter[0]).normalize();
                }
        
                if (allreactionidirs.Any(td => Math.Abs(td.dotProduct(d0)) < 0.0001))
                {
                    d1 = allreactionidirs.First(td => Math.Abs(td.dotProduct(d0)) < 0.0001);
                }
                else
                {
                    d1 = c.UnionAreaPoints[0][0].subtract(c.UnionAreaPointsCenter[0]).normalize();
                }
                

                //if (c.ToPartReactionForeceDirections.First(v => v.dotProduct(d0).Equals(0))!=null)
                //{
                //    d1 = c.ToPartReactionForeceDirections.First(v => v.dotProduct(d0).Equals(0));
                //}
                //allforcevector = new List<double[]> { dirs[0], dirs[1], new double[3] { 0.0, 0.0, 0.0 } }; //not always perpendicular or paralla to groud;

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
                        cnaxesanchor.Add(cone.Axis); //TBD
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
                                refcross =
                                    ProjectToFace(point, cylindars[0].Axis)
                                        .subtract(pjanchor)
                                        .normalize()
                                        .crossProduct(pjdir.normalize())
                                        .normalize();
                                saverefcross = true;
                                continue;
                            }
                            currencross =
                                ProjectToFace(point, cylindars[0].Axis)
                                    .subtract(pjanchor)
                                    .normalize()
                                    .crossProduct(pjdir.normalize())
                                    .normalize();
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
                                refcross =
                                    ProjectToFace(point, cones[0].Axis)
                                        .subtract(pjanchor)
                                        .normalize()
                                        .crossProduct(pjdir.normalize())
                                        .normalize();
                                saverefcross = true;
                                continue;
                            }
                            currencross =
                                ProjectToFace(point, cones[0].Axis)
                                    .subtract(pjanchor)
                                    .normalize()
                                    .crossProduct(pjdir.normalize())
                                    .normalize();
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

            // new no cylindar rotateion 



            ////no cylindar rotateion
            //var removalfacecounter = 0;
            //var foundpara = false;
            //var rotateflatpara = new double[4];
            //var candir = new List<double[]>();

            //foreach (var dir in dirs)
            //{
            //    var product = new List<double>();
            //    for (int i = 0; i < dirs.Count; i++)
            //    {
            //        var ss = dir.dotProduct(dirs[i]);
            //        if (ss < -0.9)
            //        {
            //            var qwer = 1;
            //        }
            //    }
            //    if (dirs.Any(d => Math.Round(d.dotProduct(dir), 8) == -1))
            //    {
            //        candir.Add(dir);
            //    }
            //}
            ////temp comment
            //if (candir.Count >= 3)
            //{
            //    for (int i = 0; i < candir.Count - 2; i++)
            //    {
            //        if (foundpara == true)
            //            break;
            //        for (int j = i + 1; j < candir.Count - 1; j++)
            //        {
            //            if (foundpara == true)
            //                break;
            //            if (Math.Round(candir[i].dotProduct(candir[j]), 8) == -1)
            //                continue;
            //            for (int k = j + 1; k < candir.Count; k++)
            //            {
            //                if (foundpara == true)
            //                    break;
            //                rotateflatpara =
            //                    Getplaneparameter(new List<double[]> { candir[i], candir[j], new double[] { 0, 0, 0 } });
            //                var ff = rotateflatpara[0] * candir[k][0] + rotateflatpara[1] * candir[k][1] +
            //                         rotateflatpara[2] * candir[k][2] + rotateflatpara[3];
            //                if (ff == 0)
            //                {
            //                    foundpara = true;
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //}


            //if (foundpara == true)
            //{
            //    if (linearDOF[0] == 0 || linearDOF[1] == 0)
            //    {
            //        rotateDOF[0] = 0.5;
            //        rotateDOF[1] = 0.5;
            //    }
            //    else if (linearDOF[2] == 0 || linearDOF[3] == 0)
            //    {
            //        rotateDOF[2] = 0.5;
            //        rotateDOF[3] = 0.5;
            //    }

            //    else if (linearDOF[4] == 0 || linearDOF[5] == 0)
            //    {
            //        rotateDOF[4] = 0.5;
            //        rotateDOF[5] = 0.5;
            //    }
            //    else
            //    {
            //        rotateDOF[0] = 0.5;
            //        rotateDOF[1] = 0.5;
            //    }
            //}
            var rotateXaxis = linearDOF[0] + linearDOF[1];
            var rotateYaxis = linearDOF[2] + linearDOF[3];
            var rotateZaxis = linearDOF[4] + linearDOF[5];
            if (rotateXaxis == 1 && rotateYaxis == 1)
            {
                rotateDOF[4] = 0.5;
                rotateDOF[5] = 0.5;
            }

            if (rotateXaxis == 1 && rotateZaxis == 1)
            {
                rotateDOF[2] = 0.5;
                rotateDOF[3] = 0.5;
            }

            if (rotateZaxis == 1 && rotateYaxis == 1)
            {
                rotateDOF[0] = 0.5;
                rotateDOF[1] = 0.5;
            }
            //no cylindar rotateion
            //Console.WriteLine("linearDOF");
            //Console.WriteLine("X, -X, Y, -Y, Z, -Z");
            //Console.WriteLine("X:{0}, -X:{1}, Y:{2}, -Y:{3}, Z:{4}, -Z:{5}", linearDOF[0], linearDOF[1], linearDOF[2],
            //    linearDOF[3], linearDOF[4], linearDOF[5]);
            //Console.WriteLine("rotateDOF");

            //Console.WriteLine("X:{0}, -X:{1}, Y:{2}, -Y:{3}, Z:{4}, -Z:{5}", rotateDOF[0], rotateDOF[1], rotateDOF[2],
            //    rotateDOF[3], rotateDOF[4], rotateDOF[5]);
            var alldof = new double[12];
            for (int i = 0; i < 6; i++)
            {
                alldof[i] = linearDOF[i];
                alldof[i + 6] = rotateDOF[i];
            }
            return alldof; //need check
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

        private static void GetnewforcedirectionAndlocation(List<Connection> checkarcs, node checknode,
            out List<double[]> newforcedirections, out List<double[]> newforcelocations,
            out List<List<double[]>> newforcepoints)
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

        public static void GetnewforcedirectionAndlocation(List<arc> checkarcs, node checknode,
            out List<double[]> newforcedirections, out List<double[]> newforcelocations)
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
                n.RemovealDirectionsforEachPart = GetSubPartRemovealDirectionIndexs(n,
                    AssemblyGraph.arcs.FindAll(a => a.XmlTo.Equals(n.name) || a.XmlFrom.Equals(n.name)), true);
            }
            var counter = 0;
            foreach (Connection arc in AssemblyGraph.arcs.Where(a => a is Connection))
            {
                var topartname = arc.To.name;
                var frompartname = arc.From.name;
                var a222 =
                    BlockingDetermination.OverlappingSurfaces.Where(
                        s =>
                            (s.Solid1.Name == topartname && s.Solid2.Name == frompartname) ||
                            (s.Solid1.Name == frompartname && s.Solid2.Name == topartname)).ToList();
                if (!a222.Any()) continue;
                var a22 = a222[0];
                int checkindex;

                if (topartname == a22.Solid1.Name)
                {
                    checkindex = 0;
                }
                else
                {
                    checkindex = 1;
                }
                int otherindex = 1 - checkindex;
                var OLPS = a22.Overlappings;
                var checkredum = new List<PrimitiveSurface[]>();
                foreach (var pairsurfaces in OLPS)
                {
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

                    var cxh1lines = new Dictionary<int, List<double[]>>();
                    var cxh2lines = new Dictionary<int, List<double[]>>();
                    var key1 = 0;
                    var key2 = 0;
                    var allints = new List<double[]>();
                    for (int i = 0; i < 2; i++)
                    {
                        foreach (var face in pairsurfaces[i].Faces)
                        {
                            var pointinotherface = pairsurfaces[1 - i].Vertices;
                            foreach (var v in pointinotherface)
                            {
                                if (Ispointwithinthreetrianglepoints(v.Position, face.Vertices))
                                {
                                    allints.Add(v.Position);
                                }
                            }
                            foreach (var edge in face.Edges)
                            {
                                if (i == 0)
                                {
                                    if (
                                        !cxh1lines.Values.Any(
                                            v => v[0].Equals(edge.To.Position) && v[1].Equals(edge.From.Position)
                                                 || v[1].Equals(edge.To.Position) && v[0].Equals(edge.From.Position)
                                            ))
                                    {
                                        cxh1lines.Add(key1, new List<double[]> { edge.From.Position, edge.To.Position });
                                        key1++;
                                    }
                                }
                                else
                                {
                                    if (
                                        !cxh2lines.Values.Any(
                                            v => v[0].Equals(edge.To.Position) && v[1].Equals(edge.From.Position)
                                                 || v[1].Equals(edge.To.Position) && v[0].Equals(edge.From.Position)
                                            ))
                                    {
                                        cxh2lines.Add(key2,
                                            new List<double[]> { edge.From.Position, edge.To.Position });
                                        key2++;
                                    }
                                }
                            }
                        }
                    }
                    for (int i = 0; i < cxh1lines.Count; i++)
                    {
                        for (int j = 0; j < cxh2lines.Count; j++)
                        {
                            var l1 = cxh1lines[i];
                            var l2 = cxh2lines[j];
                            //line 1 pionts 1 2
                            var x1 = l1[0][0];
                            var x2 = l1[1][0];
                            var y1 = l1[0][1];
                            var y2 = l1[1][1];
                            var z1 = l1[0][2];
                            var z2 = l1[1][2];
                            //line 1 pionts 3 4
                            var x3 = l2[0][0];
                            var x4 = l2[1][0];
                            var y3 = l2[0][1];
                            var y4 = l2[1][1];
                            var z3 = l2[0][2];
                            var z4 = l2[1][2];
                            ///calculation
                            var a1 = x2 - x1;
                            var b1 = y2 - y1;
                            var c1 = z2 - z1;
                            var a2 = x3 - x1;
                            var b2 = y3 - y1;
                            var c2 = z3 - z1;
                            var a3 = x4 - x1;
                            var b3 = y4 - y1;
                            var c3 = z4 - z1;
                            //special case perfectly olp or partialy olp
                            if ((a1 * b2 == b1 * a2 && a1 * b3 == b1 * a3) && (a1 * b3 == b1 * a3 && a1 * c3 == c1 * a3))
                            {
                                //perfectly olp
                                //if ((l1[0] == l2[0] && l1[1] == l2[1]) || (l1[0] == l2[1] && l1[1] == l2[0]))
                                //{
                                //    allints.Add(l1[0]);
                                //    allints.Add(l1[1]);
                                //}
                                ////one totelly within other
                                if (Math.Abs((l1[0].subtract(l2[0]).norm1() + l1[0].subtract(l2[1]).norm1()) -
                                             (l1[1].subtract(l2[0]).norm1() + l1[1].subtract(l2[1]).norm1())) < 0.0001)
                                {
                                    if ((l1[0].subtract(l1[1]).norm1() > l2[0].subtract(l2[1]).norm1()))
                                    {
                                        allints.Add(l1[0]);
                                        allints.Add(l1[1]);
                                    }
                                    else
                                    {
                                        allints.Add(l2[0]);
                                        allints.Add(l2[1]);
                                    }
                                }
                                //partially olp
                                else
                                {
                                    var maxlength = l1[0].subtract(l1[1]).norm1() + l2[0].subtract(l2[1]).norm1();
                                    var withinmax = 0.0;
                                    bool partialolp = false;
                                    int marker1 = 0;
                                    int marker2 = 0;
                                    for (int k = 0; k < 2; k++)
                                    {
                                        for (int l = 0; l < 2; l++)
                                        {
                                            var newl = l1[k].subtract(l2[l]).norm1();
                                            if (newl > withinmax)
                                            {
                                                withinmax = newl;
                                                marker1 = k;
                                                marker2 = l;
                                            }
                                        }
                                    }
                                    if (withinmax < maxlength)
                                    {
                                        allints.Add(l1[1 - marker1]);
                                        allints.Add(l2[1 - marker2]);
                                    }
                                }
                            }
                            else
                            {
                                //if(AB,AC,AD)!=0, four points are not in the same plane.
                                var yimian = Math.Abs((a1 * b2 * c3 + a2 * b3 * c1 + a3 * b1 * c2 - a3 * b2 * c1 - a1 * b3 * c2 - a2 * b1 * c3));
                                if (Math.Abs((a1 * b2 * c3 + a2 * b3 * c1 + a3 * b1 * c2 - a3 * b2 * c1 - a1 * b3 * c2 - a2 * b1 * c3)) > 0.1)
                                {
                                    var s = "yimian";
                                }
                                else
                                {
                                    //parallel
                                    var qwe = Math.Abs((a3 - a2) * b1 - (b3 - b2) * a1);
                                    var ff = Math.Abs((a3 - a2) * c1 - (c3 - c2) * a1);
                                    if (Math.Abs((a3 - a2) * b1 - (b3 - b2) * a1) < 0.001
                                        && Math.Abs((a3 - a2) * c1 - (c3 - c2) * a1) < 0.001)
                                    {
                                        var s = "pingxing";
                                    }
                                    else
                                    {
                                        var itx = (a1 * a3 * b2 - a1 * a2 * b3) / (b1 * a3 + b2 * a1 - b3 * a1 - a2 * b1) + x1;
                                        var ity = (b1 * a3 * b2 - b1 * a2 * b3) / (b1 * a3 + b2 * a1 - b3 * a1 - a2 * b1) + y1;
                                        var itz = (c1 * a3 * b2 - c1 * a2 * b3) / (b1 * a3 + b2 * a1 - b3 * a1 - a2 * b1) + z1;
                                        var interdot = new double[] { itx, ity, itz };
                                        if (
                                            Math.Abs(interdot.subtract(l1[0]).norm1() + interdot.subtract(l1[1]).norm1() -
                                                     l1[0].subtract(l1[1]).norm1()) < 0.0001
                                            &&
                                            Math.Abs(interdot.subtract(l2[0]).norm1() + interdot.subtract(l2[1]).norm1() -
                                                     l2[0].subtract(l2[1]).norm1()) < 0.0001
                                            )
                                        {
                                            allints.Add(new double[] { itx, ity, itz });
                                        }
                                        //    allints.Add(new double[] { itx, ity, itz });
                                    }
                                }
                            }
                        }
                    }
                    if (allints.Count < 3)
                        continue;
                    var listver = new List<Vertex>();
                    var addictionpoint = GetPointsCenter(allints); // for CVH 
                    var dir = pairsurfaces[0].Faces[0].Normal;
                    var dis = MaximumDistanceBetweenPoints(allints);
                    var adddisx = dis * dir.dotProduct(new List<double> { 1, 0, 0 });
                    var adddisy = dis * dir.dotProduct(new List<double> { 0, 1, 0 });
                    var adddisz = dis * dir.dotProduct(new List<double> { 0, 0, 1 });
                    addictionpoint[0] += addictionpoint[0] + adddisx;
                    addictionpoint[1] += addictionpoint[1] + adddisy;
                    addictionpoint[2] += addictionpoint[2] + adddisz;

                    //addictionpoint[0] += addictionpoint[0] * 0.1;
                    //addictionpoint[1] += addictionpoint[1] * 0.2;
                    //addictionpoint[2] += addictionpoint[2] * 0.3;
                    allints.Add(addictionpoint);

                    foreach (var dot in allints)
                    {
                        listver.Add(new Vertex(dot));
                    }
                    var uniquelistver = new List<Vertex>();
                    foreach (var v in listver)
                    {
                        if (
                            !uniquelistver.Any(
                                uv =>
                                    Math.Abs(uv.X - v.X) < 0.0001 && Math.Abs(uv.Y - v.Y) < 0.0001 &&
                                    Math.Abs(uv.Z - v.Z) < 0.0001))
                        {
                            uniquelistver.Add(v);
                        }
                    }
                    if (uniquelistver.Count <= 3)
                    {

                        uniquelistver.Add(new Vertex(uniquelistver[0].Position.multiply(1.1)));
                        uniquelistver.Add(new Vertex(uniquelistver[0].Position.multiply(1.2)));
                        uniquelistver.Add(new Vertex(uniquelistver[0].Position.multiply(1.3)));
                    }
                    List<Vertex> newConvexHullVerts = new List<Vertex>();
                    try
                    {
                        var newconvexull = new TVGLConvexHull(uniquelistver, 1e-8);
                        newConvexHullVerts = newconvexull.Vertices.ToList();
                        if (newConvexHullVerts == null)
                        {
                            continue;
                        }
                        //  newconvexull.Faces[0].color = new Color();

                        var listpoints = new List<double[]>();
                        foreach (var v in newConvexHullVerts)
                        {
                            listpoints.Add(v.Position);
                        }
                        var removeindex = Gettheindexofaddictionalpoint(listpoints, dir,
                            pairsurfaces[0].Faces[0].Vertices[0].Position);
                        listpoints.RemoveAt(removeindex);
                        arctoadd.UnionAreaPoints.Add(listpoints);
                        arctoadd.UnionAreaPointsCenter.Add(GetPointsCenter(listpoints));
                        arctoadd.ToPartReactionForeceDirections.Add(pairsurfaces[otherindex].Faces[0].Normal);
                        arctoadd.FromPartReactionForeceDirections.Add(pairsurfaces[checkindex].Faces[0].Normal);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                }
            }
        }
        private static double MaximumDistanceBetweenPoints(List<double[]> allints)
        {
            double maxdis = double.NegativeInfinity;
            for (int i = 0; i < allints.Count; i++)
            {
                for (int j = 0; j < allints.Count; j++)
                {
                    var dis = Math.Sqrt(Math.Pow(allints[i][0] - allints[j][0], 2) + Math.Pow(allints[i][1] - allints[j][1], 2) +
                              Math.Pow(allints[i][2] - allints[j][0], 2));
                    if (dis > maxdis)
                    {
                        maxdis = dis;
                    }
                }
            }
            return maxdis;
        }
        private static int Gettheindexofaddictionalpoint(List<double[]> listpoints, double[] dir, double[] refpoint)
        {
            int index = 0;
            for (int i = 0; i < listpoints.Count; i++)

            //foreach (var checkpoint in listpoints)
            {
                var checkpoint = listpoints[i];
                if (Math.Abs(checkpoint[0] - refpoint[0]) / refpoint[0] < 0.05 && Math.Abs(checkpoint[1] - refpoint[1]) / refpoint[1] < 0.05 &&
                                Math.Abs(checkpoint[2] - refpoint[2]) / refpoint[2] < 0.05)
                {
                    continue;
                }
                //if (Math.Abs(checkpoint[0] - refpoint[0])/ refpoint[0] < 0.1 && Math.Abs(checkpoint[1] - refpoint[1]) / refpoint[1] < 0.1 &&
                //                 Math.Abs(checkpoint[2] - refpoint[2]) / refpoint[2] < 0.1)
                //{
                //    index++;
                //}
                else
                {
                    var newvector = StarMath.normalize(new double[]
                    {checkpoint[0] - refpoint[0], checkpoint[1] - refpoint[1], checkpoint[2] - refpoint[2]});
                    //   var dot = newvector.dotProduct(dir);
                    if (Math.Abs(newvector.dotProduct(dir)) < 0.1)
                    {
                        continue;
                    }
                    else
                    {
                        index = i;
                        break;
                    }
                }

            }
            if (index == listpoints.Count) index--;
            return index;
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
                double t = (planepara[0] * Spacerandompioints[i, 0] + planepara[1] * Spacerandompioints[i, 1] +
                            planepara[2] * Spacerandompioints[i, 2] + planepara[3]) /
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
            {
                return range1;
            }
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
            var truearea = Trianglearea(oface.Vertices[0].Position, oface.Vertices[1].Position,
                oface.Vertices[2].Position);
            var totalarea = area1 + area2 + area3;
            var olparea = Math.Abs(truearea - totalarea);
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
    }
}