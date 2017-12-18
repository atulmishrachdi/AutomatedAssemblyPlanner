using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace Fastener_Detection
{
    class GearEdge
    {
        /// <summary>
        ///     Gets the From Vertex.
        /// </summary>
        /// <value>
        ///     From.
        /// </value>
        public Vertex From { get; internal set; }

        /// <summary>
        ///     Gets the To Vertex.
        /// </summary>
        /// <value>
        ///     To.
        /// </value>
        public Vertex To { get; internal set; }

        /// <summary>
        ///     Gets the length.
        /// </summary>
        /// <value>
        ///     The length.
        /// </value>
        public double Length { get; internal set; }

        /// <summary>
        ///     Gets the vector.
        /// </summary>
        /// <value>
        ///     The vector.
        /// </value>
        public double[] Vector { get; internal set; }

        internal static List<GearEdge> FromTVGLEdgeClassToGearEdgeClass(List<Edge> outerEdges)
        {
            return outerEdges.Select(outerEdge => new GearEdge
            {
                Length = outerEdge.Length,
                From = outerEdge.From,
                To = outerEdge.To,
                Vector = outerEdge.Vector
            }).ToList();
        }
    }
}
