using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyEvaluation
{
    class NewFootprintFacesClass
    {
        public double Name;
        public double[] Normal = new double[3];
        public Vertex COMProjection = new Vertex(0, 0, 0);
        public List<Vertex> externalVertices = new List<Vertex>();
        public List<Vertex> externalCoupleVertices = new List<Vertex>();
        public List<double> DistanceBetweenCOMProjAndEachnode = new List<double>();
        public double minDistCOMProjToNearestEdge;
        public double hightOfCOM;
        public List<double> adjacentFaces = new List<double>();
        public bool isComInsideFace = true;
        public double InsertionDirectionScore;
        public double RotationCostScore;
        public double RefStabilityScore;
        public double ComStabilityScore;
    }
}
