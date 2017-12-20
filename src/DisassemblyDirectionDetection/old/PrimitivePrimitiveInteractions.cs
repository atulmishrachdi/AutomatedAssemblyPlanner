using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL;
namespace Assembly_Planner
{
    internal class PrimitivePrimitiveInteractions
    {
        internal static List<int> DirInd;
        internal static double MaxProb;

        internal static bool PrimitiveOverlap(List<PrimitiveSurface> solid1P, List<PrimitiveSurface> solid2P,
            List<int> dirInd, out List<PrimitiveSurface[]> overlappedPrimitives, out double certainty)
        {
            var overlap = false;
            MaxProb = 0.0;
            DirInd = dirInd;
            var lastCheck = new PrimitiveSurface[2];
            overlappedPrimitives = new List<PrimitiveSurface[]>();
            foreach (var primitiveA in solid1P)
            {
                foreach (var primitiveB in solid2P)
                {
                    if (overlap)
                        overlappedPrimitives.Add(lastCheck);
                    lastCheck = new[] {primitiveA, primitiveB};
                    // 1=flat, 2 =cylinder, 3 = sphere, 4= cone
                    if (primitiveA is Flat && primitiveB is Flat)
                    {
                        overlap =FlatFlatOverlappingCheck((Flat)primitiveA, (Flat)primitiveB); //
                        continue;
                    }
                    if (primitiveA is Flat && primitiveB is Cylinder)
                    {
                        overlap =FlatCylinderOverlappingCheck((Cylinder)primitiveB, (Flat)primitiveA, 1); //
                        continue;
                    }
                    if (primitiveA is Flat && primitiveB is Sphere)
                    {
                        overlap = FlatSphereOverlappingCheck((Sphere)primitiveB, (Flat)primitiveA, 1); //
                        continue;
                    }
                    if (primitiveA is Flat && primitiveB is Cone)
                    {
                        overlap = FlatConeOverlappingCheck((Cone)primitiveB, (Flat)primitiveA, 1); //
                        continue;
                    }


                    if (primitiveA is Cylinder && primitiveB is Flat)
                    {
                        overlap = FlatCylinderOverlappingCheck((Cylinder)primitiveA, (Flat)primitiveB, 2); //
                        continue;
                    }
                    if (primitiveA is Cylinder && primitiveB is Cylinder)
                    {
                        overlap = CylinderCylinderOverlappingCheck((Cylinder)primitiveA, (Cylinder)primitiveB);
                        continue;
                    }
                    if (primitiveA is Cylinder && primitiveB is Sphere)
                    {
                        overlap = CylinderSphereOverlappingCheck((Cylinder)primitiveA, (Sphere)primitiveB);
                        continue;
                    }
                    if (primitiveA is Cylinder && primitiveB is Cone)
                    {
                        overlap = ConeCylinderOverlappingCheck((Cone)primitiveB, (Cylinder)primitiveA, 2);
                        continue;
                    }


                    if (primitiveA is Sphere && primitiveB is Flat)
                    {
                        overlap = FlatSphereOverlappingCheck((Sphere)primitiveA, (Flat)primitiveB, 3); //
                        continue;
                    }
                    if (primitiveA is Sphere && primitiveB is Cylinder)
                    {
                        overlap = CylinderSphereOverlappingCheck((Cylinder)primitiveB, (Sphere)primitiveA);
                        continue;
                    }
                    if (primitiveA is Sphere && primitiveB is Sphere)
                    {
                        overlap = SphereSphereOverlappingCheck((Sphere)primitiveA, (Sphere)primitiveB);
                        continue;
                    }
                    if (primitiveA is Sphere && primitiveB is Cone)
                    {
                        overlap = ConeSphereOverlappingCheck((Cone)primitiveB, (Sphere)primitiveA, 3);
                        continue;
                    }

                    if (primitiveA is Cone && primitiveB is Flat)
                    {
                        overlap = FlatConeOverlappingCheck((Cone)primitiveA, (Flat)primitiveB, 4); //
                        continue;
                    }
                    if (primitiveA is Cone && primitiveB is Cylinder)
                    {
                        overlap = ConeCylinderOverlappingCheck((Cone)primitiveA, (Cylinder)primitiveB, 4);
                        continue;
                    }
                    if (primitiveA is Cone && primitiveB is Sphere)
                    {
                        overlap = ConeSphereOverlappingCheck((Cone)primitiveA, (Sphere)primitiveB, 4);
                        continue;
                    }
                    if (primitiveA is Cone && primitiveB is Cone)
                    {
                        overlap = ConeConeOverlappingCheck((Cone)primitiveA, (Cone)primitiveB);
                        continue;
                    }
                }
            }
            certainty = MaxProb;
            if (certainty > 0)
            {
                return true;
            }
            return false;
        }

        public static bool ConeSphereOverlappingCheck(Cone cone, Sphere sphere, int re)
        {
            if (!cone.IsPositive && sphere.IsPositive)
                return NegConePosSphereOverlappingCheck(cone, sphere, re);
            return false;
        }

        private static bool NegConePosSphereOverlappingCheck(Cone cone, Sphere sphere, int re)
        {
            var t1 = (sphere.Center[0] - cone.Apex[0]) / (cone.Axis[0]);
            var t2 = (sphere.Center[1] - cone.Apex[1]) / (cone.Axis[1]);
            var t3 = (sphere.Center[2] - cone.Apex[2]) / (cone.Axis[2]);
            var p0 = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.PointOnLineL,
                OverlappingFuzzification.PointOnLineU, Math.Abs(t1 - t2));
            var p1 = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.PointOnLineL,
                OverlappingFuzzification.PointOnLineU, Math.Abs(t1 - t3));
            var p2 = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.PointOnLineL,
                OverlappingFuzzification.PointOnLineU, Math.Abs(t3 - t2));
            // min of p0, p1 and p2
            var maxOverlappingProb = Math.Min(Math.Min(p0, p1), p2);

            if (maxOverlappingProb > 0)
                /*if (Math.Abs(t1 - t2) < ConstantsPrimitiveOverlap.PointOnLine &&
                Math.Abs(t1 - t3) < ConstantsPrimitiveOverlap.PointOnLine &&
                Math.Abs(t3 - t2) < ConstantsPrimitiveOverlap.PointOnLine)*/
            {
                var localProb = 0.0;
                foreach (var f1 in cone.Faces)
                {
                    foreach (var f2 in sphere.Faces)
                    {
                        var pP = TwoTrianglesParallelCheck(f1.Normal, f2.Normal);
                        var sP = TwoTrianglesSamePlaneCheck(f1, f2);
                        if (pP == 0 || sP == 0 || !TwoTriangleOverlapCheck(f1, f2)) continue;

                        var p = Math.Min(pP, sP);
                        if (p > localProb) localProb = p;
                        if (localProb == 1)
                            break;
                    }
                    if (localProb == 1)
                        break;
                }
                maxOverlappingProb = Math.Min(maxOverlappingProb, localProb);
            }
            if (maxOverlappingProb == 0 || double.IsNaN(maxOverlappingProb)) return false;
            if (maxOverlappingProb > MaxProb) 
                MaxProb = maxOverlappingProb;

            // For a sphere inside of a cone:
            //    if sphere is reference:
            //       if the angle between axis and the normal of one of the faces is less than 90 (dot is pos)
            //          removal direction has he same direction as the neg axis
            //       if dot is neg
            //          removal direction has he same direction as the axis
            //    if cone is reference:
            //       if the angle between axis and the normal of one of the faces is less than 90 (dot is pos)
            //          removal direction has he same direction as the axis
            //       if dot is neg
            //          removal direction has he same direction as the neg axis
            
            if (re == 3)
            {
                if (cone.Axis.normalize().dotProduct(cone.Faces[0].Normal) > 0)
                {
                    for (var i = 0; i < DirInd.Count; i++)
                    {
                        var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                        if (1 + cone.Axis.normalize().dotProduct(dir) < OverlappingFuzzification.CheckWithGlobDirsParall) continue;
                        DirInd.Remove(DirInd[i]);
                        i--;
                    }
                }
                else
                {
                    for (var i = 0; i < DirInd.Count; i++)
                    {
                        var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                        if (1 - cone.Axis.normalize().dotProduct(dir) < OverlappingFuzzification.CheckWithGlobDirsParall) continue;
                        DirInd.Remove(DirInd[i]);
                        i--;
                    }
                }
                return true;
            }
            
            if (cone.Axis.normalize().dotProduct(cone.Faces[0].Normal) > 0)
            {
                for (var i = 0; i < DirInd.Count; i++)
                {
                    var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                    if (1 - cone.Axis.normalize().dotProduct(dir) < OverlappingFuzzification.CheckWithGlobDirsParall) continue;
                    DirInd.Remove(DirInd[i]);
                    i--;
                }
            }
            else
            {
                for (var i = 0; i < DirInd.Count; i++)
                {
                    var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                    if (1 + cone.Axis.normalize().dotProduct(dir) < OverlappingFuzzification.CheckWithGlobDirsParall) continue;
                    DirInd.Remove(DirInd[i]);
                    i--;
                }
            }
            return true;
        }

        public static bool ConeCylinderOverlappingCheck(Cone cone, Cylinder cylinder, int re)
        {
            if (!cone.IsPositive || !cylinder.IsPositive) return false;
            return PosConePosCylinderOverlappingCheck(cone, cylinder, re);
        }

        private static bool PosConePosCylinderOverlappingCheck(Cone cone, Cylinder cylinder, int re)
        {
            var localProb = 0.0;
            PolygonalFace closestFace = new PolygonalFace();
            foreach (var fA in cone.Faces)
            {
                foreach (var fB in cylinder.Faces)
                {
                    var pP = TwoTrianglesParallelCheck(fA.Normal, fB.Normal);
                    var sP = TwoTrianglesSamePlaneCheck(fA, fB);
                    if (pP == 0 || sP == 0 || !TwoTriangleOverlapCheck(fA, fB)) continue;
                    var p = Math.Min(pP, sP);
                    if (p > localProb)
                    {
                        localProb = p;
                        closestFace = fB;
                    }
                    
                    if (localProb == 1)
                        break;
                }
                if (localProb == 1)
                    break;
            }
            if (localProb == 0 || double.IsNaN(localProb)) return false;
            if (localProb > MaxProb)
                MaxProb = localProb;

            if (re == 2)
            {
                for (var i = 0; i < DirInd.Count; i++)
                {
                    var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                    if (closestFace.Normal.dotProduct(dir) < OverlappingFuzzification.CheckWithGlobDirs)
                    {
                        DirInd.Remove(DirInd[i]);
                        i--;
                    }

                }
                return true;
            }
            for (var i = 0; i < DirInd.Count; i++)
            {
                var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                if (closestFace.Normal.dotProduct(dir) > OverlappingFuzzification.CheckWithGlobDirs)
                {
                    DirInd.Remove(DirInd[i]);
                    i--;
                }
            }
            return true;
        }

        public static bool FlatConeOverlappingCheck(Cone cone, Flat flat, int re)
        {
            if (!cone.IsPositive) return false;
            var localProb = 0.0;
            var r = new Random();
            var rndFaceB = flat.Faces[r.Next(flat.Faces.Count)];
            foreach (var coneFace in cone.Faces)
            {
                var pP = TwoTrianglesParallelCheck(coneFace.Normal, flat.Normal);
                var sP = TwoTrianglesSamePlaneCheck(coneFace, rndFaceB);
                if (pP == 0 || sP == 0) continue;
                // now check if they overlap or not
                foreach (var fFace in flat.Faces)
                {
                    if (!TwoTriangleOverlapCheck(coneFace, fFace)) continue;
                    var p = Math.Min(pP, sP);
                    if (p > localProb) localProb = p;
                    if (localProb == 1)
                        break;
                }
                if (localProb == 1) break;
            }
            if (localProb == 0) return false;
            if (localProb > MaxProb)
                MaxProb = localProb;
            // exactly like flat-flat
            if (re == 1)
            {
                for (var i = 0; i < DirInd.Count; i++)
                {
                    var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                    if (flat.Normal.dotProduct(dir) < OverlappingFuzzification.CheckWithGlobDirs)
                        DirInd.Remove(DirInd[i]);
                }
            }
            else
            {
                for (var i = 0; i < DirInd.Count; i++)
                {
                    var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                    if (flat.Normal.dotProduct(dir) > OverlappingFuzzification.CheckWithGlobDirs)
                        DirInd.Remove(DirInd[i]);
                }
            }
            return true;
        }

        public static bool SphereSphereOverlappingCheck(Sphere sphere1, Sphere sphere2)
        {
            if (!sphere1.IsPositive && !sphere2.IsPositive) return false;
            if (sphere1.IsPositive && sphere2.IsPositive)
                return PosSpherePosSphereOverlappingCheck(sphere1, sphere2);

            return PosSphereNegSphereOverlappingCheck(sphere1, sphere2);

        }

        public static bool CylinderSphereOverlappingCheck(Cylinder cylinder, Sphere sphere)
        {
            if (cylinder.IsPositive || !sphere.IsPositive) return false;
            return NegCylinderPosSphereOverlappingCheck(cylinder, sphere);
        }

        public static bool CylinderCylinderOverlappingCheck(Cylinder primitiveA, Cylinder primitiveB)
        {
            if (!primitiveA.IsPositive && primitiveB.IsPositive)
                return NegCylinderPosCylinderOverlappingCheck(primitiveA, primitiveB,1); 
            if (primitiveA.IsPositive && !primitiveB.IsPositive)
                return NegCylinderPosCylinderOverlappingCheck(primitiveB, primitiveA,2);
            if (primitiveA.IsPositive && primitiveB.IsPositive)
                return PosCylinderPosCylinderOverlappingCheck(primitiveA, primitiveB);
            if (!primitiveA.IsPositive && !primitiveB.IsPositive) return false;
            return false;
        }

        public static bool ConeConeOverlappingCheck(Cone cone1, Cone cone2)
        {

            if (!cone1.IsPositive && cone2.IsPositive)
                return NegConePosConeOverlappingCheck(cone1, cone2, 10); // 10: first one is reference
            if (cone1.IsPositive && !cone2.IsPositive)
                return NegConePosConeOverlappingCheck(cone2, cone1, 20); // 20: second one is reference
            if (cone1.IsPositive && cone2.IsPositive)
                return PosConePosConeOverlappingCheck(cone1, cone2);
            if (!cone1.IsPositive && !cone2.IsPositive) return false;
            return false;
        }

        private static bool PosConePosConeOverlappingCheck(Cone cone1, Cone cone2)
        {
            var localProb = 0.0;
            PolygonalFace closestFace = new PolygonalFace();
            foreach (var fA in cone1.Faces)
            {
                foreach (var fB in cone2.Faces)
                {
                    var pP = TwoTrianglesParallelCheck(fA.Normal, fB.Normal);
                    var sP = TwoTrianglesSamePlaneCheck(fA, fB);
                    if (pP == 0 || sP == 0 || !TwoTriangleOverlapCheck(fA, fB)) continue;

                    var p = Math.Min(pP, sP);
                    if (p > localProb)
                    {
                        localProb = p;
                        closestFace = fB;
                    }
                    if (localProb == 1)
                        break;
                }
                if (localProb == 1)
                    break;
            }
            if (localProb == 0 || double.IsNaN(localProb)) return false;
            if (localProb > MaxProb)
                MaxProb = localProb;
            for (var i = 0; i < DirInd.Count; i++)
            {
                var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                if (closestFace.Normal.dotProduct(dir) > OverlappingFuzzification.CheckWithGlobDirs)
                {
                    DirInd.Remove(DirInd[i]);
                    i--;
                }
            }
            return true;
        }

        private static bool NegConePosConeOverlappingCheck(Cone cone1, Cone cone2, int re)
        {
            var localProb = 0.0;
            // cone1 is negative cone and cone2 is positive cone.
            var p0 = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.ParralelLinesL,
                OverlappingFuzzification.ParralelLinesU,
                Math.Abs(cone1.Axis.normalize().dotProduct(cone2.Axis.normalize())) - 1);
            if (p0 > 0)
            {
                foreach (var f1 in cone1.Faces)
                {
                    foreach (var f2 in cone2.Faces)
                    {
                        var pP = TwoTrianglesParallelCheck(f1.Normal, f2.Normal);
                        var sP = TwoTrianglesSamePlaneCheck(f1, f2);
                        if (pP == 0 || sP == 0 || !TwoTriangleOverlapCheck(f1, f2)) continue;

                        var p = Math.Min(Math.Min(pP, sP),p0);
                        if (p > localProb)
                        {
                            localProb = p;
                        }
                        if (localProb == 1)
                            break;
                    }
                    if (localProb == 1)
                        break;
                }
            }
            if (localProb == 0 || double.IsNaN(localProb)) return false;
            if (localProb > MaxProb)
                MaxProb = localProb;
            // work with cone1: negative
            if (re == 20)
            {
                if (cone1.Axis.normalize().dotProduct(cone1.Faces[0].Normal) > 0)
                {
                    for (var i = 0; i < DirInd.Count; i++)
                    {
                        var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                        if (1 + cone1.Axis.normalize().dotProduct(dir) < Math.Abs(cone1.Aperture)) continue;
                        DirInd.Remove(DirInd[i]);
                        i--;
                    }
                }
                else
                {
                    for (var i = 0; i < DirInd.Count; i++)
                    {
                        var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                        if (1 - cone1.Axis.normalize().dotProduct(dir) < Math.Abs(cone1.Aperture)) continue;
                        DirInd.Remove(DirInd[i]);
                        i--;
                    }
                }
                return true;
            }

            if (cone1.Axis.normalize().dotProduct(cone1.Faces[0].Normal) > 0)
            {
                for (var i = 0; i < DirInd.Count; i++)
                {
                    var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                    if (1 - cone1.Axis.normalize().dotProduct(dir) < Math.Abs(cone1.Aperture)) continue;
                    DirInd.Remove(DirInd[i]);
                    i--;
                }
            }
            else
            {
                for (var i = 0; i < DirInd.Count; i++)
                {
                    var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                    if (1 + cone1.Axis.normalize().dotProduct(dir) < Math.Abs(cone1.Aperture)) continue;
                    DirInd.Remove(DirInd[i]);
                    i--;
                }
            }
            return true;
        }

        public static bool FlatFlatOverlappingCheck(Flat primitiveA, Flat primitiveB)
        {
            // Find the equation of a plane and see if all of the vertices of another primitive are in the plane or not (with a delta).
            // if yes, now check and see if these primitives overlapp or not.
            //primitiveA.Normal;
            // Take a random face and make a plane.
            var localProb = 0.0;
            var r = new Random();
            var rndFaceA = primitiveA.Faces[r.Next(primitiveA.Faces.Count)];
            var rndFaceB = primitiveB.Faces[r.Next(primitiveB.Faces.Count)];

            var pP = TwoTrianglesParallelCheck(primitiveA.Normal, primitiveB.Normal);
            var sP = TwoTrianglesSamePlaneCheck(rndFaceA, rndFaceB);
            if (pP > 0 && sP > 0)
            {
                // now check and see if any area of a is inside the boundaries of b or vicee versa
                foreach (var f1 in primitiveA.Faces)
                {
                    foreach (var f2 in primitiveB.Faces)
                    {
                        if (!TwoTriangleOverlapCheck(f1, f2)) continue;
                        var p = Math.Min(pP, sP);
                        if (p > localProb)
                        {
                            localProb = p;
                        }
                        if (localProb == 1)
                            break;
                    }
                    if (localProb == 1)
                        break;
                }
            }
            // if they overlap, update the directions
            if (localProb == 0 || double.IsNaN(localProb)) return false;
            if (localProb > MaxProb)
                MaxProb = localProb;

            // take one of the parts, for example A, then in the directions, remove the ones which make a positive dot product with the normal
            for (var i = 0; i < DirInd.Count; i++)
            {
                var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                if (primitiveA.Normal.dotProduct(dir) < OverlappingFuzzification.CheckWithGlobDirs)
                {
                    DirInd.Remove(DirInd[i]);
                    i--;
                }
            }
            return true;
        }

        public static bool FlatCylinderOverlappingCheck(Cylinder cylinder, Flat flat, int re)
        {
            // This must be a positive cylinder. There is no flat and negative cylinder. A cyliner, B flat
            // if there is any triangle on the cylinder with a parralel normal to the flat patch (and opposite direction). And then
            // if the distance between them is close to zero, then, check if they overlap o not.
            if (!cylinder.IsPositive) return false;
            var localProb = 0.0;
            //var r = new Random();
            //var rndFaceB = flat.Faces[r.Next(flat.Faces.Count)];
            var rndFaceB = flat.Faces[0];
            foreach (var cylFace in cylinder.Faces)
            {
                var pP = TwoTrianglesParallelCheck(cylFace.Normal, flat.Normal);
                var sP = TwoTrianglesSamePlaneCheck(cylFace, rndFaceB);
                if (pP > 0 && sP > 0)
                {
                    // now check if they overlap or not
                    foreach (var fFace in flat.Faces)
                    {
                        if (!TwoTriangleOverlapCheck(cylFace, fFace)) continue;
                        var p = Math.Min(pP, sP);
                        if (p > localProb)
                        {
                            localProb = p;
                        }
                        if (localProb == 1)
                            break;
                    }
                }
                if (localProb == 1)
                    break;
            }
            if (localProb == 0 || double.IsNaN(localProb)) return false;
            if (localProb > MaxProb)
                MaxProb = localProb;
            if (re == 1)
            {
                for (var i = 0; i < DirInd.Count; i++)
                {
                    var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                    if (flat.Normal.dotProduct(dir) < 0.0)//ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                    {
                        DirInd.Remove(DirInd[i]);
                        i--;
                    }
                }
            }
            else // ref == 2
            {
                for (var i = 0; i < DirInd.Count; i++)
                {
                    var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                    if (flat.Normal.dotProduct(dir) > 0.0)//ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                    {
                        DirInd.Remove(DirInd[i]);
                        i--;
                    }
                }
            }

            return true;
        }

        private static bool NegCylinderPosCylinderOverlappingCheck(Cylinder cylinder1, Cylinder cylinder2, int reference)
        {
            // this is actually positive cylinder with negative cylinder. primitiveA is negative cylinder and 
            // primitiveB is positive cylinder. Like a normal 
            // check the centerlines. Is the vector of the center lines the same? 
            // now check the radius. 

            // Update: I need to consider one more case: half cylinders : it's already considered
            var partialCylinder1 = false;
            var localProb = 0.0;
            var p0 = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.ParralelLinesL,
                OverlappingFuzzification.ParralelLinesU, Math.Abs(cylinder1.Axis.dotProduct(cylinder2.Axis)) - 1);
            if (p0 > 0)
            {
                // now centerlines are either parallel or the same. Now check and see if they are exactly the same
                // Take the anchor of B, using the axis of B, write the equation of the line. Check and see if 
                // the anchor of A is on the line equation.
                var t = new List<double>();
                for (var  i = 0; i < 3; i++)
                {
                    var axis = cylinder2.Axis[i];
                    if (Math.Abs(axis) < OverlappingFuzzification.EqualToZeroL) // if a, b or c is zero
                    {
                        if (Math.Abs(cylinder1.Anchor[i] - cylinder2.Anchor[i]) > OverlappingFuzzification.EqualToZero2L)
                            return false;
                    }
                    else
                        t.Add((cylinder1.Anchor[i] - cylinder2.Anchor[i]) / axis);
                }
                for (var i = 0; i < t.Count-1; i++)
                {
                    for (var j = i+1; j < t.Count; j++)
                    {
                        if (Math.Abs(t[i] - t[j]) > OverlappingFuzzification.PointOnLineL)
                            return false;
                    }
                }
                // Now check the radius
                var p1 = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.RadiusDifsL,
                    OverlappingFuzzification.RadiusDifsU, Math.Abs(cylinder1.Radius - cylinder2.Radius));
                if (p1 > 0)
                {
                    localProb = Math.Min(p0, p1);
                    foreach (var f1 in cylinder1.Faces)
                    {
                        foreach (var f2 in cylinder2.Faces.Where(f2=>TwoTriangleOverlapCheck(f1, f2)))
                        {
                            if (localProb == 1)
                                break;
                        }
                        if (localProb == 1)
                            break;
                    }
                }
            }
            if (localProb == 0 || double.IsNaN(localProb)) return false;
            if (localProb > MaxProb)
                MaxProb = localProb;
            // is cylinder1 (negative) half? 
            
            var sum = new [] {0.0, 0.0, 0.0};
            sum = cylinder1.Faces.Aggregate(sum, (current, face) => face.Normal.add(current));
            if (Math.Sqrt((Math.Pow(sum[0], 2.0)) + (Math.Pow(sum[1], 2.0)) + (Math.Pow(sum[2], 2.0))) > 6)
                partialCylinder1 = true;
            // only keep the directions along the axis of the cylinder. Keep the ones with the angle close to zero.
            if (!partialCylinder1)
            {
                for (var i = 0; i < DirInd.Count; i++)
                {
                    var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                    if (1 - Math.Abs(cylinder1.Axis.normalize().dotProduct(dir)) <
                        OverlappingFuzzification.CheckWithGlobDirsParall)
                        continue;
                    DirInd.Remove(DirInd[i]);
                    i--;
                }
            }
            else
            {
                if (reference == 1) // cylinder1(negative) is reference
                {
                    for (var i = 0; i < DirInd.Count; i++)
                    {
                        var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                        if ((1 - Math.Abs(cylinder1.Axis.normalize().dotProduct(dir)) >
                             OverlappingFuzzification.CheckWithGlobDirsParall) &&
                            cylinder1.Faces.All(
                                f => (Math.Abs(1 - dir.dotProduct(f.Normal))) > OverlappingFuzzification.ParralelLines2L))
                        {
                            DirInd.Remove(DirInd[i]);
                            i--;
                        }

                    }
                }
                else // cylinder2(positive) is reference
                {
                    for (var i = 0; i < DirInd.Count; i++)
                    {
                        var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                        if ((1 - Math.Abs(cylinder1.Axis.normalize().dotProduct(dir)) >
                             OverlappingFuzzification.CheckWithGlobDirsParall) &&
                            cylinder1.Faces.All(
                                f => (Math.Abs(1 + dir.dotProduct(f.Normal))) > OverlappingFuzzification.ParralelLines2L))
                        {
                            DirInd.Remove(DirInd[i]);
                            i--;
                        }
                    }
                }

            }
            return true;
        }

        private static bool PosCylinderPosCylinderOverlappingCheck(Cylinder cylinder1, Cylinder cylinder2)
        {
            var localProb = 0.0;
            PolygonalFace closestFace = new PolygonalFace();
            foreach (var fA in cylinder1.Faces)
            {
                foreach (var fB in cylinder2.Faces)
                {
                    var pP = TwoTrianglesParallelCheck(fA.Normal, fB.Normal);
                    var sP = TwoTrianglesSamePlaneCheck(fA, fB);
                    if (pP == 0 || sP == 0 || !TwoTriangleOverlapCheck(fA, fB)) continue;

                    var p = Math.Min(pP, sP);
                    if (p > localProb)
                    {
                        localProb = p;
                        closestFace = fB;
                    }
                    if (localProb == 1)
                        break;
                }
                if (localProb == 1)
                    break;
            }
            if (localProb == 0 || double.IsNaN(localProb)) return false;
            if (localProb > MaxProb)
                MaxProb = localProb;
            for (var i = 0; i < DirInd.Count; i++)
            {
                var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                if (closestFace.Normal.dotProduct(dir) > OverlappingFuzzification.CheckWithGlobDirs)
                {
                    DirInd.Remove(DirInd[i]);
                    i--;
                }
            }
            return true;
        }

        private static bool PosSphereNegSphereOverlappingCheck(Sphere primitiveA, Sphere primitiveB)
        {
            //postive(A)-negative(B)
            // if their centers are the same or really close
            // if their radius is equal or close
            var centerDif = primitiveA.Center.subtract(primitiveB.Center);
            var p0 = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.PointPointL,
                OverlappingFuzzification.PointPointU, Math.Abs(centerDif[0]));
            var p1 = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.PointPointL,
                OverlappingFuzzification.PointPointU, Math.Abs(centerDif[1]));
            var p2 = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.PointPointL,
                OverlappingFuzzification.PointPointU, Math.Abs(centerDif[2]));
            if (p0 > 0 && p1 > 0 && p2 < 0)
            {
                if (Math.Abs(primitiveA.Radius - primitiveB.Radius) < 0.001)
                    return true;
            }
            return false;
        }

        private static bool PosSpherePosSphereOverlappingCheck(Sphere sphere1, Sphere sphere2)
        {
            //postive(A)-Positive(B)
            // Seems to be really time consuming
            var localProb = 0.0;
            PolygonalFace closestFace = new PolygonalFace();
            var overlap = false;
            foreach (var fA in sphere1.Faces)
            {
                foreach (var fB in sphere2.Faces)
                {
                    var pP = TwoTrianglesParallelCheck(fA.Normal, fB.Normal);
                    var sP = TwoTrianglesSamePlaneCheck(fA, fB);
                    if (pP == 0 || sP == 0 || !TwoTriangleOverlapCheck(fA, fB)) continue;
                    var p = Math.Min(pP, sP);
                    if (p > localProb)
                    {
                        localProb = p;
                        closestFace = fB;
                    }
                    if (localProb == 1)
                        break;
                }
                if (localProb == 1)
                    break;
            }
            if (localProb == 0 || double.IsNaN(localProb)) return false;
            if (localProb > MaxProb)
                MaxProb = localProb;
            for (var i = 0; i < DirInd.Count; i++)
            {
                var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                if (closestFace.Normal.dotProduct(dir) > OverlappingFuzzification.CheckWithGlobDirs)
                {
                    DirInd.Remove(DirInd[i]);
                    i--;
                }
            }
            return true;
        }

        public static bool FlatSphereOverlappingCheck(Sphere sphere, Flat flat, int re)
        {
            if (!sphere.IsPositive) return false;
            var localProb = 0.0;
            // Positive sphere (primitiveA) and primitiveB is flat.
            // similar to flat-cylinder
            var r = new Random();
            var rndFaceB = flat.Faces[r.Next(flat.Faces.Count)];
            foreach (var sophFace in sphere.Faces)
            {
                var pP = TwoTrianglesParallelCheck(sophFace.Normal, flat.Normal);
                var sP = TwoTrianglesSamePlaneCheck(sophFace, rndFaceB);
                foreach (var fFace in flat.Faces)
                {
                    if (!TwoTriangleOverlapCheck(sophFace, fFace)) continue;
                    var p = Math.Min(pP, sP);
                    if (p > localProb)
                    {
                        localProb = p;
                    }
                    if (localProb == 1)
                        break;
                }
                if (localProb == 1)
                    break;
            }
            if (localProb == 0 || double.IsNaN(localProb)) return false;
            if (localProb > MaxProb)
                MaxProb = localProb;
            if (re == 1)
            {
                for (var i = 0; i < DirInd.Count; i++)
                {
                    var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                    if (flat.Normal.dotProduct(dir) < OverlappingFuzzification.CheckWithGlobDirs)
                        DirInd.Remove(DirInd[i]);
                }
            }
            else
            {
                for (var i = 0; i < DirInd.Count; i++)
                {
                    var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                    if (flat.Normal.dotProduct(dir) > OverlappingFuzzification.CheckWithGlobDirs)
                        DirInd.Remove(DirInd[i]);
                }
            }
            return true;
        }

        private static bool NegCylinderPosSphereOverlappingCheck(Cylinder cylinder, Sphere sphere)
        {
            // if the center of the sphere is on the cylinder centerline.
            // or again: two faces parralel, on the same plane and overlap
            var t1 = (sphere.Center[0] - cylinder.Anchor[0]) / (cylinder.Axis[0]);
            var t2 = (sphere.Center[1] - cylinder.Anchor[1]) / (cylinder.Axis[1]);
            var t3 = (sphere.Center[2] - cylinder.Anchor[2]) / (cylinder.Axis[2]);
            var p0 = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.PointOnLineL,
                OverlappingFuzzification.PointOnLineU, Math.Abs(t1 - t2));
            var p1 = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.PointOnLineL,
                OverlappingFuzzification.PointOnLineU, Math.Abs(t1 - t3));
            var p2 = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.PointOnLineL,
                OverlappingFuzzification.PointOnLineU, Math.Abs(t3 - t2));
            var pRadius = OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.RadiusDifsL,
                OverlappingFuzzification.RadiusDifsU, Math.Abs(cylinder.Radius - cylinder.Radius));
            var maxOverlappingProb = Math.Min(Math.Min(Math.Min(p0, p1), p2), pRadius);
            if (maxOverlappingProb > 0)
            {
                var localProb = 0.0;
                foreach (var f1 in cylinder.Faces)
                {
                    foreach (var f2 in sphere.Faces)
                    {
                        var pP = TwoTrianglesParallelCheck(f1.Normal, f2.Normal);
                        var sP = TwoTrianglesSamePlaneCheck(f1, f2);
                        if (pP == 0 || sP == 0 || !TwoTriangleOverlapCheck(f1, f2)) continue;

                        var p = Math.Min(pP, sP);
                        if (p > localProb) localProb = p;
                        if (localProb == 1)
                            break;
                    }
                    if (localProb == 1)
                        break;
                }
                maxOverlappingProb = Math.Min(maxOverlappingProb, localProb);
            }
            if (maxOverlappingProb == 0 || double.IsNaN(maxOverlappingProb)) return false;
            if (maxOverlappingProb > MaxProb)
                MaxProb = maxOverlappingProb;
            // the axis of the cylinder is the removal direction
            for (var i = 0; i < DirInd.Count; i++)
            {
                var dir = Geometric_Reasoning.StartProcess.Directions[DirInd[i]];
                if (1 - Math.Abs(cylinder.Axis.normalize().dotProduct(dir)) < OverlappingFuzzification.CheckWithGlobDirsParall) continue;
                DirInd.Remove(DirInd[i]);
                i--;
            }
            return true;
        }


        private static double TwoTrianglesSamePlaneCheck(PolygonalFace rndFaceA, PolygonalFace rndFaceB)
        {
            var q = rndFaceA.Center;
            var p = rndFaceB.Center;
            var pq = new[] { q[0] - p[0], q[1] - p[1], q[2] - p[2] };
            return OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.PlaneDistL,
                OverlappingFuzzification.PlaneDistU, Math.Abs(pq.dotProduct(rndFaceA.Normal)));
        }

        private static double TwoTrianglesParallelCheck(double[] aNormal, double[] bNormal)
        {
            // they must be parralel but in the opposite direction.
            // This function can be fuzzified in order to give a certainty to the connection
            return OverlappingFuzzification.FuzzyProbabilityCalculator(OverlappingFuzzification.ParralelLinesL,
                OverlappingFuzzification.ParralelLinesU, Math.Abs(bNormal.dotProduct(aNormal) + 1));

        }

        private static bool TwoTriangleOverlapCheck(PolygonalFace fA, PolygonalFace fB)
        {
            // this function is not really fuzziabled
            foreach (var edge in fA.Edges)
            {
                var edgeVector = edge.Vector;
                var third = fA.Vertices.Where(a => a != edge.From && a != edge.To).ToList()[0].Position;
                var checkVec = new[]
                {third[0] - edge.From.Position[0], third[1] - edge.From.Position[1], third[2] - edge.From.Position[2]};
                double[] cross1 = edgeVector.crossProduct(checkVec);
                var c = 0;
                foreach (var vertexB in fB.Vertices)
                {
                    var newVec = vertexB.Position.subtract(edge.From.Position);
                    var cross2 = edgeVector.crossProduct(newVec);
                    if ((Math.Sign(cross1[0]) != Math.Sign(cross2[0]) ||
                         (Math.Sign(cross1[0]) == 0 && Math.Sign(cross2[0]) == 0)) &&
                        (Math.Sign(cross1[1]) != Math.Sign(cross2[1]) ||
                         (Math.Sign(cross1[1]) == 0 && Math.Sign(cross2[1]) == 0)) &&
                        (Math.Sign(cross1[2]) != Math.Sign(cross2[2]) ||
                         (Math.Sign(cross1[2]) == 0 && Math.Sign(cross2[2]) == 0)))
                    {
                        c++;
                    }
                }
                if (c == 3)
                    return false;
            }
            return true;
        }
    }
}
