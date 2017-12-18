using GraphSynth.Representation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Assembly_Planner.GraphSynth.BaseClasses
{
    public class Component : node
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "Component" /> class.
        /// </summary>
        /// <param name = "name">The name.</param>
        public Component(string name)
            : base(name)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Component" /> class.
        /// </summary>
        public Component()
        {
        }

        /// <summary>
        ///   Copies this instance.
        /// </summary>
        /// <returns></returns>
        public override node copy()
        {
            var copyOfVertex = new Component(name);
            base.copy(copyOfVertex);
            return copyOfVertex;
        }

        /// <summary>
        /// The mass
        /// </summary>
        public double Mass;

        /// <summary>
        /// The volume
        /// </summary>
        public double Volume;

        /// <summary>
        /// The center of mass
        /// </summary>
        public double[] CenterOfMass;

        /// <summary>
        /// Pins connected to the component 
        /// </summary>
        public List<Fastener> Pins = new List<Fastener>();
        
        /// <summary>
        /// The rotational inertia
        /// </summary>
        public double RotationalInertia;

        [XmlIgnore]
        public Dictionary<string, List<int>> RemovealDirectionsforEachPart = new Dictionary<string, List<int>>();
        /// <summary>
        /// The dictionary of stability scores and DOF with different connection.
        /// </summary>
        [XmlIgnore]
        public Dictionary<HashSet<string>, List<double>> SingleStabilityAndDOF = new Dictionary<HashSet<string>, List<double>>();
    }
}
