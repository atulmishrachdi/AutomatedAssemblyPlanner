using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Assembly_Planner.GeometryReasoning;
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
            var s = Stopwatch.StartNew();
            s.Start();
            Console.WriteLine();
            Console.WriteLine("Classifying Primitives for " + parts.Count + " unique parts ....");
            var partPrimitive = new Dictionary<TessellatedSolid, List<PrimitiveSurface>>();
            //Parallel.ForEach(parts, solid =>
            foreach (var solid in parts)
            {
                var solidPrim = TesselationToPrimitives.Run(solid);
                lock (partPrimitive)
                    partPrimitive.Add(solid, solidPrim);
            }
             //);

            s.Stop();
            Console.WriteLine("Primitive classification is done in:" + "     " + s.Elapsed);
            return partPrimitive;
        }

        internal static bool DefineBlocking(designGraph assemblyGraph, TessellatedSolid solid1, TessellatedSolid solid2,
            List<PrimitiveSurface> solid1P, List<PrimitiveSurface> solid2P, List<int> globalDirPool,
            out List<int> dirInd, out double certainty, int printResults = 0)
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
                    if (PrimitivePrimitiveInteractions.PrimitiveOverlap(solid1P, solid2P, localDirInd,
                        out overlappedPrimitives, out certainty))
                    {
                        var overlappingSurface = new OverlappedSurfaces
                        {
                            Solid1 = solid1,
                            Solid2 = solid2,
                            Overlappings = overlappedPrimitives
                        };
                        OverlappingSurfaces.Add(overlappingSurface);
                        // dirInd is the list of directions that must be added to the arc between part1 and part2
                        globalDirPool.AddRange(localDirInd.Where(d => !globalDirPool.Contains(d)));
                        if (printResults > 0)
                        {
                            if (printResults == 1)
                                Console.WriteLine(@"An overlap is detected between   " + solid1.Name + "   and   " +
                                                  solid2.Name);
                            else
                            {
                                Console.WriteLine(@"An overlap is detected between   " + solid1.Name + "   and   " +
                                                  solid2.Name);
                                foreach (var i in localDirInd)
                                {
                                    Console.WriteLine(DisassemblyDirections.Directions[i][0] + " " +
                                                      DisassemblyDirections.Directions[i][1] + " " +
                                                      DisassemblyDirections.Directions[i][2]);
                                }
                            }

                        }
                        dirInd = localDirInd;
                        return true;
                    }
                }
            }
            certainty = 1.0;
            dirInd = null;
            return false;
        }

        private static bool ParallelNormals(designGraph assemblyGraph, TessellatedSolid solid1, TessellatedSolid solid2)
        {
            var dir1 = VariableOfTheIndex(DisConstants.GearNormal, assemblyGraph[solid1.Name].localVariables);
            var dir2 = VariableOfTheIndex(DisConstants.GearNormal, assemblyGraph[solid1.Name].localVariables);
            return 1 - Math.Abs(dir1.dotProduct(dir2)) < OverlappingFuzzification.EqualToZeroL;
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
                var dStar = (f.Normal.dotProduct(f.Vertices[0].Position));
                if (b.ConvexHullVertices.All(pt => (f.Normal.dotProduct(pt.Position)) > dStar + 0.001))
                    return false;
            }
            foreach (var f in b.ConvexHullFaces)
            {
                var dStar = (f.Normal.dotProduct(f.Vertices[0].Position));
                if (a.ConvexHullVertices.All(pt => (f.Normal.dotProduct(pt.Position)) > dStar + 0.001))
                    return false;
            }
            return true;
        }

        internal static bool BoundingBoxOverlap(TessellatedSolid a, TessellatedSolid b)
        {
            var aveXLength = (Math.Abs(a.XMax - a.XMin) + Math.Abs(b.XMax - b.XMin)) / 2.0;
            var aveYLength = (Math.Abs(a.YMax - a.YMin) + Math.Abs(b.YMax - b.YMin)) / 2.0;
            var aveZLength = (Math.Abs(a.ZMax - a.ZMin) + Math.Abs(b.ZMax - b.ZMin)) / 2.0;
            // There are some cases that two boxes are touching each other. So the bounding box or the CVH must not
            // return false. Define a threshold:
            if (a.XMin > b.XMax + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveXLength
                || a.YMin > b.YMax + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveYLength
                || a.ZMin > b.ZMax + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveZLength
                || b.XMin > a.XMax + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveXLength
                || b.YMin > a.YMax + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveYLength
                || b.ZMin > a.ZMax + OverlappingFuzzification.FractionIncreaseForAABBIntersect * aveZLength)
                return false;
            return true;
        }

        internal static List<int> NormalIndexInGlobalDirns(double[] p)
        {
            var dirs =
                DisassemblyDirections.Directions.Where(
                    globalDirn => 1 - Math.Abs(p.dotProduct(globalDirn)) < OverlappingFuzzification.EqualToZeroL)
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
