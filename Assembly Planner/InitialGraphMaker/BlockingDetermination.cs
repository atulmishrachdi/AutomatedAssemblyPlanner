using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using StarMathLib;
using TVGL;
using TVGL;
using PrimitiveClassificationOfTessellatedSolids;

namespace Assembly_Planner
{
    internal class BlockingDetermination
    {
        public static List<OverlappedSurfaces> OverlappingSurfaces = new List<OverlappedSurfaces>();
        internal static Dictionary<TessellatedSolid, List<PrimitiveSurface>> PrimitiveMaker(List<TessellatedSolid> parts)
        {
            var partPrimitive = new Dictionary<TessellatedSolid, List<PrimitiveSurface>>();
            //string pathDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //string filePath = pathDesktop + "\\mycsvfile.csv";

            //if (!File.Exists(filePath))
            //{
            //    File.Create(filePath).Close();
            //}
            //string delimter = ",";
            //List<string[]> output = new List<string[]>();



            //Parallel.ForEach(parts, solid =>
            foreach (var solid in parts)
            {
                if (solid.Faces.Count() == 2098 || solid.Faces.Count() == 896 || solid.Faces.Count() == 2096) continue;
                var solidPrim = TesselationToPrimitives.Run(solid);
                //lock (partPrimitive)
                partPrimitive.Add(solid, solidPrim);

                /*var cones = solidPrim.Where(p => p is Cone).ToList();
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
                var feature7 = (coneArea + cylinderArea)/solid.SurfaceArea;
                //lock (output)
                output.Add(new[]
                {
                    feature1.ToString(), feature2.ToString(), feature3.ToString(), feature4.ToString(),
                    feature5.ToString(), feature6.ToString(), feature7.ToString()
                });*/
                //(coneFacesCount / solid.Faces.Count()).ToString(), 
                //(coneAndCylinderArea/solid.SurfaceArea).ToString(), (flatArea/solid.SurfaceArea).ToString() });
            }
            // );


            /*int length = output.Count;
            using (TextWriter writer = File.CreateText(filePath))
            {

                for (int index = 0; index < length; index++)
                {
                    writer.WriteLine(string.Join(delimter, output[index]));
                }
            }*/

            return partPrimitive;
        }

        internal static bool DefineBlocking(designGraph assemblyGraph, TessellatedSolid solid1, TessellatedSolid solid2,
            List<PrimitiveSurface> solid1P, List<PrimitiveSurface> solid2P, List<int> globalDirPool,
            out List<int> dirInd)
        {
            if (BoundingBoxOverlap(solid1, solid2))
            {
                if (ConvexHullOverlap(solid1, solid2))
                {
                    //if (GearGear(assemblyGraph, solid1, solid2))
                    //{
                    // one more condition to check. If Gear-Gear, check and see if the normals of the solids are parallel
                    //    if (!ParallelNormals(assemblyGraph, solid1, solid2))
                    //    {
                    //        dirInd = null;
                    //        return false;
                    //    }
                    //    dirInd = GearNormal(assemblyGraph, solid1);
                    //    return true;
                    //}
                    var localDirInd = new List<int>();
                    for (var i = 0; i < DisassemblyDirections.Directions.Count; i++)
                        localDirInd.Add(i);
                    var overlappedPrimitives = new List<PrimitiveSurface[]>();
                    if (PrimitivePrimitiveInteractions.PrimitiveOverlap(solid1P, solid2P, localDirInd, out overlappedPrimitives))
                    {
                        var overlappingSurface = new OverlappedSurfaces { Solid1 = solid1, Solid2 = solid2, Overlappings = overlappedPrimitives };
                        OverlappingSurfaces.Add(overlappingSurface);
                        // dirInd is the list of directions that must be added to the arc between part1 and part2
                        Console.WriteLine(@"An overlap is detected between   " + solid1.Name + "   and   " + solid2.Name);
                        globalDirPool.AddRange(localDirInd.Where(d => !globalDirPool.Contains(d)));
                        foreach (var i in localDirInd)
                        {
                            Console.WriteLine(DisassemblyDirections.Directions[i][0] + " " +
                                              DisassemblyDirections.Directions[i][1] + " " +
                                              DisassemblyDirections.Directions[i][2]);
                        }
                        dirInd = localDirInd;
                        return true;
                    }
                }
            }
            dirInd = null;
            return false;
        }

        private static bool ParallelNormals(designGraph assemblyGraph, TessellatedSolid solid1, TessellatedSolid solid2)
        {
            var dir1 = VariableOfTheIndex(DisConstants.GearNormal, assemblyGraph[solid1.Name].localVariables);
            var dir2 = VariableOfTheIndex(DisConstants.GearNormal, assemblyGraph[solid1.Name].localVariables);
            return 1 - Math.Abs(dir1.dotProduct(dir2)) < ConstantsPrimitiveOverlap.EqualToZero;
        }

        private static double[] VariableOfTheIndex(double p, List<double> vars)
        {
            var ind = vars.IndexOf(p);
            return new[] { vars[ind + 1], vars[ind + 2], vars[ind + 3] };

        }

        private static List<int> GearNormal(designGraph assemblyGraph, TessellatedSolid solid1)
        {
            var dir = VariableOfTheIndex(DisConstants.GearNormal, assemblyGraph[solid1.Name].localVariables);
            return NormalIndexInGlobalDirns(dir);
        }

        private static bool GearGear(designGraph assemblyGraph, TessellatedSolid solid1, TessellatedSolid solid2)
        {
            return assemblyGraph[solid1.Name].localLabels.Contains(DisConstants.Gear) &&
                   assemblyGraph[solid2.Name].localLabels.Contains(DisConstants.Gear);
        }

        internal static bool ConvexHullOverlap(TessellatedSolid a, TessellatedSolid b)
        {
            foreach (var f in a.ConvexHullFaces)
            {
                var dStar = f.Normal.dotProduct(f.Vertices[0].Position);
                if (b.ConvexHullVertices.All(pt => (f.Normal.dotProduct(pt.Position)) > dStar+0.00001))
                    return false;
            }
            foreach (var f in b.ConvexHullFaces)
            {
                var dStar = f.Normal.dotProduct(f.Vertices[0].Position);
                if (a.ConvexHullVertices.All(pt => (f.Normal.dotProduct(pt.Position)) > dStar + 0.00001))
                    return false;
            }
            return true;
        }

        internal static bool BoundingBoxOverlap(TessellatedSolid a, TessellatedSolid b)
        {
            var aveXLength = ((a.XMax - a.XMin) + (b.XMax - b.XMin)) / 2.0;
            var aveYLength = ((a.YMax - a.YMin) + (b.YMax - b.YMin)) / 2.0;
            var aveZLength = ((a.ZMax - a.ZMin) + (b.ZMax - b.ZMin)) / 2.0;
            // There are some cases that two boxes are touching each other. So the bounding box or the CVH must not
            // return false. Define a threshold:
            if (a.XMin > b.XMax + ConstantsPrimitiveOverlap.FractionIncreaseForAABBIntersect * aveXLength
                || a.YMin > b.YMax + ConstantsPrimitiveOverlap.FractionIncreaseForAABBIntersect * aveYLength
                || a.ZMin > b.ZMax + ConstantsPrimitiveOverlap.FractionIncreaseForAABBIntersect * aveZLength
                || b.XMin > a.XMax + ConstantsPrimitiveOverlap.FractionIncreaseForAABBIntersect * aveXLength
                || b.YMin > a.YMax + ConstantsPrimitiveOverlap.FractionIncreaseForAABBIntersect * aveYLength
                || b.ZMin > a.ZMax + ConstantsPrimitiveOverlap.FractionIncreaseForAABBIntersect * aveZLength)
                return false;
            return true;
        }

        internal static List<int> NormalIndexInGlobalDirns(double[] p)
        {
            var dirs =
                DisassemblyDirections.Directions.Where(
                    globalDirn => 1 - Math.Abs(p.dotProduct(globalDirn)) < ConstantsPrimitiveOverlap.EqualToZero)
                    .ToList();
            return dirs.Select(dir => DisassemblyDirections.Directions.IndexOf(dir)).ToList();
        }
    }

    internal class OverlappedSurfaces
    {
        // This class is written for Weifeng's stability code
        /// <summary>
        /// Gets or sets the Solid1.
        /// </summary>
        /// <value>The Solid1.</value>
        internal TessellatedSolid Solid1 { set; get; }

        /// <summary>
        /// Gets or sets the Solid2.
        /// </summary>
        /// <value>The Solid2.</value>
        internal TessellatedSolid Solid2 { set; get; }

        /// <summary>
        /// The first element of array is surface of the Solid1 and
        /// the second one is surface of Solid2
        /// </summary>
        /// <value>The Overlapping surfaces</value>
        internal List<PrimitiveSurface[]> Overlappings {set; get;} 

    }
}
