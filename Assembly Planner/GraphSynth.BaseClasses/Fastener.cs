// ***********************************************************************
// Assembly         : Assembly Planner
// Author           : 
// Created          : 08-27-2015
//
// Last Modified By : Matt
// Last Modified On : 08-27-2015
// ***********************************************************************
// <copyright file="Fastener.cs" company="">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************
/// <summary>
/// The BaseClasses namespace.
/// </summary>

using System.Collections.Generic;

namespace Assembly_Planner.GraphSynth.BaseClasses
{
    /// <summary>
    /// Enum FastenerTypeEnum
    /// </summary>
    public enum FastenerTypeEnum
    {
        /// <summary>
        /// The bolt
        /// </summary>
        Bolt,
        /// <summary>
        /// The screw
        /// </summary>
        Screw,
        /// <summary>
        /// The other threaded post
        /// </summary>
        OtherThreadedPost,
        /// <summary>
        /// The pin
        /// </summary>
        Pin,
        /// <summary>
        /// The rivet
        /// </summary>
        Rivet,
        /// <summary>
        /// The clip
        /// </summary>
        Clip,
        /// <summary>
        /// The adhesive
        /// </summary>
        Adhesive,
        /// <summary>
        /// The weld
        /// </summary>
        Weld,
    }
    /// <summary>
    /// Class Fastener.
    /// </summary>
    public class Fastener
    {
        /// <summary>
        /// The removal direction
        /// </summary>
        public int RemovalDirection;
        /// <summary>
        /// The overall length
        /// </summary>
        public double OverallLength;
        /// <summary>
        /// The engaged length
        /// </summary>
        public double EngagedLength;
        /// <summary>
        /// The diameter
        /// </summary>
        public double Diameter;
        /// <summary>
        /// The mass
        /// </summary>
        public double Mass;
        /// <summary>
        /// The access position
        /// </summary>
        public double[] AccessPosition;
        /// <summary>
        /// The access direction
        /// </summary>
        public double[] AccessDirection;
        /// <summary>
        /// The access cylinder radius
        /// </summary>
        public double AccessCylinderRadius;
        /// <summary>
        /// The fastener type
        /// </summary>
        public FastenerTypeEnum FastenerType;
                /// <summary>
        /// The fastener type
        /// </summary>
        public List<int> PartsLockedByFastener = new List<int>();
        
    }
}