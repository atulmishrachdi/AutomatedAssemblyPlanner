using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assembly_Planner.GeometryReasoning;
using StarMathLib;
using TVGL;

namespace Assembly_Planner
{
    public class BoundingCylinder
    {
        public double[] CenterLineVector;
        public double[] PointOnTheCenterLine;
        public double Radius;
        public double Length;
        public double Volume;
        public double[] PerpVector;
        // Idea:
        //   1. Take the OBB
        //   2. Take every side (3 sides)
        //   3. The center of each side with its normal, create the centerline of the cylinder. 
        //   4. The distance of the farthest vertex from the center line is the radius. 
        //   5. Three different cylinders, with three different volumes. The cylinder with  
        //      smallest volume is the bounding cylinder
        public static BoundingCylinder Run(TessellatedSolid solid)
        {
            var threeSides = ThreeSidesOfTheObb(solid);
            var bC = new BoundingCylinder();
            var minVolume = double.PositiveInfinity;
            foreach (var side in threeSides)
            {
                // every side will create a seperate bounding cylinder
                var faceCenters = FaceCenterFinder(side.Key);
                // Now I have the center, I have the vector of the centerline and also the length
                // find the radius:
                var radius = RadiusOfBoundingCylinder(solid, faceCenters, side.Key.Normal);
                var volume = Math.PI*Math.Pow(radius, 2.0)*side.Value;
                if (volume >=  minVolume) continue;
                minVolume = volume;
                bC.CenterLineVector = side.Key.Normal;
                bC.PointOnTheCenterLine = faceCenters;
                bC.PerpVector = (side.Key.Vertices[0].Position.subtract(faceCenters)).normalize();
                bC.Radius = radius;
                bC.Length = side.Value;
                bC.Volume = volume;
            }
            return bC;
        }


        private static Dictionary<PolygonalFace,double> ThreeSidesOfTheObb(TessellatedSolid solid)
        {
            // This is based on my own OBB function:
            // it returns a dictionary with size of three (3 sides). 
            //    Key: triangle, value: length of the potential cylinder 
            double[][] dir;
            bool clockWise;
            var obb = OBB.BuildUsingPoints(solid.Vertices.ToList(), out dir, out clockWise);
            var normal1 = ((obb[1].subtract(obb[0])).crossProduct(obb[3].subtract(obb[0]))).normalize(); // For 0,1,3
            var normal2 = ((obb[4].subtract(obb[0])).crossProduct(obb[1].subtract(obb[0]))).normalize(); // For 0,1,4
            var normal3 = ((obb[3].subtract(obb[0])).crossProduct(obb[4].subtract(obb[0]))).normalize(); // For 0,3,4
            var face1 = new PolygonalFace(new[] {new Vertex(obb[0]), new Vertex(obb[1]), new Vertex(obb[3])},
                clockWise ? normal1.multiply(-1.0) : normal1);
            var length1 = BasicGeometryFunctions.DistanceBetweenTwoVertices(obb[0], obb[4]);
            var face2 = new PolygonalFace(new[] {new Vertex(obb[0]), new Vertex(obb[1]), new Vertex(obb[4])},
                clockWise ? normal2.multiply(-1.0) : normal2);
            var length2 = BasicGeometryFunctions.DistanceBetweenTwoVertices(obb[0], obb[3]);
            var face3 = new PolygonalFace(new[] {new Vertex(obb[0]), new Vertex(obb[3]), new Vertex(obb[4])},
                clockWise ? normal3.multiply(-1.0) : normal3);
            var length3 = BasicGeometryFunctions.DistanceBetweenTwoVertices(obb[0], obb[1]);
            return new Dictionary<PolygonalFace, double> {{face1, length1}, {face2, length2}, {face3, length3}};
        }

        private static double[] FaceCenterFinder(PolygonalFace side)
        {
            var longestEdge = new Vertex[2];
            var maxLength = double.NegativeInfinity;
            for (var i = 0; i < side.Vertices.Count -1; i++)
            {
                for (var j = i+1; j < side.Vertices.Count; j++)
                {
                    var dis = BasicGeometryFunctions.DistanceBetweenTwoVertices(side.Vertices[i].Position,
                        side.Vertices[j].Position);
                    if (dis > maxLength) maxLength = dis;
                    longestEdge = new[] {side.Vertices[i], side.Vertices[j]};
                }
            }
            return (longestEdge[0].Position.add(longestEdge[1].Position)).divide(2.0);
        }

        private static double RadiusOfBoundingCylinder(TessellatedSolid solid, double[] faceCenters, double[] vector)
        {
            var maxDistance = double.NegativeInfinity;
            foreach (var vertex in solid.Vertices)
            {
                var dis = BasicGeometryFunctions.DistanceBetweenLineAndPoint(vector, faceCenters, vertex.Position);
                if (dis > maxDistance) maxDistance = dis;
            }
            return maxDistance;
        }
    }
}
