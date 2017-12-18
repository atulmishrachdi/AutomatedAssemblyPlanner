using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assembly_Planner;
using StarMathLib;
using TVGL;
using Vertex = TVGL.Vertex;

namespace Assembly_Planner
{
    internal class FastenerBoundingBoxPartition
    {
        internal int NumberOfPartitions { get; set; }
        internal TessellatedSolid Solid { get; set; }
        internal List<FastenerAndNutPartition> Partitions { get; set; }

        internal FastenerBoundingBoxPartition(TessellatedSolid solid, PolygonalFace faceFromLongestSide, int numberOfPartitions)
        {
            this.NumberOfPartitions = numberOfPartitions;
            this.Solid = solid;
            this.Partitions = CreatePartitions(faceFromLongestSide, this.NumberOfPartitions);
            SolidTrianglesOfPartitions(this.Partitions, solid, faceFromLongestSide);
        }

        internal static void SolidTrianglesOfPartitions(List<FastenerAndNutPartition> partitions, TessellatedSolid solid, PolygonalFace faceFromLongestSide)
        {
            foreach (var vertex in solid.Vertices)
            {
                var ray = new Ray(new Vertex(vertex.Position), faceFromLongestSide.Normal);
                var intersectionPoint = GeometryFunctions.RayIntersectionPointWithFace(ray, faceFromLongestSide);
                var chosenPrtn = PartitionOfThePoint(partitions, intersectionPoint);
                chosenPrtn.VerticesOfSolidInPartition.Add(vertex);
            }
            foreach (var face in solid.Faces)
            {
                var inds = new List<int>();
                foreach (var fV in face.Vertices)
                {
                    var prt = partitions.First(p => p.VerticesOfSolidInPartition.Contains(fV));
                    inds.Add(partitions.IndexOf(prt));
                }
                for (var i = inds.Min(); i <= inds.Max(); i++)
                    partitions[i].FacesOfSolidInPartition.Add(face);
            }
        }

        private List<FastenerAndNutPartition> CreatePartitions(PolygonalFace faceFromLongestSide, int numberOfPartitions)
        {
            var sortedEdges = GeometryFunctions.SortedEdgesOfTriangle(faceFromLongestSide);
            var cornerVer = sortedEdges[0].First(sortedEdges[1].Contains);
            var otherVerShertestEdge = sortedEdges[0].First(a => a != cornerVer);
            var partitionGeneratorDirection = sortedEdges[1].First(a => a != cornerVer).Position.subtract(cornerVer.Position);
            var stepVector = partitionGeneratorDirection.divide((double)numberOfPartitions);
            var partis = new List<FastenerAndNutPartition>();
            for (var i = 0; i < NumberOfPartitions; i++)
            {
                var prt = new FastenerAndNutPartition
                {
                    Edge1 =
                        new[]
                        {
                            new Vertex(cornerVer.Position.add(stepVector.multiply(i))),
                            new Vertex(otherVerShertestEdge.Position.add(stepVector.multiply(i)))
                        },
                    Edge2 =
                        new[]
                        {
                            new Vertex(cornerVer.Position.add(stepVector.multiply(i + 1))),
                            new Vertex(otherVerShertestEdge.Position.add(stepVector.multiply(i + 1)))
                        }
                };
                if (i == 0)
                {
                    prt.Edge1 = new[]
                    {
                        new Vertex(cornerVer.Position.add(stepVector.multiply(-0.1))),
                        new Vertex(otherVerShertestEdge.Position.add(stepVector.multiply(-0.1)))
                    };
                }
                if (i == NumberOfPartitions-1)
                {
                    prt.Edge2 = new[]
                    {
                        new Vertex(cornerVer.Position.add(stepVector.multiply(i+1.1))),
                        new Vertex(otherVerShertestEdge.Position.add(stepVector.multiply(i+1.1)))
                    };
                }
                partis.Add(prt);
            }
            return partis;
        }


        internal static FastenerAndNutPartition PartitionOfThePoint(List<FastenerAndNutPartition> partitions, double[] point)
        {
            FastenerAndNutPartition chosenPrtn = null;
            var sumDisToEdgs = double.PositiveInfinity;
            foreach (var prtn in partitions)
            {
                var dis1 =
                    GeometryFunctions.DistanceBetweenLineAndVertex(
                        prtn.Edge1[0].Position.subtract(prtn.Edge1[1].Position), prtn.Edge1[0].Position,
                        point);
                var dis2 =
                    GeometryFunctions.DistanceBetweenLineAndVertex(
                        prtn.Edge2[0].Position.subtract(prtn.Edge2[1].Position), prtn.Edge2[0].Position,
                        point);
                var sum = dis1 + dis2;
                if (sum < sumDisToEdgs)
                {
                    sumDisToEdgs = sum;
                    chosenPrtn = prtn;
                }
            }
            return chosenPrtn;
        }
    }


    internal class FastenerAndNutPartition
    {
        internal HashSet<PolygonalFace> FacesOfSolidInPartition = new HashSet<PolygonalFace>();
        internal HashSet<Vertex> VerticesOfSolidInPartition = new HashSet<Vertex>();
        internal Vertex[] Edge1;
        internal Vertex[] Edge2;
    }
}
