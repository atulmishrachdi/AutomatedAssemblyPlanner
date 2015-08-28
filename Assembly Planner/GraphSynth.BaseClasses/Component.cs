using GraphSynth.Representation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
