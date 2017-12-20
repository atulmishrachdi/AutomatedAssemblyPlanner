// ***********************************************************************
// Assembly         : Assembly Planner
// Author           : NR, MC
// Created          : 08-27-2015
//
// Last Modified By : Matt
// Last Modified On : 08-27-2015
// ***********************************************************************
// <copyright file="Connection.cs" company="">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

using GraphSynth.Representation;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// The Representation namespace.
/// </summary>
namespace Assembly_Planner.GraphSynth.BaseClasses
{
    /// <summary>
    /// Enum ConnectionTypeEnum
    /// </summary>
    public enum ConnectionTypeEnum
    {
        /// <summary>
        /// The loose
        /// </summary>
        Loose,
        /// <summary>
        /// The tight
        /// </summary>
        Tight
        // just a brainstorm right now...
    }

    /// <summary>
    /// Class Connection.
    /// </summary>
    public class Connection : arc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="edge" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="fromNode">From node.</param>
        /// <param name="toNode">To node.</param>
        public Connection(string name, node fromNode, node toNode) : base(name)
        {
            From = fromNode;
            To = toNode;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="edge" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public Connection(string name) : base(name) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="edge" /> class.
        /// </summary>
        public Connection() { }
        /// <summary>
        /// Copies this instance of an arc and returns the copy.
        /// </summary>
        /// <returns>the copy of the arc.</returns>
        public override arc copy()
        {
            var copyOfEdge = new Connection();
            base.copy(copyOfEdge);

            return copyOfEdge;
        }

        /// <summary>
        /// The infinite directions
        /// </summary>
        public List<int> InfiniteDirections = new List<int>();

        /// <summary>
        /// The finite directions
        /// </summary>
        public List<int> FiniteDirections = new List<int>();

        /// <summary>
        /// The fasteners
        /// </summary>
        public List<Fastener> Fasteners = new List<Fastener>();

        /// <summary>
        /// The certainty
        /// </summary>
        public double Certainty;

        /// <summary>
        /// The connection type
        /// </summary>
        public ConnectionTypeEnum ConnectionType;


        [XmlIgnore]
        public List<List<double[]>> UnionAreaPoints = new List<List<double[]>>();
        /// <summary>
        /// FromPartForeceReactionDirectons
        /// </summary>

        [XmlIgnore]
        public List<double[]> FromPartReactionForeceDirections = new List<double[]>();
        /// <summary>
        /// ToPartForeceReactionDirectons
        /// </summary>
        [XmlIgnore]
        public List<double[]> ToPartReactionForeceDirections = new List<double[]>();
        /// <summary>
        /// Suppoted Area Points Center
        /// </summary>
        [XmlIgnore]
        public List<double[]> UnionAreaPointsCenter = new List<double[]>();

        /// <summary>
        /// Connected cylindar axes vectors (indicated by two points)
        [XmlIgnore]
        public List<double[,]> Axes = new List<double[,]>();

    }
}