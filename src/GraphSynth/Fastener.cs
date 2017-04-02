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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Xml.Serialization;
using BaseClasses.Representation;
using StarMathLib;
using TVGL;

namespace BaseClasses
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
        Weld
    }
    public enum NutType
    {
        /// <summary>
        /// The hex
        /// </summary>
        Hex,
        /// <summary>
        /// The wing
        /// </summary>
        Wing
    }
    public enum Tool
    {
        /// <summary>
        /// The Hex Wrench
        /// </summary>
        HexWrench,
        /// <summary>
        /// The Allen or Hex socket
        /// </summary>
        Allen,
        /// <summary>
        /// The simple flat screw driver
        /// </summary>
        FlatBlade,
        /// <summary>
        /// The phillips blade screw driver, 4 sided
        /// </summary>
        PhillipsBlade,
        /// <summary>
        /// The power screw driver
        /// </summary>
        powerscrewdriver
    }
    /// <summary>
    /// Class Fastener.
    /// </summary>
    public class Fastener : FastenerBase
    {
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
        /// The number of threads if available
        /// </summary>
        public int NumberOfThreads;
        /// <summary>
        /// The nuts if available
        /// </summary>
        public List<Nut> Nuts;
        /// <summary>
        /// The washers if available
        /// </summary>
        public List<Washer> Washer;
        /// <summary>
        /// The Parts Locked By Fastener
        /// </summary>
        public List<int> PartsLockedByFastener;
        /// <summary>
        /// The Secure time input for this fastener
        /// </summary>
        [XmlIgnore]
        public double[,] SecureModelInputs;
        /// <summary>
        /// for every part that is locked by this fastener, (if they are more than 2), I add one triangle that is touching the
        /// surface of the fastener. The order is the same as the order of the parts added to "PartsLockedByFastener". I can
        /// use this in order to understand which part is on the top, which one is on the bottom. In other words to understand
        /// the order of the PartsLockedByFastener. This will help later to install fastener at the right moment.
        /// </summary>
    }

    public class Nut : FastenerBase
    {
        /// <summary>
        /// The overall length
        /// </summary>
        public double OverallLength;
        /// <summary>
        /// The number of threads if available
        /// </summary>
        public int NumberOfThreads;
        /// <summary>
        /// The nut type
        /// </summary>
        public NutType NutType;
        /// <summary>
        /// The washers if available
        /// </summary>
        public List<Washer> Washer;
    }

    public class Washer : FastenerBase
    {

    }

}