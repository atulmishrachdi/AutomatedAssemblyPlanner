using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AssemblyEvaluation;
using GraphSynth.Representation;
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace GeometryReasoning
{
    public static class BlockingDetermination
    {
        internal static bool DefineBlocking(designGraph graphAssembly, Dictionary<string, ConvexHull<Vertex, DefaultConvexFace<Vertex>>> ConvexHullDictionary,
            Dictionary<string, double[]> boundingBoxDictionary)
        {
            if (graphAssembly.arcs.Count == 0)
            {
                //  Parallel.ForEach(graphAssembly.arcs, a =>
                for (int i = 0; i < graphAssembly.nodes.Count - 1; i++)
                    for (int j = i + 1; j < graphAssembly.nodes.Count; j++)
                    {
                        var from = graphAssembly.nodes[i];
                        var to = graphAssembly.nodes[j];
                        var fromName = from.name;
                        var toName = to.name;
                        if (BoundingBoxOverlap(boundingBoxDictionary[fromName], boundingBoxDictionary[toName]))
                        {
                            if (ConvexHullOverlap(ConvexHullDictionary[fromName], ConvexHullDictionary[toName]))
                            {       int flag = 10000;
                                DataInterface.CheckRelationOne(fromName, toName,ref flag);
                                if(flag == 1)
                                //if (DataInterface.CheckRelationOne(fromName, toName))
                                {
                                    graphAssembly.addArc(from, to);
                                    var a = graphAssembly.arcs.Last();
                                    AddArcInformation(a, graphAssembly, ConvexHullDictionary, boundingBoxDictionary);
                                }
                            }
                        }
                    }
            }
            else
                foreach (arc a in graphAssembly.arcs)
                {
                    AddArcInformation(a, graphAssembly, ConvexHullDictionary, boundingBoxDictionary);
                }
            Console.WriteLine("Number of Interfaces failing = " + DataInterface.failures + "; number succeeding = " + DataInterface.successes);
            var successrate = ((double)DataInterface.successes) / (DataInterface.failures + DataInterface.successes);
            Console.WriteLine("Success rate = " + (successrate * 100.0) + "%.");
            if (successrate < Constants.MinInterfaceSuccessRate)
            {
                Console.WriteLine("====> Because the success rate is so low, the process is terminated. Please see preceding ACIS load errors.");

                return false;
            }
            return true;
        }

        private static bool BoundingBoxOverlap(double[] part1, double[] part2)
        {
            /* each of these arrays is of length 6 and has the following variables:
             * { X_min, X_max, Y_min, Y_max, Z_min, Z_max } 
             * define this function to return false as soon as possible and continue checking if the
             * possibility of overlap exists. */
            return (!(part1[0] > part2[1] || part1[2] > part2[3] || part1[4] > part2[5]
                      || part2[0] > part1[1] || part2[2] > part1[3] || part2[4] > part1[5]));
        }

        private static bool ConvexHullOverlap(ConvexHull<Vertex, DefaultConvexFace<Vertex>> part1, ConvexHull<Vertex, DefaultConvexFace<Vertex>> part2)
        {
            /* the two convex hulls of two parts are passed as arguments.
             * check to see if there is overlap using the Separation axis theorem.
             * go through each face of part1 */
            foreach (var f in part1.Faces)
            {
                var n = f.Normal;
                var dStar = StarMath.dotProduct(n, f.Vertices[0].Position, 3);
                if (part2.Points.All(pt => (StarMath.dotProduct(n, pt.Position, 3)) > dStar))
                {
                    return false;
                }
            }
            foreach (var f in part2.Faces)
            {
                var n = f.Normal;
                var dStar = StarMath.dotProduct(n, f.Vertices[0].Position, 3);
                if (part1.Points.All(pt => (StarMath.dotProduct(n, pt.Position, 3)) > dStar))
                {
                    return false;
                }
            }
            return true;
        }


        private static void AddArcInformation(arc a, designGraph graphAssembly, Dictionary<string,
            ConvexHull<Vertex, DefaultConvexFace<Vertex>>> ConvexHullDictionary, Dictionary<string, double[]> boundingBoxDictionary)
        {
            Console.WriteLine("Detecting clash between:" + a.From.name + " & " + a.To.name);
            var di = DataInterface.MakeDataInterface(a.From.name, a.To.name);
            if (di == null) return;
            var connectType = (ConnectType)di.ClashClassType;
            #region Adding local labels.
            if (di.ClashClassType == 1 || di.ClashClassType == 2)
                a.localLabels.Add("tight_contact");
            //else a.localLabels.Add("strongly_connected");
            a.localLabels.Add(connectType.ToString());
            /***** Fastening Methood ****/
            // it seems that we had a variable, fasteningMethod, it was added at this point but
            // it's value was always "unknown".
            //FileStream << "        <string>" << fasteningMethod << "</string>"  << endl;

            //if (dat.Matrix == 1) a.localLabels.Add("rectilinear");
            //if (dat.Matrix == 2) a.localLabels.Add("radial");
            /*********** end of local labels ***********/
            #endregion
            #region Adding local variables.
            if (connectType == ConnectType.rectilinear ||
                connectType == ConnectType.radial ||
                connectType == ConnectType.strongly_connected || connectType == ConnectType.unknown_connection)
            {
                double clashX = 0.0, clashY = 0.0, clashZ = 0.0;
                for (int i = 0; i < di.visibleDOF.Count; i++)
                {
                    a.localVariables.Add(Constants.VISIBLE_DOF); // visible DOF
                    a.localVariables.Add(di.visibleDOF[i].x);
                    a.localVariables.Add(di.visibleDOF[i].y);
                    a.localVariables.Add(di.visibleDOF[i].z);
                    if (i >= di.visibleDOF_DBP.Count) di.visibleDOF_DBP.Add(null);
                    di.visibleDOF_DBP[i] = FindBlockingNodes(graphAssembly, graphAssembly.nodes.IndexOf(a.From),
                        graphAssembly.nodes.IndexOf(a.To), di.visibleDOF[i], ConvexHullDictionary, boundingBoxDictionary);

                    /* Evaluation: Compute Orientation & Translation Time for each visibleDOF
                    Evaluation CE(Part, PartBB, Mass, CofG, visibleDOF[i], clashBB); */
                    a.localVariables.Add(Constants.EVALUATION);
                    if (di.Evaluation_Times.Count > 0)
                    {
                        a.localVariables.Add(di.Evaluation_Times[i][0]); // orientation Time
                        a.localVariables.Add(di.Evaluation_Times[i][1]);  // insertion Time
                        a.localVariables.Add(di.Evaluation_Times[i][2]);     // handling Time
                    }
                    else
                    {
                        a.localVariables.Add(0);
                        a.localVariables.Add(0);
                        a.localVariables.Add(0);
                    }
                    if (di.clashLocation.Count > 0)
                    {
                        clashX = di.clashLocation[i][0];
                        clashY = di.clashLocation[i][1];
                        clashZ = di.clashLocation[i][2];
                    }

                }
                if (di.visibleDOF.Count == 0)
                { // for test only
                    //quick hack for estimating free directions when there are none based on centers of gravity
                    //a.localVariables.Add(Constants.VISIBLE_DOF); // visible DOF
                    //node nf = a.From;
                    //node nt = a.To;
                    //int findex = nf.localVariables.IndexOf(-7000);//index of position tag on from node
                    //int tindex = nt.localVariables.IndexOf(-7000);
                    //double xdiff = nt.localVariables[tindex + 1] - nf.localVariables[findex + 1];//difference between x centers of gravity               
                    //double ydiff = nt.localVariables[tindex + 2] - nf.localVariables[findex + 2];//difference between x centers of gravity 
                    //double zdiff = nt.localVariables[tindex + 3] - nf.localVariables[findex + 3];//difference between x centers of gravity 
                    //double magnitude = Math.Sqrt(xdiff * xdiff + ydiff * ydiff + zdiff * zdiff);

                    //a.localVariables.Add(xdiff / magnitude);
                    //a.localVariables.Add(ydiff / magnitude);
                    //a.localVariables.Add(zdiff / magnitude);
                    //a.localVariables.Add(Constants.EVALUATION);
                    //a.localVariables.Add(0);    // orientation Time
                    //a.localVariables.Add(0);    // insertion Time
                    //a.localVariables.Add(0);    // handling Time
                }
                for (int count = 0; count < di.invisibleDOF.Count; count++)
                {
                    var X = di.invisibleDOF[count].x;
                    var Y = di.invisibleDOF[count].y;
                    var Z = di.invisibleDOF[count].z;
                    if (X != 0 || Y != 0 || Z != 0)
                    {
                        double d = di.invisibleDistance[count];
                        a.localVariables.Add(Constants.INVISIBLE_DOF); // invisible DOF
                        a.localVariables.Add(X * d);
                        a.localVariables.Add(Y * d);
                        a.localVariables.Add(Z * d);

                    }
                }
                a.localVariables.Add(Constants.CLASH_LOCATION); // clash location
                a.localVariables.Add(clashX);
                a.localVariables.Add(clashY);
                a.localVariables.Add(clashZ); // NOT IMPLEMENTED

            }

            else if (connectType == ConnectType.loose_contact)
            {
                for (int i = 0; i < di.concentricDOF.Count; i++)
                {

                    a.localVariables.Add(Constants.CONCENTRIC_DOF); // concentric DOF
                    a.localVariables.Add(di.concentricDOF[i].x);
                    a.localVariables.Add(di.concentricDOF[i].y);
                    a.localVariables.Add(di.concentricDOF[i].z);
                    if (i >= di.concentricDOF_DBP.Count) di.concentricDOF_DBP.Add(null);
                    di.concentricDOF_DBP[i] = FindBlockingNodes(graphAssembly, graphAssembly.nodes.IndexOf(a.From),
                    graphAssembly.nodes.IndexOf(a.To), di.concentricDOF[i], ConvexHullDictionary, boundingBoxDictionary);
                    a.localVariables.Add(di.concentricDOF_DBP[i].Count);
                    for (int k = 0; k < di.concentricDOF_DBP[i].Count; k++)  // DBP
                        if (di.concentricDOF_DBP[i][k] > 0)
                            a.localVariables.Add(di.concentricDOF_DBP[i][k]);
                }
                if (di.concentricDOF.Count == 0) // for test only
                {
                    a.localVariables.Add(Constants.CONCENTRIC_DOF); // concentric DOF
                    node nf = a.From;
                    node nt = a.To;
                    int findex = nf.localVariables.IndexOf(-7000);//index of position tag on from node
                    int tindex = nt.localVariables.IndexOf(-7000);
                    double xdiff = nt.localVariables[tindex + 1] - nf.localVariables[findex + 1];//difference between x centers of gravity               
                    double ydiff = nt.localVariables[tindex + 2] - nf.localVariables[findex + 2];//difference between x centers of gravity 
                    double zdiff = nt.localVariables[tindex + 3] - nf.localVariables[findex + 3];//difference between x centers of gravity 
                    double magnitude = Math.Sqrt(xdiff * xdiff + ydiff * ydiff + zdiff * zdiff);

                    a.localVariables.Add(xdiff / magnitude);
                    a.localVariables.Add(ydiff / magnitude);
                    a.localVariables.Add(zdiff / magnitude);
                }
            }
            #endregion

        }

        private static List<int> FindBlockingNodes(designGraph graphAssembly, int nodeIndex, int matingIndex, DataInterface.DIRECTION dir,
            Dictionary<string, ConvexHull<Vertex, DefaultConvexFace<Vertex>>> ConvexHullDictionary, Dictionary<string, double[]> boundingBoxDictionary)
        {
            var v = new Vector(dir.x, dir.y, dir.z);
            var movingPartPoints = ConvexHullDictionary[graphAssembly.nodes[nodeIndex].name].Points;
            var movingPartBox = boundingBoxDictionary[graphAssembly.nodes[nodeIndex].name];
            var blockingIndices = new List<int>();
            //make rays
            var rays = new List<Ray>();
            foreach (var point in movingPartPoints)
                rays.Add(new Ray(point, v));
            Parallel.For(0, graphAssembly.nodes.Count, i =>
            {
                if (i == nodeIndex || i == matingIndex) return;
                var n = graphAssembly.nodes[i];
                if (!BoundingBoxBlocking(v, movingPartBox, boundingBoxDictionary[n.name])) return;
                var hull = ConvexHullDictionary[n.name];
                if (rays.Any(ray => hull.Faces.Any(f => STLGeometryFunctions.RayIntersectsWithFace(ray, f))))
                    lock (blockingIndices)
                        blockingIndices.Add(i);

            });
            var blockingIndicesString = "";
            foreach (var blockingIndex in blockingIndices)
            {
                blockingIndicesString += blockingIndex + " ";
            }
            Console.WriteLine("DBP from c#:" + blockingIndicesString);
            return blockingIndices;
        }

        private static bool BoundingBoxBlocking(Vector v, double[] movingPartBox, double[] blockingBox)
        {
            var facingCornerIndices = new int[3];
            lock (facingCornerIndices)
            {
                for (int i = 0; i < 3; i++)
                {
                    var signOfElement = Math.Sign(v.Position[i]);
                    if (signOfElement > 0 && movingPartBox[2 * i] > blockingBox[2 * i + 1])
                        return false;
                    if (signOfElement < 0 && movingPartBox[2 * i + 1] < blockingBox[2 * i])
                        return false;
                    if (signOfElement > 0) facingCornerIndices[i] = 1;
                }
                var complementaryCornerIndices = new[] { (1 - facingCornerIndices[0]), (1 - facingCornerIndices[1]), (1 - facingCornerIndices[2]) };

                var movingCompleCorner = new[]
                {
                    movingPartBox[complementaryCornerIndices[0]],
                    movingPartBox[complementaryCornerIndices[1] + 2],
                    movingPartBox[complementaryCornerIndices[2] + 4]
                };
                var blockingFacingCorner = new[]
                {
                    blockingBox[facingCornerIndices[0]],
                    blockingBox[facingCornerIndices[1] + 2],
                    blockingBox[facingCornerIndices[2] + 4]
                };

                var movingDxPlaneMin = StarMath.dotProduct(v.Position, movingCompleCorner, 3);
                var blockingDxPlaneMax = StarMath.dotProduct(v.Position, blockingFacingCorner, 3);
                if (movingDxPlaneMin > blockingDxPlaneMax) return false;
                var superficialBloackingFace = new DefaultConvexFace<Vertex>
                {
                    Vertices = new Vertex[6],
                    Normal = v.Position
                };
                var index = 0;
                for (int i = 0; i < 8; i++)
                {
                    var vertexIndices = binaryFaceIndices[i];
                    if (vertexIndices[0] == facingCornerIndices[0] && vertexIndices[1] == facingCornerIndices[1] &&
                        vertexIndices[2] == facingCornerIndices[2]) continue;
                    if (vertexIndices[0] == complementaryCornerIndices[0] &&
                        vertexIndices[1] == complementaryCornerIndices[1] &&
                        vertexIndices[2] == complementaryCornerIndices[2]) continue;
                    superficialBloackingFace.Vertices[index] = new Vertex(new[]
                    {
                        blockingBox[vertexIndices[0]],
                        blockingBox[vertexIndices[1] + 2],
                        blockingBox[vertexIndices[2] + 4]
                    });
                    index++;
                }

                for (int i = 0; i < 8; i++)
                {
                    var vertexIndices = binaryFaceIndices[i];
                    if (vertexIndices[0] == facingCornerIndices[0] && vertexIndices[1] == facingCornerIndices[1] &&
                        vertexIndices[2] == facingCornerIndices[2]) continue;
                    if (vertexIndices[0] == complementaryCornerIndices[0] &&
                        vertexIndices[1] == complementaryCornerIndices[1] &&
                        vertexIndices[2] == complementaryCornerIndices[2]) continue;
                    var ray = new Ray(new Vertex(new[]
                    {
                        movingPartBox[vertexIndices[0]],
                        movingPartBox[vertexIndices[1] + 2],
                        movingPartBox[vertexIndices[2] + 4]
                    }),
                        v);
                    if (STLGeometryFunctions.RayIntersectsWithFace(ray, superficialBloackingFace)) return true;
                }
                return false;
            }
        }


        static List<int[]> binaryFaceIndices = new List<int[]>
        {
           new []{0,0,0}, new []{0,0,1},new []{0,1,0},new []{0,1,1},
           new []{1,0,0}, new []{1,0,1},new []{1,1,0},new []{1,1,1},
        };
    }
}
