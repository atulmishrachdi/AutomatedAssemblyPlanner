using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geometric_Reasoning;
using StarMathLib;
using TVGL;

namespace Fastener_Detection
{
    internal class NutBoundingCylinderPartition
    {
        // This class can be merged with FastenerBoindingBoxPartition class. Need to spend ome time for it.

        internal int NumberOfPartitions { get; set; }
        internal TessellatedSolid Solid { get; set; }
        internal List<FastenerAndNutPartition> Partitions { get; set; }

        internal NutBoundingCylinderPartition(TessellatedSolid solid, int numberOfPartitions)
        {
            this.NumberOfPartitions = numberOfPartitions;
            this.Solid = solid;
            Vertex[] startingVerts;
            double[] partnCreationVect;
            var faceFromObbSide = GeometryFunctions.BoundingCylindertoFaceOnObbRespectiveSide(solid, out startingVerts, out partnCreationVect);
            this.Partitions = CreatePartitions(startingVerts, partnCreationVect, this.NumberOfPartitions);
            FastenerBoundingBoxPartition.SolidTrianglesOfPartitions(this.Partitions, solid, faceFromObbSide);
        }

        private List<FastenerAndNutPartition> CreatePartitions(Vertex[] startingVerts, double[] partnCreationVect, int numberOfPartitions)
        {
            var stepVector = partnCreationVect.divide((double)numberOfPartitions);
            var partis = new List<FastenerAndNutPartition>();
            for (var i = 0; i < NumberOfPartitions; i++)
            {
                var prt = new FastenerAndNutPartition
                {
                    Edge1 =
                        new[]
                        {
                            new Vertex(startingVerts[0].Position.add(stepVector.multiply(i))),
                            new Vertex(startingVerts[1].Position.add(stepVector.multiply(i)))
                        },
                    Edge2 =
                        new[]
                        {
                            new Vertex(startingVerts[0].Position.add(stepVector.multiply(i + 1))),
                            new Vertex(startingVerts[1].Position.add(stepVector.multiply(i + 1)))
                        }
                };
                if (i == 0)
                {
                    prt.Edge1 = new[]
                    {
                        new Vertex(startingVerts[0].Position.add(stepVector.multiply(-0.1))),
                        new Vertex(startingVerts[1].Position.add(stepVector.multiply(-0.1)))
                    };
                }
                if (i == NumberOfPartitions - 1)
                {
                    prt.Edge2 = new[]
                    {
                        new Vertex(startingVerts[0].Position.add(stepVector.multiply(i+1.1))),
                        new Vertex(startingVerts[1].Position.add(stepVector.multiply(i+1.1)))
                    };
                }
                partis.Add(prt);
            }
            return partis;
        }
    }
}
