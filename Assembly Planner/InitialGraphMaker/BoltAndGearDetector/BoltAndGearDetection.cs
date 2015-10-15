using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using AssemblyEvaluation;
using Assembly_Planner.GraphSynth.BaseClasses;
using StarMathLib;
using TVGL;
using Tool = Assembly_Planner.GraphSynth.BaseClasses.Tool;

namespace Assembly_Planner
{
    class BoltAndGearDetection
    {
        internal static List<Fastener>  Fasteners= new List<Fastener>();
        internal static List<Nut> Nuts = new List<Nut>(); 
        internal static HashSet<TessellatedSolid> ScrewAndBoltDetector(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs, bool autoDetection = false)
        {
            var s = Stopwatch.StartNew();
            s.Start();
            Console.WriteLine();
            Console.WriteLine("Detecting Gears and Fasteners ....");
            var fastener = new HashSet<TessellatedSolid>();
            if (!autoDetection)
            {
                foreach (var solid in solidPrimitive.Keys)
                    if (solid.Name.Contains("Screw") || solid.Name.Contains("Test - Part-S"))
                        fastener.Add(solid);
                    else if (solid.Name.Contains("ShaftCollar"))
                        fastener.Add(solid);
                    else if (solid.Name.Contains("DowellGrooved"))
                        if (solid.Name.Contains("-5") || solid.Name.Contains("-14") || solid.Name.Contains("-27") ||
                            solid.Name.Contains("-30"))
                            continue;
                        else
                            fastener.Add(solid);
            }
            else
            {
                fastener = AutoFastenerDetection(solidPrimitive, multipleRefs);
            }

            //var smallObjects = SmallObjectsDetector(solidPrimitive);
            //fastener.UnionWith(smallObjects);
            s.Stop();
            Console.WriteLine("Gear and Fastener Detection:" + "     " + s.Elapsed);
            return fastener;
            // Here are my thoughts about a bolt:
            // Since all of the threads are classified as cone, 
            //    if the number of cones are more than 30 percent of the total number of primitives
            //    AND, the summation of area of cone primitivies are more than 30 percent of the solid surface area
            foreach (var solid in solidPrimitive.Keys)
            {
                var cones = solidPrimitive[solid].Where(p => p is Cone).ToList();
                if (cones.Count < BoltAndGearConstants.ConePortion*solidPrimitive[solid].Count ||
                    cones.Sum(p => p.Area) < BoltAndGearConstants.ConeAreaPortion*solid.SurfaceArea)
                    continue;
                Console.WriteLine("Is " + solid.Name + " a Bolt or Screw? 'y' or 'n'");
                var read = Convert.ToString(Console.ReadLine());
                if (read == "y")
                {
                    fastener.Add(solid);
                }
            }
            return fastener;
        }

        private static HashSet<TessellatedSolid> AutoFastenerDetection(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs)
        {
            var fastener = new HashSet<TessellatedSolid>();
            var firstFilter = multipleRefs.Keys.ToList();//SmallObjectsDetector(multipleRefs);
            var equalPrimittivesForEverySolid = EqualPrimitiveAreaFinder(firstFilter, solidPrimitive);
            foreach (var solid in firstFilter)
            {
                if (HexBoltNutAllen(solid, solidPrimitive[solid], equalPrimittivesForEverySolid[solid]))
                    continue;
                if (PhilipsHeadBolt(solid, solidPrimitive[solid], equalPrimittivesForEverySolid[solid]))
                    continue;
                if (SlotHeadBolt(solid, solidPrimitive[solid], equalPrimittivesForEverySolid[solid]))
                    continue;
            }
            


            return fastener;
        }

        private static bool HexBoltNutAllen(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<PrimitiveSurface, List<PrimitiveSurface>> equalPrimittives)
        {
            var sixFlat = EqualPrimitivesFinder(equalPrimittives, 6);
            if (!sixFlat.Any()) return false;
            foreach (var candidateHex in sixFlat)
            {
                var candidateHexVal = equalPrimittives[candidateHex];
                var cos = new List<double>();
                var firstPrimNormal = ((Flat)candidateHexVal[0]).Normal;
                for (var i = 1; i < candidateHexVal.Count; i++)
                    cos.Add(firstPrimNormal.dotProduct(((Flat)candidateHexVal[i]).Normal));
                // if it is a hex or allen bolt, the cos list must have two 1/2, two -1/2 and one -1
                if (cos.Count(c => Math.Abs(0.5 - c) < 0.0001) != 2 || 
                    cos.Count(c => Math.Abs(-0.5 - c) < 0.0001) != 2 ||
                    cos.Count(c => Math.Abs(-1 - c) < 0.0001) != 1) continue;
                if (candidateHexVal.Any(p => p.OuterEdges.Any(e => e.Curvature == CurvatureType.Concave)))
                {
                    // this is a socket bolt (allen)
                    var fastener = new Fastener
                    {
                        Solid = solid,
                        FastenerType = FastenerTypeEnum.Bolt,
                        Tool = Tool.Allen
                    };
                    Fasteners.Add(fastener);
                    return true;
                }
                // else: it is a hex bolt or nut
                if (IsItNut(solidPrim.Where(p => p is Cylinder).Cast<Cylinder>().ToList(), solid))
                {
                    var nut = new Nut { NutType = NutType.Hex, Solid = solid };
                    Nuts.Add(nut);
                    return true;
                }
                Fasteners.Add(new Fastener
                {
                    Solid = solid,
                    FastenerType = FastenerTypeEnum.Bolt,
                    Tool = Tool.HexWrench
                });
                return true;
            }
            return false;
        }

        private static bool IsItNut(List<Cylinder> cylinders, TessellatedSolid boltOrNut)
        {
            if (cylinders.Any(p => !p.IsPositive))
                if (cylinders.Where(c => c.IsPositive).Sum(pC => pC.Area) <
                    0.05 * boltOrNut.SurfaceArea)
                    // this is a nut. Because it has negative cylinders and positive cylinders that it has
                    // are minor
                    return true;
            return false;
        }

        private static bool PhilipsHeadBolt(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<PrimitiveSurface, List<PrimitiveSurface>> equalPrimittives)
        {
            var eightFlat = EqualPrimitivesFinder(equalPrimittives, 8);
            if (!eightFlat.Any()) return false;
            foreach (var candidateHex in eightFlat)
            {
                var candidateHexVal = equalPrimittives[candidateHex];
                var cos = new List<double>();
                var firstPrimNormal = ((Flat) candidateHexVal[0]).Normal;
                for (var i = 1; i < candidateHexVal.Count; i++)
                    cos.Add(firstPrimNormal.dotProduct(((Flat) candidateHexVal[i]).Normal));
                // if it is philips head, the cos list must have four 0, two -1 and one 1
                if (cos.Count(c => Math.Abs(0.0 - c) < 0.0001) != 4 || 
                    cos.Count(c => Math.Abs(-1 - c) < 0.0001) != 2 ||
                    cos.Count(c => Math.Abs(1 - c) < 0.0001) != 1) continue;
                var fastener = new Fastener
                {
                    Solid = solid,
                    FastenerType = FastenerTypeEnum.Bolt,
                    Tool = Tool.PhilipsBlade
                };
                Fasteners.Add(fastener);
                return true;
            }
            return false;
        }


        private static bool SlotHeadBolt(TessellatedSolid solid, List<PrimitiveSurface> solidPrim,
            Dictionary<PrimitiveSurface, List<PrimitiveSurface>> equalPrimittives)
        {
            var twoFlat = EqualPrimitivesFinder(equalPrimittives, 2);
            if (!twoFlat.Any()) return false;
            foreach (var candidateHex in twoFlat)
            {
                var candidateHexVal = equalPrimittives[candidateHex];
                var cos = ((Flat) candidateHexVal[0]).Normal.dotProduct(((Flat) candidateHexVal[1]).Normal);
                if (!(Math.Abs(-1 - cos) < 0.0001)) continue;
                // I will add couple of conditions here:
                //    1. If the number of solid vertices in front of each flat is equal to another
                //    2. If the summation of the vertices in 1 is greater than the total # of verts
                //    3. and I also need to add some constraints for the for eample the area of the cylinder
                var leftVerts = VertsInfrontOfFlat(solid, (Flat)candidateHexVal[0]);
                var rightVerts = VertsInfrontOfFlat(solid, (Flat)candidateHexVal[1]);
                if (Math.Abs(leftVerts - rightVerts) > 2) return false;
                if (!solidPrim.Where(p => p is Cylinder).Cast<Cylinder>().Any(c => c.IsPositive)) return false;
                var fastener = new Fastener
                {
                    Solid = solid,
                    FastenerType = FastenerTypeEnum.Bolt,
                    Tool = Tool.FlatBlade
                };
                Fasteners.Add(fastener);
                return true;
            }
            return false;
        }

        private static Dictionary<TessellatedSolid, Dictionary<PrimitiveSurface, List<PrimitiveSurface>>>
            EqualPrimitiveAreaFinder(List<TessellatedSolid> firstFilter,
                Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive)
        {
            var equalPrim = new Dictionary<TessellatedSolid, Dictionary<PrimitiveSurface, List<PrimitiveSurface>>>();
            foreach (var solid in firstFilter)
            {
                var primEqualArea = new Dictionary<PrimitiveSurface, List<PrimitiveSurface>>();
                foreach (var prim in solidPrimitive[solid].Where(p => p is Flat))
                {
                    var equalExist = primEqualArea.Keys.Where(p =>Math.Abs(p.Area - prim.Area) < 0.01).ToList();
                    if (!equalExist.Any()) primEqualArea.Add(prim, new List<PrimitiveSurface> { prim });
                    else
                    {
                        foreach (var equal in equalExist)
                        {
                            primEqualArea[equal].Add(prim);
                            break;
                        }
                    }
                }
                equalPrim.Add(solid, primEqualArea);
            }
            return equalPrim;
        }


        private static List<PrimitiveSurface> EqualPrimitivesFinder(
            Dictionary<PrimitiveSurface, List<PrimitiveSurface>> equalPrimittives, int numberOfEqualPrim)
        {
            return equalPrimittives.Keys.Where(
                k => equalPrimittives[k].Count == numberOfEqualPrim).ToList();
        }

        private static int VertsInfrontOfFlat(TessellatedSolid solid, Flat flat)
        {
            return
                solid.Vertices.Count(
                    v => flat.Normal.dotProduct(v.Position.subtract(flat.Faces[0].Vertices[0].Position)) > 0);
        }

        private static List<TessellatedSolid> SmallObjectsDetector(Dictionary<TessellatedSolid, List<TessellatedSolid>> solidRepeated)
        {
            var partSize = new Dictionary<TessellatedSolid, double>();
            var parts = solidRepeated.Keys.ToList();
            foreach (var solid in solidRepeated.Keys.ToList())
            {
                var shortestObbEdge = double.PositiveInfinity;
                var longestObbEdge = double.NegativeInfinity;
                var solidObb = PartitioningSolid.OrientedBoundingBoxDic[solid];
                for (var i = 1; i < solidObb.CornerVertices.Count(); i++)
                {
                    var dis =
                        Math.Sqrt(Math.Pow(solidObb.CornerVertices[0].Position[0] - solidObb.CornerVertices[i].Position[0], 2.0) +
                                  Math.Pow(solidObb.CornerVertices[0].Position[1] - solidObb.CornerVertices[i].Position[1], 2.0) +
                                  Math.Pow(solidObb.CornerVertices[0].Position[2] - solidObb.CornerVertices[i].Position[2], 2.0));
                    if (dis < shortestObbEdge) shortestObbEdge = dis;
                    if (dis > longestObbEdge) longestObbEdge = dis;
                }
                var sizeMetric = solid.Volume * (longestObbEdge / shortestObbEdge);
                partSize.Add(solid, sizeMetric);
            }
            // if removing the first 10 percent drops the max size by 95 percent, consider them as noise: 
            var maxSize = partSize.Values.Max();
            var sortedPartSize = partSize.ToList();
            sortedPartSize.Sort((x, y) => y.Value.CompareTo(x.Value));

            var noise = new List<TessellatedSolid>();
            for (var i = 0; i < Math.Ceiling(partSize.Count * 5 / 100.0); i++)
                noise.Add(sortedPartSize[i].Key);
            var approvedNoise = new List<TessellatedSolid>();
            for (var i = 0; i < noise.Count; i++)
            {
                var newList = new List<TessellatedSolid>();
                for (var j = 0; j < i + 1; j++)
                    newList.Add(noise[j]);
                var max =
                    partSize.Where(a => !newList.Contains(a.Key))
                        .ToDictionary(key => key.Key, value => value.Value)
                        .Values.Max();
                if (max > maxSize * 10.0 / 100.0) continue;
                approvedNoise = newList;
                break;
            }
            maxSize = partSize.Where(a => !approvedNoise.Contains(a.Key))
                        .ToDictionary(key => key.Key, value => value.Value)
                        .Values.Max();

            // If I have detected a portion of the fasteners (a very good number of them possibly) by this point,
            // it can accellerate the primitive classification. If it is fastener, it doesn't need to be classified?
            //string pathDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //string filePath = pathDesktop + "\\mycsvfile.csv";

            //if (!File.Exists(filePath))
            //{
            //    File.Create(filePath).Close();
            //}
            //string delimter = ",";
            //List<string[]> output = new List<string[]>();
            //foreach (var solid in parts.Where(s=>!approvedNoise.Contains(s)))
            //{
            /*var solidPrim = partPrimitive[solid];
            var cones = solidPrim.Where(p => p is Cone).ToList();
            var flat = solidPrim.Where(p => p is Flat).ToList();
            var cylinder = solidPrim.Where(p => p is Cylinder).ToList();

            double coneFacesCount = cones.Sum(c => c.Faces.Count);
            double flatFacesCount = flat.Sum(f => f.Faces.Count);
            double cylinderFacesCount = cylinder.Sum(c => c.Faces.Count);

            var coneArea = cones.Sum(c => c.Area);
            var flatArea = flat.Sum(c => c.Area);
            var cylinderArea = cylinder.Sum(c => c.Area);

            var feature1 = flatFacesCount/(flatArea/solid.SurfaceArea);
            var feature2 = coneFacesCount/(coneArea/solid.SurfaceArea);
            var feature3 = cylinderFacesCount/(cylinderArea/solid.SurfaceArea);
            var feature4 = coneFacesCount/solid.Faces.Count();
            var feature5 = flatFacesCount/solid.Faces.Count();
            var feature6 = cylinderFacesCount/solid.Faces.Count();
            var feature7 = (coneArea + cylinderArea)/solid.SurfaceArea;*/
            //var feature8 = partSize[solid]/maxSize;
            //Console.WriteLine(solid.Name + "   " + feature8);
            //lock (output)
            //output.Add(new[]
            // {
            //    feature1.ToString(), feature2.ToString(), feature3.ToString(), feature4.ToString(),
            //     feature5.ToString(), feature6.ToString(), feature7.ToString()
            // });
            //}

            /* int length = output.Count;
             using (TextWriter writer = File.CreateText(filePath))
                 for (int index = 0; index < length; index++)
                     writer.WriteLine(string.Join(delimter, output[index]));*/


            // creating the dictionary:
            var n = 99.0; // number of classes
            var dic = new Dictionary<double, List<TessellatedSolid>>();

            // Filling up the keys
            var minSize = partSize.Where(a => !approvedNoise.Contains(a.Key))
                        .ToDictionary(key => key.Key, value => value.Value)
                        .Values.Min();
            var smallestSolid = partSize.Keys.Where(s => partSize[s] == minSize).ToList();

            for (var i = 0; i < n; i++)
            {
                var ini = new List<TessellatedSolid>();
                var key = minSize + (i / (n - 1)) * (maxSize - minSize);
                dic.Add(key, ini);
            }

            dic[minSize].AddRange(smallestSolid);
            dic[maxSize].AddRange(approvedNoise);

            // Filling up the values
            var keys = dic.Keys.ToList();
            foreach (var f in parts.Where(a => !approvedNoise.Contains(a)))
            {
                for (var i = 0; i < n - 2; i++)
                {
                    if (partSize[f] >= keys[i] && partSize[f] <= keys[i + 1])
                    {
                        if (Math.Abs(partSize[f] - keys[i]) > Math.Abs(partSize[f] - keys[i + 1]))
                            dic[keys[i + 1]].Add(f);
                        else dic[keys[i]].Add(f);
                    }
                }
            }
            //foreach (var v in dic[minSize])
            //    Console.WriteLine(v.Name);//partSize[v]/maxSize);
            return dic[minSize];
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
