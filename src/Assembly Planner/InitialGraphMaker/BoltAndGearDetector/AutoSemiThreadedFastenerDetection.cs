using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace Assembly_Planner
{
    internal class AutoSemiThreadedFastenerDetection
    {
        internal static void Run(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs)
        {
            var width = 55;
            var check = 0;
            LoadingBar.start(width, 0);

            var smallParts = FastenerDetector.SmallObjectsDetector(DisassemblyDirectionsWithFastener.PartsWithOneGeom);

            PreSelectedFastenerToFastenerClass(solidPrimitive, multipleRefs);
            foreach (
                var preSelected in
                    FastenerDetector.PreSelectedFasteners.Where(preSelected => !smallParts.Contains(preSelected)))
                smallParts.Add(preSelected);

            FastenerDetector.PotentialFastener = new Dictionary<TessellatedSolid, double>();
            foreach (var p in smallParts)
                FastenerDetector.PotentialFastener.Add(p, 0.1);
            var groupedPotentialFasteners = FastenerDetector.GroupingSmallParts(smallParts);
            var uniqueParts = new HashSet<TessellatedSolid>();
            foreach (var s in multipleRefs.Keys.Where(FastenerDetector.PotentialFastener.Keys.Contains))
                uniqueParts.Add(s);
            foreach (
                var preFastener in
                    FastenerDetector.Fasteners.Where(preFastener => uniqueParts.Contains(preFastener.Solid)))
                uniqueParts.Remove(preFastener.Solid);

            var equalPrimitivesForEveryUniqueSolid = FastenerDetector.EqualFlatPrimitiveAreaFinder(uniqueParts,
                solidPrimitive);
            List<int> learnerVotes;
            var learnerWeights = FastenerPerceptronLearner.ReadingLearnerWeightsAndVotesFromCsv(out learnerVotes);

            
            List<string> nameList = new List<string>();
            foreach (var part in uniqueParts)
            {
                nameList.Add(part.Name);
            }
            int preCutoff = 0;
            int postCutoff = 0;
            PartNameAnalysis.findCommonPreSuffixes(nameList, ref preCutoff, ref postCutoff);
            


            var refresh = (int)Math.Ceiling((float)uniqueParts.Count / (float)(width * 4));

            Parallel.ForEach(uniqueParts, solid =>
                //foreach (var solid in uniqueParts)
            {
                if (check % refresh == 0)
                {
                    LoadingBar.refresh(width, ((float)check) / ((float)uniqueParts.Count));
                }
                check++;

                double toolSize;
                var commonHead = AutoThreadedFastenerDetection.CommonHeadCheck(solid, solidPrimitive[solid],
                    equalPrimitivesForEveryUniqueSolid[solid], out toolSize);
                if (commonHead == 1)
                {
                    // can be a threaded hex fastener, threaded hex nut, nonthreaded hex fastener, nonthreaded hex nut
                    var newFastener = FastenerPolynomialTrend.PolynomialTrendDetector(solid);
                    if (newFastener != null)
                    {
                        // threaded hex fastener
                        newFastener.Tool = Tool.HexWrench;
                        newFastener.ToolSize = toolSize;
                        lock (FastenerDetector.Fasteners)
                            FastenerDetector.Fasteners.Add(newFastener);
                        AutoThreadedFastenerDetection.AddRepeatedSolidstoFasteners(newFastener, multipleRefs[solid]);
                        //continue;
                        return;
                    }
                    var lengthAndRadius =
                        AutoNonthreadedFastenerDetection.FastenerEngagedLengthAndRadiusNoThread(solid, solidPrimitive[solid]);
                    if (AutoNonthreadedFastenerDetection.IsItNut(
                        solidPrimitive[solid].Where(p => p is Cylinder).Cast<Cylinder>().ToList(), solid))
                    {
                        var nut = new Nut
                        {
                            NutType = NutType.Hex,
                            Solid = solid,
                            ToolSize = toolSize,
                            OverallLength = lengthAndRadius[0],
                            Diameter = lengthAndRadius[1]*2.0
                        };
                        lock (FastenerDetector.Nuts)
                            FastenerDetector.Nuts.Add(nut);
                        AutoThreadedFastenerDetection.AddRepeatedSolidstoNuts(nut, multipleRefs[solid]);
                    }
                    // nonthreaded hex fastener
                    var fas = new Fastener
                    {
                        Solid = solid,
                        FastenerType = FastenerTypeEnum.Bolt,
                        Tool = Tool.HexWrench,
                        ToolSize = toolSize,
                        RemovalDirection = FastenerDetector.RemovalDirectionFinderUsingObb(solid,
                        BoundingGeometry.OrientedBoundingBoxDic[solid]),
                        OverallLength =
                            GeometryFunctions.SortedLengthOfObbEdges(BoundingGeometry.OrientedBoundingBoxDic[solid])
                                [2],
                        EngagedLength = lengthAndRadius[0],
                        Diameter = lengthAndRadius[1]*2.0,
                        Certainty = 1.0
                    };
                    lock (FastenerDetector.Fasteners)
                        FastenerDetector.Fasteners.Add(fas);
                    AutoThreadedFastenerDetection.AddRepeatedSolidstoFasteners(fas, multipleRefs[solid]);
                    return;
                }
                if (commonHead == 2)
                {
                    // can be a threaded allen, nonthreaded allen
                    AddThreadedOrNonthreadedFastener(solid, multipleRefs[solid], solidPrimitive[solid], Tool.Allen,
                        toolSize);
                    return;
                }
                if (commonHead == 3 || commonHead == 5)
                {
                    // can be a threaded philips fastener, nonthreaded philips fastener
                    AddThreadedOrNonthreadedFastener(solid, multipleRefs[solid], solidPrimitive[solid], Tool.PhillipsBlade,
                        toolSize);
                    return;
                }
                if (commonHead == 4)
                {
                    // can be a threaded flat fastener, nonthreaded flat fastener
                    AddThreadedOrNonthreadedFastener(solid, multipleRefs[solid], solidPrimitive[solid], Tool.FlatBlade,
                        toolSize);
                    return;
                }

                // if it's not a common head and not threaded, I cannot detect it.
                // so the rest will be similar to threaded fastener detector
                if (FastenerPerceptronLearner.FastenerPerceptronClassifier(solidPrimitive[solid], solid, learnerWeights, learnerVotes))
                {
                    var newFastener = FastenerPolynomialTrend.PolynomialTrendDetector(solid);
                    if (newFastener != null)
                    {
                        lock (FastenerDetector.Fasteners)
                            FastenerDetector.Fasteners.Add(newFastener);
                        AutoThreadedFastenerDetection.AddRepeatedSolidstoFasteners(newFastener, multipleRefs[solid]);
                        //continue;
                        return;
                    }
                    // can be a nut
                    // use bounding cylinder to detect nuts.
                    // Since the nuts are small, the OBB function doesnt work accurately for them.
                    //  So, I cannot really trust this. 
                    var newNut = NutPolynomialTrend.PolynomialTrendDetector(solid);
                    if (newNut != null)
                        // It is a nut with certainty == 1
                    {
                        lock (FastenerDetector.Nuts)
                            FastenerDetector.Nuts.Add(newNut);
                        AutoThreadedFastenerDetection.AddRepeatedSolidstoNuts(newNut, multipleRefs[solid]);
                        //continue;
                        return;
                    }
                    // still can be a nut since the upper approach is not really accurate
                    // this 50 percent certainty can go up if the nut is mated with a 
                    // detected fastener 
                    foreach (var repeatedSolid in multipleRefs[solid])
                    {
                        lock (FastenerDetector.Nuts)
                            FastenerDetector.Nuts.Add(new Nut
                            {
                                Solid = repeatedSolid,
                                Diameter = BoundingGeometry.BoundingCylinderDic[solid].Radius*2.0,
                                // this is approximate
                                OverallLength = BoundingGeometry.BoundingCylinderDic[solid].Length,
                                Certainty = 0.5
                            });
                    }
                    //continue;
                    return;
                }
                // if it is not captured by any of the upper methods, give it another chance:
                if (AutoThreadedFastenerDetection.ThreadDetector(solid, solidPrimitive[solid]))
                {
                    var newFastener = FastenerPolynomialTrend.PolynomialTrendDetector(solid);
                    if (newFastener != null)
                    {
                        newFastener.FastenerType = FastenerTypeEnum.Bolt;
                        lock (FastenerDetector.Fasteners)
                            FastenerDetector.Fasteners.Add(newFastener);
                        AutoThreadedFastenerDetection.AddRepeatedSolidstoFasteners(newFastener, multipleRefs[solid]);
                        //continue;
                        return;
                    }
                    //if not, it is a nut:
                    foreach (var repeatedSolid in multipleRefs[solid])
                    {
                        lock (FastenerDetector.Nuts)
                            FastenerDetector.Nuts.Add(new Nut
                            {
                                Solid = repeatedSolid,
                                Diameter = BoundingGeometry.BoundingCylinderDic[solid].Radius*2.0,
                                // this is approximate
                                OverallLength = BoundingGeometry.BoundingCylinderDic[solid].Length*2.0,
                                Certainty = 0.5
                            });
                    }
                }
            }
                );
            // now use groupped small objects:
            AutoNonthreadedFastenerDetection.ConnectFastenersNutsAndWashers(groupedPotentialFasteners);
            LoadingBar.refresh(width, 1);
            Console.WriteLine("\n");
        }

        private static void AddThreadedOrNonthreadedFastener(TessellatedSolid solid, List<TessellatedSolid> repeated,
            List<PrimitiveSurface> prim, Tool tool, double toolSize)
        {
            var newFastener = FastenerPolynomialTrend.PolynomialTrendDetector(solid);
            if (newFastener != null)
            {
                newFastener.Tool = tool;
                newFastener.ToolSize = toolSize;
                lock (FastenerDetector.Fasteners)
                    FastenerDetector.Fasteners.Add(newFastener);
                AutoThreadedFastenerDetection.AddRepeatedSolidstoFasteners(newFastener, repeated);
                //continue;
                return;
            }
            var lengthAndRadius =
                AutoNonthreadedFastenerDetection.FastenerEngagedLengthAndRadiusNoThread(solid, prim);
            var fastener = new Fastener
            {
                Solid = solid,
                FastenerType = FastenerTypeEnum.Bolt,
                Tool = tool,
                ToolSize = toolSize,
                RemovalDirection =
                    FastenerDetector.RemovalDirectionFinderUsingObb(solid,
                        BoundingGeometry.OrientedBoundingBoxDic[solid]),
                OverallLength =
                    GeometryFunctions.SortedLengthOfObbEdges(BoundingGeometry.OrientedBoundingBoxDic[solid])[2],
                EngagedLength = lengthAndRadius[0],
                Diameter = lengthAndRadius[1]*2.0,
                Certainty = 1.0
            };
            lock (FastenerDetector.Fasteners)
                FastenerDetector.Fasteners.Add(fastener);
            AutoThreadedFastenerDetection.AddRepeatedSolidstoFasteners(fastener, repeated);
        }

        private static void PreSelectedFastenerToFastenerClass(
            Dictionary<TessellatedSolid, List<PrimitiveSurface>> solidPrimitive,
            Dictionary<TessellatedSolid, List<TessellatedSolid>> multipleRefs)
        {
            foreach (var preFastener in FastenerDetector.PreSelectedFasteners)
            {
                // if this part is repeated, add also those repeated parts to the fastener list
                var repeated = new HashSet<TessellatedSolid>();
                var isAKeyInMultipleRefs = multipleRefs.Keys.Where(r => r == preFastener).ToList();
                if (isAKeyInMultipleRefs.Any())
                {
                    repeated = new HashSet<TessellatedSolid>(multipleRefs[isAKeyInMultipleRefs[0]]) {preFastener};
                }
                else
                {
                    // it is a part in a value list
                    foreach (var key in multipleRefs.Keys)
                    {
                        if (!multipleRefs[key].Contains(preFastener)) continue;
                        repeated = new HashSet<TessellatedSolid>(multipleRefs[key]) {key};
                    }
                }
                var newFastener = FastenerPolynomialTrend.PolynomialTrendDetector(preFastener);
                if (newFastener != null)
                {
                    foreach (var repeatedSolid in repeated)
                    {
                        FastenerDetector.Fasteners.Add(new Fastener
                        {
                            Solid = repeatedSolid,
                            NumberOfThreads = newFastener.NumberOfThreads,
                            FastenerType = newFastener.FastenerType,
                            RemovalDirection = newFastener.RemovalDirection,
                            OverallLength = newFastener.OverallLength,
                            EngagedLength = newFastener.EngagedLength,
                            Diameter = newFastener.Diameter,
                            Certainty = 1.0
                        });
                    }
                    return;
                }
                var overalLength =
                    GeometryFunctions.SortedLengthOfObbEdges(BoundingGeometry.OrientedBoundingBoxDic[preFastener])[2];
                foreach (var repeatedSolid in repeated)
                {
                    FastenerDetector.Fasteners.Add(new Fastener
                    {
                        Solid = repeatedSolid,
                        FastenerType = FastenerTypeEnum.Bolt,
                        RemovalDirection =
                            FastenerDetector.RemovalDirectionFinderUsingObb(preFastener,
                                BoundingGeometry.OrientedBoundingBoxDic[preFastener]),
                        OverallLength = overalLength,
                        EngagedLength = overalLength,
                        Diameter = BoundingGeometry.BoundingCylinderDic[repeatedSolid].Radius,
                        Certainty = 1.0
                    });
                }
            }
        }
    }
}
