using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseClasses;
using Geometric_Reasoning;
using StarMathLib;
using TVGL;


namespace Fastener_Detection
{
    public class GearDetector
    {
        public static List<Gear> Run(
            List<TessellatedSolid> solids,
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive, 
            bool estimate = false)
        {
            // Fast and inaccurate results vs Slow but accurate
            // estimate: inaccurate but fast
            return new List<Gear>();
            if (!estimate)
            {
                var gears =
                    solids.Select(GearPolynomialTrend.PolynomialTrendDetector).Where(gear => gear != null).ToList();
                return gears;
            }
            // This can be my old gear detector approach
            return solids.Select(s => GearDetectorEstimate(s, solidPrimitive[s])).Where(gear => gear != null).ToList();
        }


        public static List<int> UpdateRemovalDirectionsIfGearMate(List<TessellatedSolid> subAssem1, List<TessellatedSolid> subAssem2,
            List<Gear> gears, List<int> localDirInd)
        {
            // "gear1" is always "Reference" and "gear2" is always "Moving"
            if (subAssem1.Count != 1 || subAssem2.Count != 1) return localDirInd;
            var gear1L = gears.Where(g => g.Solid == subAssem1[0]).ToList();
            var gear2L = gears.Where(g => g.Solid == subAssem2[0]).ToList();
            if (!gear1L.Any() || !gear2L.Any()) return localDirInd;
            var gear1 = gear1L[0];
            var gear2 = gear2L[0];
            // now it is a gear mate
            var rd = Geometric_Reasoning.StartProcess.Directions.First(d => Math.Abs(d.dotProduct(gear1.Axis) - 1) < 0.01);
            var ind1 = Geometric_Reasoning.StartProcess.Directions.IndexOf(rd);
            var ind2 = Geometric_Reasoning.StartProcess.Directions.IndexOf(rd.multiply(-1.0));

            if (Math.Abs(gear1.Axis.dotProduct(gear2.Axis)) < 0.001)
            {
                gear1.Type = GearType.Bevel;
                gear2.Type = GearType.Bevel;
                if (!localDirInd.Contains(ind1)) localDirInd.Add(ind1);
                if (!localDirInd.Contains(ind2)) localDirInd.Add(ind2);
                var rd2 = Geometric_Reasoning.StartProcess.Directions.First(d => Math.Abs(d.dotProduct(gear2.Axis) - 1) < 0.01);
                var ind21 = Geometric_Reasoning.StartProcess.Directions.IndexOf(rd2);
                var ind22 = Geometric_Reasoning.StartProcess.Directions.IndexOf(rd2.multiply(-1.0));
                if (!localDirInd.Contains(ind21)) localDirInd.Add(ind1);
                if (!localDirInd.Contains(ind22)) localDirInd.Add(ind2);
            }
            // add two simple removal direction if they are not already in the list:
            if (gear1.Type == GearType.Internal || gear2.Type == GearType.Internal)
            {
                if (!localDirInd.Contains(ind1)) localDirInd.Add(ind1);
                if (!localDirInd.Contains(ind2)) localDirInd.Add(ind2);
                return localDirInd;
            }
            // if they are simple gears: spur:
            if (!localDirInd.Contains(ind1)) localDirInd.Add(ind1);
            if (!localDirInd.Contains(ind2)) localDirInd.Add(ind2);
            if (gear1.PointOnAxis == null || gear2.PointOnAxis == null)
                return localDirInd;
            var oneMore =
                Geometric_Reasoning.StartProcess.Directions.First(
                    d => Math.Abs(d.dotProduct(gear2.PointOnAxis.subtract(gear1.PointOnAxis)) - 1) < 0.01);
            var indOneMore = Geometric_Reasoning.StartProcess.Directions.IndexOf(oneMore);
            if (!localDirInd.Contains(indOneMore)) localDirInd.Add(indOneMore);
            return localDirInd;
        }

        internal static Gear GearDetectorEstimate(TessellatedSolid solid, List<PrimitiveSurface> solidPrimitives)
        {
            var flats =
                solidPrimitives.Where(
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
                        var cross = new[] {0.0, 0, 0};
                        var vec1 = newPatch[i].Vector.normalize();
                        var vec2 = newPatch[i + 1].Vector.normalize();
                        if (SmoothAngle(vec1, vec2))
                            continue;
                        cross = vec1.crossProduct(vec2);
                        crossP.Add(cross);
                    }
                    if (crossP.Count < 10) continue;
                    var crossSign = GeometryFunctions.ConvertCrossProductToSign(crossP);
                    if (!IsGear(crossSign)) continue;
                    //Console.WriteLine("Is " + solid.Name + " a gear? 'y' or 'n'");
                    //var read = Convert.ToString(Console.ReadLine());
                    //if (read == "n")
                    //    continue;
                    return new Gear { Solid = solid, Axis = flatPrim.Faces[0].Normal};
                }
            }
            return null;
        }

        private static bool IsGear(List<int> crossSign)
        {
            crossSign = BoltAndGearUpdateFunctions.CrossUpdate(crossSign);
            var isGear = true;
            var counter = 0;
            var startInd = 0;
            for (var i = 0; i < crossSign.Count; i++)
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
