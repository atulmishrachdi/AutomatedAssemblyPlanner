using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;
using TVGL;

namespace Assembly_Planner
{
    internal class PrimitivePrimitiveInteractions
    {
        public static int c1;
        internal static bool PrimitiveOverlap(List<PrimitiveSurface> solid1P, List<PrimitiveSurface> solid2P, List<int> dirInd, out List<PrimitiveSurface[]> overlappedPrimitives)
        {
            var overlap = false;
            var globlOverlappingCheck = dirInd.Count();
            c1 = 0;
            var c2 = 0;
            var lastCheck = new PrimitiveSurface[2];
            overlappedPrimitives = new List<PrimitiveSurface[]>();
            foreach (var primitiveA in solid1P)
            {
                foreach (var primitiveB in solid2P)
                {
                    if (overlap)
                        overlappedPrimitives.Add(lastCheck);
                    lastCheck = new[] { primitiveA, primitiveB };
                    overlap = false;
                    c2++;
                    // 1=flat, 2 =cylinder, 3 = sphere, 4= cone
                    if (primitiveA is Flat && primitiveB is Flat) 
                    {
                        overlap = FlatFlatOverlappingCheck((Flat)primitiveA, (Flat)primitiveB, dirInd); //
                        continue;
                    }
                    if (primitiveA is Flat && primitiveB is Cylinder)
                    {
                        overlap = FlatCylinderOverlappingCheck((Cylinder)primitiveB, (Flat)primitiveA, dirInd, 1);//
                        continue;
                    }
                    if (primitiveA is Flat && primitiveB is Sphere)
                    {
                        overlap = FlatSphereOverlappingCheck((Sphere)primitiveB, (Flat)primitiveA, dirInd, 1);//
                        continue;
                    }
                    if (primitiveA is Flat && primitiveB is Cone)
                    {
                        overlap = FlatConeOverlappingCheck((Cone)primitiveB, (Flat)primitiveA, dirInd, 1);//
                        continue;
                    }


                    if (primitiveA is Cylinder && primitiveB is Flat)
                    {
                        overlap = FlatCylinderOverlappingCheck((Cylinder)primitiveA, (Flat)primitiveB, dirInd, 2);//
                        continue;
                    }
                    if (primitiveA is Cylinder && primitiveB is Cylinder)
                    {
                        overlap = CylinderCylinderOverlappingCheck((Cylinder)primitiveA, (Cylinder)primitiveB, dirInd);
                        continue;
                    }
                    if (primitiveA is Cylinder && primitiveB is Sphere)
                    {
                        overlap = CylinderSphereOverlappingCheck((Cylinder)primitiveA, (Sphere)primitiveB, dirInd);
                        continue;
                    }
                    if (primitiveA is Cylinder && primitiveB is Cone)
                    {
                        overlap = ConeCylinderOverlappingCheck((Cone)primitiveB, (Cylinder)primitiveA, dirInd, 2);
                        continue;
                    }


                    if (primitiveA is Sphere && primitiveB is Flat)
                    {
                        overlap = FlatSphereOverlappingCheck((Sphere)primitiveA, (Flat)primitiveB, dirInd, 3);//
                        continue;
                    }
                    if (primitiveA is Sphere && primitiveB is Cylinder)
                    {
                        overlap = CylinderSphereOverlappingCheck((Cylinder)primitiveB, (Sphere)primitiveA, dirInd);
                        continue;
                    }
                    if (primitiveA is Sphere && primitiveB is Sphere)
                    {
                        overlap = SphereSphereOverlappingCheck((Sphere)primitiveA, (Sphere)primitiveB, dirInd);
                        continue;
                    }
                    if (primitiveA is Sphere && primitiveB is Cone)
                    {
                        overlap = ConeSphereOverlappingCheck((Cone)primitiveB, (Sphere)primitiveA, dirInd, 3);
                        continue;
                    }

                    if (primitiveA is Cone && primitiveB is Flat)
                    {
                        overlap = FlatConeOverlappingCheck((Cone)primitiveA, (Flat)primitiveB, dirInd, 4);//
                        continue;
                    }
                    if (primitiveA is Cone && primitiveB is Cylinder)
                    {
                        overlap = ConeCylinderOverlappingCheck((Cone)primitiveA, (Cylinder)primitiveB, dirInd, 4);
                        continue;
                    }
                    if (primitiveA is Cone && primitiveB is Sphere)
                    {
                        overlap = ConeSphereOverlappingCheck((Cone)primitiveA, (Sphere)primitiveB, dirInd, 4);
                        continue;
                    }
                    if (primitiveA is Cone && primitiveB is Cone)
                    {
                        overlap = ConeConeOverlappingCheck((Cone)primitiveA, (Cone)primitiveB, dirInd);
                        continue;
                    }
                }
            }
            if (globlOverlappingCheck > dirInd.Count)
            {
                return true;
            }
            return false;
        }
        
        public static bool ConeSphereOverlappingCheck(Cone cone, Sphere sphere, List<int> dirInd, int re)
        {
            if (!cone.IsPositive && sphere.IsPositive)
                return NegConePosSphereOverlappingCheck(cone, sphere, dirInd, re);
            return false;
        }

        private static bool NegConePosSphereOverlappingCheck(Cone cone, Sphere sphere, List<int> dirInd, int re)
        {
            var overlap = false;
            var t1 = (sphere.Center[0] - cone.Apex[0]) / (cone.Axis[0]);
            var t2 = (sphere.Center[1] - cone.Apex[1]) / (cone.Axis[1]);
            var t3 = (sphere.Center[2] - cone.Apex[2]) / (cone.Axis[2]);
            if (Math.Abs(t1 - t2) < ConstantsPrimitiveOverlap.PointOnLine &&
                Math.Abs(t1 - t3) < ConstantsPrimitiveOverlap.PointOnLine &&
                Math.Abs(t3 - t2) < ConstantsPrimitiveOverlap.PointOnLine)
            {
                foreach (var f1 in cone.Faces)
                {
                    foreach (var f2 in sphere.Faces.Where(f2 => TwoTrianglesParallelCheck(f1.Normal, f2.Normal)
                                                                && TwoTrianglesSamePlaneCheck(f1, f2)))
                    {
                        overlap = TwoTriangleOverlapCheck(f1, f2);
                        if (overlap)
                            break;
                    }
                    if (overlap)
                        break;
                }
            }
            if (!overlap) return false;
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
                    for (var i = 0; i < dirInd.Count; i++)
                    {
                        var dir = DisassemblyDirections.Directions[dirInd[i]];
                        if (1 + cone.Axis.normalize().dotProduct(dir) < ConstantsPrimitiveOverlap.CheckWithGlobDirsParall) continue;
                        dirInd.Remove(dirInd[i]);
                        i--;
                    }
                }
                else
                {
                    for (var i = 0; i < dirInd.Count; i++)
                    {
                        var dir = DisassemblyDirections.Directions[dirInd[i]];
                        if (1 - cone.Axis.normalize().dotProduct(dir) < ConstantsPrimitiveOverlap.CheckWithGlobDirsParall) continue;
                        dirInd.Remove(dirInd[i]);
                        i--;
                    }
                }
                return true;
            }
            
            if (cone.Axis.normalize().dotProduct(cone.Faces[0].Normal) > 0)
            {
                for (var i = 0; i < dirInd.Count; i++)
                {
                    var dir = DisassemblyDirections.Directions[dirInd[i]];
                    if (1 - cone.Axis.normalize().dotProduct(dir) < ConstantsPrimitiveOverlap.CheckWithGlobDirsParall) continue;
                    dirInd.Remove(dirInd[i]);
                    i--;
                }
            }
            else
            {
                for (var i = 0; i < dirInd.Count; i++)
                {
                    var dir = DisassemblyDirections.Directions[dirInd[i]];
                    if (1 + cone.Axis.normalize().dotProduct(dir) < ConstantsPrimitiveOverlap.CheckWithGlobDirsParall) continue;
                    dirInd.Remove(dirInd[i]);
                    i--;
                }
            }
            return true;
        }

        public static bool ConeCylinderOverlappingCheck(Cone cone, Cylinder cylinder, List<int> dirInd, int re)
        {
            if (!cone.IsPositive || !cylinder.IsPositive) return false;
            return PosConePosCylinderOverlappingCheck(cone, cylinder, dirInd, re);
        }

        private static bool PosConePosCylinderOverlappingCheck(Cone cone, Cylinder cylinder, List<int> dirInd, int re)
        {
            var overlap = false;
            foreach (var fA in cone.Faces)
            {
                foreach (var fB in cylinder.Faces.Where(fB => TwoTrianglesParallelCheck(fA.Normal, fB.Normal) &&
                    TwoTrianglesSamePlaneCheck(fA, fB)))
                {
                    overlap = TwoTriangleOverlapCheck(fA, fB);

                    if (!overlap) continue;
                    if (re == 2)
                    {
                        for (var i = 0; i < dirInd.Count; i++)
                        {
                            var dir = DisassemblyDirections.Directions[dirInd[i]];
                            if (fB.Normal.dotProduct(dir) < ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                            {
                                dirInd.Remove(dirInd[i]);
                                i--;
                            }
                                    
                        }
                        return true;
                    }
                    for (var i = 0; i < dirInd.Count; i++)
                    {
                        var dir = DisassemblyDirections.Directions[dirInd[i]];
                        if (fB.Normal.dotProduct(dir) > ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                        {
                            dirInd.Remove(dirInd[i]);
                            i--;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool FlatConeOverlappingCheck(Cone cone, Flat flat, List<int> dirInd, int re)
        {
            if (!cone.IsPositive) return false;
            var r = new Random();
            var rndFaceB = flat.Faces[r.Next(flat.Faces.Count)];
            var overlap = false;
            foreach (var coneFace in cone.Faces)
            {
                if (TwoTrianglesParallelCheck(coneFace.Normal, flat.Normal))
                {
                    if (TwoTrianglesSamePlaneCheck(coneFace, rndFaceB))
                    {
                        // now check if they overlap or not
                        foreach (var fFace in flat.Faces)
                        {
                            overlap = TwoTriangleOverlapCheck(coneFace, fFace);
                        }
                    }
                }
            }
            if (!overlap) return false;
            // exactly like flat-flat
            if (re == 1)
            {
                for (var i = 0; i < dirInd.Count; i++)
                {
                    var dir = DisassemblyDirections.Directions[dirInd[i]];
                    if (flat.Normal.dotProduct(dir) < ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                        dirInd.Remove(dirInd[i]);
                }
            }
            else
            {
                for (var i = 0; i < dirInd.Count; i++)
                {
                    var dir = DisassemblyDirections.Directions[dirInd[i]];
                    if (flat.Normal.dotProduct(dir) > ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                        dirInd.Remove(dirInd[i]);
                }
            }
            return true;
        }

        public static bool SphereSphereOverlappingCheck(Sphere sphere1, Sphere sphere2, List<int> dirInd)
        {
            if (!sphere1.IsPositive && !sphere2.IsPositive) return false;
            if (sphere1.IsPositive && sphere2.IsPositive)
                return PosSpherePosSphereOverlappingCheck(sphere1, sphere2, dirInd);

            return PosSphereNegSphereOverlappingCheck(sphere1, sphere2, dirInd);

        }

        public static bool CylinderSphereOverlappingCheck(Cylinder cylinder, Sphere sphere, List<int> dirInd)
        {
            if (cylinder.IsPositive || !sphere.IsPositive) return false;
            return NegCylinderPosSphereOverlappingCheck(cylinder, sphere, dirInd);
        }

        public static bool CylinderCylinderOverlappingCheck(Cylinder primitiveA, Cylinder primitiveB, List<int> dirInd)
        {
            if (!primitiveA.IsPositive && primitiveB.IsPositive)
                return NegCylinderPosCylinderOverlappingCheck(primitiveA, primitiveB, dirInd,1); 
            if (primitiveA.IsPositive && !primitiveB.IsPositive)
                return NegCylinderPosCylinderOverlappingCheck(primitiveB, primitiveA, dirInd,2);
            if (primitiveA.IsPositive && primitiveB.IsPositive)
                return PosCylinderPosCylinderOverlappingCheck(primitiveA, primitiveB, dirInd);
            if (!primitiveA.IsPositive && !primitiveB.IsPositive) return false;
            return false;
        }

        public static bool ConeConeOverlappingCheck(Cone cone1, Cone cone2, List<int> dirInd)
        {

            if (!cone1.IsPositive && cone2.IsPositive)
                return NegConePosConeOverlappingCheck(cone1, cone2, dirInd, 10); // 10: first one is reference
            if (cone1.IsPositive && !cone2.IsPositive)
                return NegConePosConeOverlappingCheck(cone2, cone1, dirInd, 20); // 20: second one is reference
            if (cone1.IsPositive && cone2.IsPositive)
                return PosConePosConeOverlappingCheck(cone1, cone2, dirInd);
            if (!cone1.IsPositive && !cone2.IsPositive) return false;
            return false;
        }

        private static bool PosConePosConeOverlappingCheck(Cone cone1, Cone cone2, List<int> dirInd)
        {
            var overlap = false;
            foreach (var fA in cone1.Faces)
            {
                foreach (var fB in cone2.Faces.Where(fB => TwoTrianglesParallelCheck(fA.Normal, fB.Normal) &&
                    TwoTrianglesSamePlaneCheck(fA, fB)))
                {
                    overlap = TwoTriangleOverlapCheck(fA, fB);
                    if (!overlap) continue;
                    for (var i = 0; i < dirInd.Count; i++)
                    {
                        var dir = DisassemblyDirections.Directions[dirInd[i]];
                        if (fB.Normal.dotProduct(dir) > ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                        {
                            dirInd.Remove(dirInd[i]);
                            i--;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private static bool NegConePosConeOverlappingCheck(Cone cone1, Cone cone2, List<int> dirInd, int re)
        {

            // cone1 is negative cone and cone2 is positive cone.
            var overlap = false;
            if (Math.Abs(cone1.Axis.normalize().dotProduct(cone2.Axis.normalize())) - 1 < ConstantsPrimitiveOverlap.ParralelLines)
            {
                foreach (var f1 in cone1.Faces)
                {
                    foreach (var f2 in cone2.Faces.Where(f2=>TwoTrianglesParallelCheck(f1.Normal, f2.Normal) 
                        && TwoTrianglesSamePlaneCheck(f1, f2) && TwoTriangleOverlapCheck(f1, f2)))
                    {
                        overlap = true;
                        break;
                    }
                    if (overlap) break;
                }
            }
            if (!overlap) return false;
            
            // work with cone1: negative
            if (re == 20)
            {
                if (cone1.Axis.normalize().dotProduct(cone1.Faces[0].Normal) > 0)
                {
                    for (var i = 0; i < dirInd.Count; i++)
                    {
                        var dir = DisassemblyDirections.Directions[dirInd[i]];
                        if (1 + cone1.Axis.normalize().dotProduct(dir) < Math.Abs(cone1.Aperture)) continue;
                        dirInd.Remove(dirInd[i]);
                        i--;
                    }
                }
                else
                {
                    for (var i = 0; i < dirInd.Count; i++)
                    {
                        var dir = DisassemblyDirections.Directions[dirInd[i]];
                        if (1 - cone1.Axis.normalize().dotProduct(dir) < Math.Abs(cone1.Aperture)) continue;
                        dirInd.Remove(dirInd[i]);
                        i--;
                    }
                }
                return true;
            }

            if (cone1.Axis.normalize().dotProduct(cone1.Faces[0].Normal) > 0)
            {
                for (var i = 0; i < dirInd.Count; i++)
                {
                    var dir = DisassemblyDirections.Directions[dirInd[i]];
                    if (1 - cone1.Axis.normalize().dotProduct(dir) < Math.Abs(cone1.Aperture)) continue;
                    dirInd.Remove(dirInd[i]);
                    i--;
                }
            }
            else
            {
                for (var i = 0; i < dirInd.Count; i++)
                {
                    var dir = DisassemblyDirections.Directions[dirInd[i]];
                    if (1 + cone1.Axis.normalize().dotProduct(dir) < Math.Abs(cone1.Aperture)) continue;
                    dirInd.Remove(dirInd[i]);
                    i--;
                }
            }
            return true;
        }

        public static bool FlatFlatOverlappingCheck(Flat primitiveA, Flat primitiveB, List<int> dirInd)
        {
            // Find the equation of a plane and see if all of the vertices of another primitive are in the plane or not (with a delta).
            // if yes, now check and see if these primitives overlapp or not.
            //primitiveA.Normal;
            bool overlap = false;
            var aFaces = primitiveA.Faces;
            var bFaces = primitiveB.Faces;
            // Take a random face and make a plane.
            var r = new Random();
            var rndFaceA = aFaces[r.Next(aFaces.Count)];
            var rndFaceB = bFaces[r.Next(bFaces.Count)];
            var c = 0;
            if (TwoTrianglesParallelCheck(primitiveA.Normal, primitiveB.Normal))
            {
                bool samePlane = TwoTrianglesSamePlaneCheck(rndFaceA, rndFaceB);
                if (samePlane)
                {
                    // now check and see if any area of a is inside the boundaries of b or vicee versa
                    foreach (var f1 in primitiveA.Faces)
                    {
                        foreach (var f2 in primitiveB.Faces)
                        {
                            if (TwoTriangleOverlapCheck(f1, f2))
                            {
                                overlap = true;
                                break;
                            }
                        }
                        if (overlap) break;
                    }
                }
            }
            // if they overlap, update the directions
            if (overlap)
            {
                // take one of the parts, for example A, then in the directions, remove the ones which make a positive dot product with the normal
                for (var i = 0; i < dirInd.Count; i++)
                {
                    var dir = DisassemblyDirections.Directions[dirInd[i]];
                    if (primitiveA.Normal.dotProduct(dir) < ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                    {
                        dirInd.Remove(dirInd[i]);
                        i--;
                    }
                }
            }
            return overlap;
        }

        public static bool FlatCylinderOverlappingCheck(Cylinder cylinder, Flat flat, List<int> dirInd, int re)
        {
            // This must be a positive cylinder. There is no flat and negative cylinder. A cyliner, B flat
            // if there is any triangle on the cylinder with a parralel normal to the flat patch (and opposite direction). And then
            // if the distance between them is close to zero, then, check if they overlap o not.
            if (!cylinder.IsPositive) return false;
            //var r = new Random();
            //var rndFaceB = flat.Faces[r.Next(flat.Faces.Count)];
            var rndFaceB = flat.Faces[0];
            var overlap = false;
            foreach (var cylFace in cylinder.Faces)
            {
                if (TwoTrianglesParallelCheck(cylFace.Normal, flat.Normal))
                {
                    if (TwoTrianglesSamePlaneCheck(cylFace, rndFaceB))
                    {
                        // now check if they overlap or not
                        foreach (var fFace in flat.Faces)
                        {
                            overlap = TwoTriangleOverlapCheck(cylFace, fFace);
                        }
                    }
                }
            }
            if (!overlap) return false;
            if (re == 1)
            {
                for (var i = 0; i < dirInd.Count; i++)
                {
                    var dir = DisassemblyDirections.Directions[dirInd[i]];
                    if (flat.Normal.dotProduct(dir) < 0.0)//ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                    {
                        dirInd.Remove(dirInd[i]);
                        i--;
                    }
                }
            }
            else // ref == 2
            {
                for (var i = 0; i < dirInd.Count; i++)
                {
                    var dir = DisassemblyDirections.Directions[dirInd[i]];
                    if (flat.Normal.dotProduct(dir) > 0.0)//ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                    {
                        dirInd.Remove(dirInd[i]);
                        i--;
                    }
                }
            }

            return true;
        }

        private static bool NegCylinderPosCylinderOverlappingCheck(Cylinder cylinder1, Cylinder cylinder2, List<int> dirInd, int reference)
        {
            // this is actually positive cylinder with negative cylinder. primitiveA is negative cylinder and 
            // primitiveB is positive cylinder. Like a normal 
            // check the centerlines. Is the vector of the center lines the same? 
            // now check the radius. 

            // Update: I need to consider one more case: half cylinders
            var overlap = true;
            var partialCylinder1 = false;
            var partialCylinder2 = false;
            if (Math.Abs(cylinder1.Axis.dotProduct(cylinder2.Axis)) - 1 < ConstantsPrimitiveOverlap.ParralelLines)
            {
                // now centerlines are either parallel or the same. Now check and see if they are exactly the same
                // Take the anchor of B, using the axis of B, write the equation of the line. Check and see if 
                // the anchor of A is on the line equation.
                var t = new List<double>();
                for (var  i = 0; i < 3; i++)
                {
                    var axis = cylinder2.Axis[i];
                    if (Math.Abs(axis) < ConstantsPrimitiveOverlap.EqualToZero) // if a, b or c is zero
                    {
                        if (Math.Abs(cylinder1.Anchor[i] - cylinder2.Anchor[i]) > ConstantsPrimitiveOverlap.EqualToZero2)
                        {
                            overlap = false;
                            break;
                        }
                    }
                    else
                        t.Add((cylinder1.Anchor[i] - cylinder2.Anchor[i]) / axis);
                }
                if (overlap)
                {
                    for (var i = 0; i < t.Count-1; i++)
                    {
                        for (var j = i+1; j < t.Count; j++)
                        {
                            if (Math.Abs(t[i] - t[j]) > ConstantsPrimitiveOverlap.PointOnLine)
                                overlap = false;
                        }
                    }
                }
                if (overlap)
                {
                    overlap = false;
                    // Now check the radius
                    if (Math.Abs(cylinder1.Radius - cylinder2.Radius) < ConstantsPrimitiveOverlap.RadiusDifs)
                    {
                        foreach (var f1 in cylinder1.Faces)
                        {
                            foreach (var f2 in cylinder2.Faces.Where(f2=>TwoTriangleOverlapCheck(f1, f2)))
                            {
                                overlap = true;
                                break;
                            }
                            if (overlap)
                                break;
                        }
                    }
                }
            }
            if (!overlap) return false;
            // is cylinder1 (negative) half? 
            var sum = new double[] {0.0, 0.0, 0.0};
            sum = cylinder1.Faces.Aggregate(sum, (current, face) => face.Normal.add(current));
            if (Math.Sqrt((Math.Pow(sum[0], 2.0)) + (Math.Pow(sum[1], 2.0)) + (Math.Pow(sum[2], 2.0))) > 6)
                partialCylinder1 = true;
            // only keep the directions along the axis of the cylinder. Keep the ones with the angle close to zero.
            if (!partialCylinder1)
            {
                for (var i = 0; i < dirInd.Count; i++)
                {
                    var dir = DisassemblyDirections.Directions[dirInd[i]];
                    if (1 - Math.Abs(cylinder1.Axis.normalize().dotProduct(dir)) <
                        ConstantsPrimitiveOverlap.CheckWithGlobDirsParall)
                        continue;
                    dirInd.Remove(dirInd[i]);
                    i--;
                }
            }
            else
            {
                if (reference == 1) // cylinder1(negative) is reference
                {
                    for (var i = 0; i < dirInd.Count; i++)
                    {
                        var dir = DisassemblyDirections.Directions[dirInd[i]];
                        if ((1 - Math.Abs(cylinder1.Axis.normalize().dotProduct(dir)) >
                             ConstantsPrimitiveOverlap.CheckWithGlobDirsParall) &&
                            cylinder1.Faces.All(
                                f => ( Math.Abs(1 -dir.dotProduct(f.Normal))) > ConstantsPrimitiveOverlap.ParralelLines2))
                        {
                            dirInd.Remove(dirInd[i]);
                            i--;
                        }

                    }
                }
                else // cylinder2(positive) is reference
                {
                    for (var i = 0; i < dirInd.Count; i++)
                    {
                        var dir = DisassemblyDirections.Directions[dirInd[i]];
                        if ((1 - Math.Abs(cylinder1.Axis.normalize().dotProduct(dir)) >
                             ConstantsPrimitiveOverlap.CheckWithGlobDirsParall) &&
                            cylinder1.Faces.All(
                                f => (Math.Abs(1 + dir.dotProduct(f.Normal))) > ConstantsPrimitiveOverlap.ParralelLines2))
                        {
                            dirInd.Remove(dirInd[i]);
                            i--;
                        }
                    }
                }

            }
            return true;
        }

        private static bool PosCylinderPosCylinderOverlappingCheck(Cylinder cylinder1, Cylinder cylinder2, List<int> dirInd)
        {
            var overlap = false;
            foreach (var fA in cylinder1.Faces)
            {
                foreach (var fB in cylinder2.Faces.Where(fB => TwoTrianglesParallelCheck(fA.Normal, fB.Normal) &&
                    TwoTrianglesSamePlaneCheck(fA, fB)))
                {
                    overlap = TwoTriangleOverlapCheck(fA, fB);
                    if (!overlap) continue;
                    for (var i = 0; i < dirInd.Count; i++)
                    {
                        var dir = DisassemblyDirections.Directions[dirInd[i]];
                        if (fB.Normal.dotProduct(dir) > ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                        {
                            dirInd.Remove(dirInd[i]);
                            i--;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private static bool PosSphereNegSphereOverlappingCheck(Sphere primitiveA, Sphere primitiveB, List<int> dirInd)
        {
            //postive(A)-negative(B)
            // if their centers are the same or really close
            // if their radius is equal or close
            double[] centerA = primitiveA.Center;
            double[] centerB = primitiveB.Center;
            if (Math.Abs(centerA[0] - centerB[0]) < ConstantsPrimitiveOverlap.PointPoint &&
                Math.Abs(centerA[1] - centerB[1]) < ConstantsPrimitiveOverlap.PointPoint &&
                Math.Abs(centerA[2] - centerB[2]) < ConstantsPrimitiveOverlap.PointPoint)
            {
                if (Math.Abs(primitiveA.Radius - primitiveB.Radius) < 0.001)
                    return true;
            }
            return false;
        }

        private static bool PosSpherePosSphereOverlappingCheck(Sphere sphere1, Sphere sphere2, List<int> dirInd)
        {
            //postive(A)-Positive(B)
            // Seems to be really time consuming
            var overlap = false;
            foreach (var fA in sphere1.Faces)
            {
                foreach (var fB in sphere2.Faces.Where(fB => TwoTrianglesParallelCheck(fA.Normal, fB.Normal) &&
                    TwoTrianglesSamePlaneCheck(fA, fB)))
                {
                    if (!TwoTriangleOverlapCheck(fA, fB)) continue;
                    for (var i = 0; i < dirInd.Count; i++)
                    {
                        var dir = DisassemblyDirections.Directions[dirInd[i]];
                        if (fB.Normal.dotProduct(dir) > ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                        {
                            dirInd.Remove(dirInd[i]);
                            i--;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool FlatSphereOverlappingCheck(Sphere sphere, Flat flat, List<int> dirInd, int re)
        {
            if (!sphere.IsPositive) return false;
            // Positive sphere (primitiveA) and primitiveB is flat.
            // similar to flat-cylinder
            var overlap = false;
            var r = new Random();
            var rndFaceB = flat.Faces[r.Next(flat.Faces.Count)];
            foreach (var sophFace in sphere.Faces.Where(sophFace => TwoTrianglesParallelCheck(sophFace.Normal, flat.Normal))
                                                     .Where(sophFace => TwoTrianglesSamePlaneCheck(sophFace, rndFaceB)))
            {
                foreach (var fFace in flat.Faces)
                {
                    if (TwoTriangleOverlapCheck(sophFace, fFace))
                        overlap = true;
                }
            }
            if (!overlap) return false;
            if (re == 1)
            {
                for (var i = 0; i < dirInd.Count; i++)
                {
                    var dir = DisassemblyDirections.Directions[dirInd[i]];
                    if (flat.Normal.dotProduct(dir) < ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                        dirInd.Remove(dirInd[i]);
                }
            }
            else
            {
                for (var i = 0; i < dirInd.Count; i++)
                {
                    var dir = DisassemblyDirections.Directions[dirInd[i]];
                    if (flat.Normal.dotProduct(dir) > ConstantsPrimitiveOverlap.CheckWithGlobDirs)
                        dirInd.Remove(dirInd[i]);
                }
            }
            return true;
        }

        private static bool NegCylinderPosSphereOverlappingCheck(Cylinder cylinder, Sphere sphere, List<int> dirInd)
        {
            // if the center of the sphere is on the cylinder centerline.
            // or again: two faces parralel, on the same plane and overlap
            var overlap = false;
            var t1 = (sphere.Center[0] - cylinder.Anchor[0]) / (cylinder.Axis[0]);
            var t2 = (sphere.Center[1] - cylinder.Anchor[1]) / (cylinder.Axis[1]);
            var t3 = (sphere.Center[2] - cylinder.Anchor[2]) / (cylinder.Axis[2]);
            if (Math.Abs(t1 - t2) < ConstantsPrimitiveOverlap.PointOnLine &&
                Math.Abs(t1 - t3) < ConstantsPrimitiveOverlap.PointOnLine &&
                Math.Abs(t3 - t2) < ConstantsPrimitiveOverlap.PointOnLine)
            {
                // Now check the radius
                if (Math.Abs(cylinder.Radius - cylinder.Radius) < ConstantsPrimitiveOverlap.RadiusDifs)
                {
                    foreach (var f1 in cylinder.Faces)
                    {
                        foreach (var f2 in sphere.Faces.Where(f2 => TwoTrianglesParallelCheck(f1.Normal, f2.Normal)
                                                                        && TwoTrianglesSamePlaneCheck(f1, f2)))
                        {
                            overlap = TwoTriangleOverlapCheck(f1, f2);
                        }
                    }
                }
            }
            if (!overlap) return false;
            // the axis of the cylinder is the removal direction
            for (var i = 0; i < dirInd.Count; i++)
            {
                var dir = DisassemblyDirections.Directions[dirInd[i]];
                if (1 - Math.Abs(cylinder.Axis.normalize().dotProduct(dir)) < ConstantsPrimitiveOverlap.CheckWithGlobDirsParall) continue;
                dirInd.Remove(dirInd[i]);
                i--;
            }
            return true;
        }


        private static bool TwoTrianglesSamePlaneCheck(PolygonalFace rndFaceA, PolygonalFace rndFaceB)
        {
            var q = rndFaceA.Center;
            var p = rndFaceB.Center;

            var pq = new[] { q[0] - p[0], q[1] - p[1], q[2] - p[2] };
            var d = Math.Abs(pq.dotProduct(rndFaceA.Normal));
            return d < ConstantsPrimitiveOverlap.PlaneDist;
        }

        private static bool TwoTrianglesParallelCheck(double[] aNormal, double[] bNormal)
        {
            // they must be parralel but in the opposite direction.
            return Math.Abs(bNormal.dotProduct(aNormal) + 1) < ConstantsPrimitiveOverlap.ParralelLines;
        }

        private static bool TwoTriangleOverlapCheck(PolygonalFace fA, PolygonalFace fB)
        {
            foreach (var edge in fA.Edges)
            {
                var edgeVector = edge.Vector;
                var third = fA.Vertices.Where(a => a != edge.From && a != edge.To).ToList()[0].Position;
                var checkVec = new[] {third[0] - edge.From.Position[0], third[1] - edge.From.Position[1], 
              third[2] - edge.From.Position[2]};
                double[] cross1 = edgeVector.crossProduct(checkVec);
                var c = 0;
                foreach (var vertexB in fB.Vertices)
                {
                    var newVec = new[]
                    {
                        vertexB.Position[0] - edge.From.Position[0], vertexB.Position[1] - edge.From.Position[1],
                        vertexB.Position[2] - edge.From.Position[2]
                    };
                    double[] cross2 = edgeVector.crossProduct(newVec);
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
                {
                    return false;
                }
            }
            return true;
        }
    }
}
