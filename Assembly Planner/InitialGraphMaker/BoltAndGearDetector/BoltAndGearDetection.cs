using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using StarMathLib;
using TVGL;
using TVGL.Tessellation;

namespace Assembly_Planner
{
    class BoltAndGearDetection
    {
        internal static List<TessellatedSolid> ScrewAndBoltDetector(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive)
        {
            var bolts = new List<TessellatedSolid>();
            foreach (var solid in solidPrimitive.Keys)
                if (solid.Name.Contains("Screw")) 
                    bolts.Add(solid);
            return bolts;
            // Here are my thoughts about a bolt:
            // Since all of the threads are classified as cone, 
            //    if the number of cones are more than 30 percent of the total number of primitives
            //    AND, the summation of area of cone primitivies are more than 30 percent of the solid surface area
            foreach (var solid in solidPrimitive.Keys)
            {
                var cones = solidPrimitive[solid].Where(p => p is Cone).ToList();
                if (cones.Count < BoltAndGearConstants.ConePortion * solidPrimitive[solid].Count ||
                    cones.Sum(p => p.Area) < BoltAndGearConstants.ConeAreaPortion * solid.SurfaceArea)
                    continue;
                Console.WriteLine("Is " + solid.Name + " a Bolt or Screw? 'y' or 'n'");
                var read = Convert.ToString(Console.ReadLine());
                if (read == "y")
                {
                    bolts.Add(solid);
                }
            }
            return bolts;
        }

        private static double[] BoltCenterLine(List<PrimitiveSurface> primitiveSurfaces)
        {
            // the center line of the screw CAN be the axis of the largest cylinder. I have looked at several test cases
            // and this rule works for almost all of them. 
            var maxCountFace = 0;
            var finalCenterline = new double[3];
            foreach (Cylinder cylinder in primitiveSurfaces.Where(prim => prim is Cylinder))
            {
                if (cylinder.Faces.Count > maxCountFace)
                    finalCenterline = cylinder.Axis;
            }
            return finalCenterline;
        }

        internal static Dictionary<TessellatedSolid, double[]> GearDetector(Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive)
        {
            var gears = new Dictionary<TessellatedSolid, double[]>();
            foreach (var solid in solidPrimitive.Keys)
            {
                var gear = false;
                var flats =
                    solidPrimitive[solid].Where(
                        p => p is Flat && p.Faces.Count > BoltAndGearConstants.TriabglesInTheGearSideFaces).ToList();
                
                foreach (var flatPrim in flats)
                {
                    var outerGearEdges = GearEdge.FromTVGLEdgeClassToGearEdgeClass(flatPrim.OuterEdges);
                    var patches = SortedConnectedPatches(outerGearEdges);
                    foreach (var patch in patches)
                    {
                        var cluster = new List<GearEdge>[2];
                        var newPatch = new List<GearEdge>();
                        if (ClusteringDenseAndSparseEdges.ContainsDense(patch))
                            cluster = ClusteringDenseAndSparseEdges.ClusteringDenseSparse(patch);
                        if (cluster[0] != null && cluster[1].Count > BoltAndGearConstants.AcceptableNumberOfDenseEdges)
                            newPatch = ClusteringDenseAndSparseEdges.ReplacingDenseEdges(patch, cluster);
                        var crossP = new List<double[]>();
                        if (newPatch.Count == 0) newPatch = patch;
                        for (var i = 0; i < newPatch.Count - 1; i++)
                        {
                            var cross = new[] { 0.0, 0, 0 };
                            var vec1 = newPatch[i].Vector.normalize();
                            var vec2 = newPatch[i + 1].Vector.normalize();
                            if (SmoothAngle(vec1, vec2))
                                continue;
                            cross = vec1.crossProduct(vec2);
                            crossP.Add(cross);
                        }
                        if (crossP.Count < 10) continue;
                        var crossSign = BoltAndGearUpdateFunctions.ConvertCrossToSign(crossP);
                        if (!IsGear(crossSign)) continue;
                        Console.WriteLine("Is " + solid.Name + " a gear? 'y' or 'n'");
                        var read = Convert.ToString(Console.ReadLine());
                        if (read == "n")
                            continue;
                        gear = true;
                        gears.Add(solid, flatPrim.Faces[0].Normal);
                        break;
                    }
                    if (gear) break;
                }
            }
            return gears;
        }

        private static bool IsGear(List<int> crossSign)
        {
            crossSign = BoltAndGearUpdateFunctions.CrossUpdate(crossSign);
            var isGear = true;
            var counter = 0;
            var startInd = 0;
            for (var i = 0; i < crossSign.Count;i++)
            {
                if (crossSign[i] == crossSign[i + 1])
                {
                    startInd = i;
                    break;
                }
            }
            for (var i = startInd; i < crossSign.Count - 5; i += 2)
            {
                if (crossSign[i] == crossSign[i + 1] && crossSign[i] != crossSign[i + 2])
                {
                    counter++;
                    continue;
                }
                isGear = false;
                break;
            }
            if (isGear && counter > BoltAndGearConstants.GearTeeth)
                return true;
            return false;
        }

        private static List<List<GearEdge>> SortedConnectedPatches(List<GearEdge> outerEdges)
        {
            var outer = new List<GearEdge>(outerEdges);
            var patches = new List<List<GearEdge>>();
            var c = -1;

            while (outer.Count > 0)
            {
                c++;
                patches.Add(new List<GearEdge>());
                var ind = outerEdges.IndexOf(outer[0]);
                patches[c].Add(outerEdges[ind]);
                outer.Remove(outer[0]);
                var count1 = patches[c].Count;
                var count2 = 0;
                while (count1 != count2)
                {
                    count1 = patches[c].Count;
                    var ed1 = patches[c][patches[c].Count - 1];
                    for (var j = 0; j < outer.Count; j++)
                    {
                        var ed2 = outer[j];
                        //if (patches[c].Contains(ed2)) continue;
                        if (ed1.To != ed2.From && ed1.To != ed2.To)
                            continue;
                        var copy = outerEdges.Where(e => e == ed2).ToList()[0];
                        patches[c].Add(copy);
                        count2 = patches[c].Count;
                        outer.Remove(ed2);
                        var last = patches[c][patches[c].Count - 1];
                        if (ed1.To == last.To)
                        {
                            var tem = last.To;
                            last.To = last.From;
                            last.From = tem;
                            last.Vector = last.Vector.multiply(-1);
                        }

                        break;
                    }
                    if (count2 == 0) break;
                }
            }
            return patches;
        }

        private static bool SmoothAngle(double[] vec1, double[] vec2)
        {
            return Math.Abs(vec1.dotProduct(vec2)) > BoltAndGearConstants.SmoothAngle;
        }
    }
}
