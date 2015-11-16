using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using StarMathLib;
using TVGL;

namespace Assembly_Planner.GeometryReasoning
{
    public class OBB
    {
        // There are three different approaches to build the Oriented Bounding Box.
        //   1. Using points
        //   2. Using triangles
        //   3. Using convex hull
        // I will only implement the "using points" method here.

        internal static BoundingBox BuildUsingPoints(List<Vertex> points)
        {
            var mu = new[] {0.0, 0.0, 0.0};
            var C = new double[3, 3];
            // loop over the points to find the mean point location
            foreach (var point in points)
                mu = mu.add(point.Position.divide((double) points.Count));

            // loop over the points again to build the covariance matrix.  
            // Note that we only have to build terms for the upper 
            // trianglular portion since the matrix is symmetric
            double cxx = 0.0, cxy = 0.0, cxz = 0.0, cyy = 0.0, cyz = 0.0, czz = 0.0;
            foreach (var p in points.Select(point => point.Position))
            {
                cxx += p[0]*p[0] - mu[0]*mu[0];
                cxy += p[0]*p[1] - mu[0]*mu[1];
                cxz += p[0]*p[2] - mu[0]*mu[2];
                cyy += p[1]*p[1] - mu[1]*mu[1];
                cyz += p[1]*p[2] - mu[1]*mu[2];
                czz += p[2]*p[2] - mu[2]*mu[2];
            }
            // now build the covariance matrix
            C[0, 0] = cxx;
            C[0, 1] = cxy;
            C[0, 2] = cxz;
            C[1, 0] = cxy;
            C[1, 1] = cyy;
            C[1, 2] = cyz;
            C[2, 0] = cxz;
            C[2, 1] = cyz;
            C[2, 2] = czz;
            double[][] dirs;
            double volume;
            var verts = BuildFromCovarianceMatrix(C, points, out dirs, out volume);
            return new BoundingBox {CornerVertices = verts, Volume = volume, Directions = dirs};
        }

        internal static Vertex[] BuildFromCovarianceMatrix(double[,] C, List<Vertex> points, out double[][] eigenVecs, out double volume)
        {
            var eugVal = C.GetEigenValuesAndVectors(out eigenVecs);
            var r = eigenVecs[0];
            var u = eigenVecs[1];
            var f = eigenVecs[2];
            r.normalize();
            u.normalize();
            f.normalize();
            // set the rotation matrix using the eigvenvectors
            var mRot = new double[3][];
            mRot[0] = new double[3];
            mRot[1] = new double[3];
            mRot[2] = new double[3];
            mRot[0][0] = r[0];
            mRot[0][1] = u[0];
            mRot[0][2] = f[0];
            mRot[1][0] = r[1];
            mRot[1][1] = u[1];
            mRot[1][2] = f[1];
            mRot[2][0] = r[2];
            mRot[2][1] = u[2];
            mRot[2][2] = f[2];
            var minVec = new[] {double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity};
            var maxVec = new[] {double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity};
            foreach (var point in points)
            {
                var p = point.Position;
                var pPrime1 = p.dotProduct(r);
                var pPrime2 = p.dotProduct(u);
                var pPrime3 = p.dotProduct(f);
                if (pPrime1 < minVec[0]) minVec[0] = pPrime1;
                if (pPrime1 > maxVec[0]) maxVec[0] = pPrime1;
                if (pPrime2 < minVec[1]) minVec[1] = pPrime2;
                if (pPrime2 > maxVec[1]) maxVec[1] = pPrime2;
                if (pPrime3 < minVec[2]) minVec[2] = pPrime3;
                if (pPrime3 > maxVec[2]) maxVec[2] = pPrime3;
            }
            // set the center of the OBB to be the average of the 
            // minimum and maximum, and the extents be half of the
            // difference between the minimum and maximum
            var center = (minVec.add(maxVec)).divide(2.0);
            var pos = new[] {mRot[0].dotProduct(center), mRot[1].dotProduct(center), mRot[2].dotProduct(center)};
            var extent = (maxVec.subtract(minVec)).divide(2.0);
            var orientedBoundingBox = new[]
            {
                new Vertex(((pos.subtract(r.multiply(extent[0]))).subtract(u.multiply(extent[1]))).subtract(f.multiply(extent[2]))),
                new Vertex(((pos.add(r.multiply(extent[0]))).subtract(u.multiply(extent[1]))).subtract(f.multiply(extent[2]))),
                new Vertex(((pos.add(r.multiply(extent[0]))).subtract(u.multiply(extent[1]))).add(f.multiply(extent[2]))),
                new Vertex(((pos.subtract(r.multiply(extent[0]))).subtract(u.multiply(extent[1]))).add(f.multiply(extent[2]))),
                new Vertex(((pos.subtract(r.multiply(extent[0]))).add(u.multiply(extent[1]))).subtract(f.multiply(extent[2]))),
                new Vertex(((pos.add(r.multiply(extent[0]))).add(u.multiply(extent[1]))).subtract(f.multiply(extent[2]))),
                new Vertex(((pos.add(r.multiply(extent[0]))).add(u.multiply(extent[1]))).add(f.multiply(extent[2]))),
                new Vertex(((pos.subtract(r.multiply(extent[0]))).add(u.multiply(extent[1]))).add(f.multiply(extent[2])))
               
            };
            volume = 8 * extent[0] * extent[1] * extent[2];
            var clockWise = points.Any(
                p =>
                    p.Position.subtract(orientedBoundingBox[0].Position)
                        .dotProduct(
                            ((orientedBoundingBox[1].Position.subtract(orientedBoundingBox[0].Position)).crossProduct(
                                orientedBoundingBox[3].Position.subtract(orientedBoundingBox[0].Position))).normalize()) > 0);
            if (!clockWise)
            {
                // Make it CW:
                return new[]
                {
                    orientedBoundingBox[1], orientedBoundingBox[0], orientedBoundingBox[3], orientedBoundingBox[2],
                    orientedBoundingBox[5], orientedBoundingBox[4], orientedBoundingBox[7], orientedBoundingBox[6]
                };
            }
            return orientedBoundingBox;
        }

    }
}