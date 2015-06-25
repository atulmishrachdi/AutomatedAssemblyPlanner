using GraphSynth.Representation;
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using StarMathLib;


namespace AssemblyEvaluation
{

    public class Part
    {
        [XmlAttribute]
        public string Name;

        [XmlIgnore]
        public List<string> PartNodes { get; protected set; }

        /// <summary>
        /// The mass of the part
        /// </summary>
        public double Mass;

        /// <summary>
        /// The volume of the part
        /// </summary>
        public double Volume;

        /// <summary>
        /// The average moment of inertia is really 
        /// </summary>
        [XmlIgnore]
        public double AvgMomentofInertia;

        /// <summary>
        /// The center of mass for the part
        /// </summary>
        [XmlIgnore]
        public Vertex CenterOfMass;

        [XmlIgnore]
        public ConvexHull<Vertex, DefaultConvexFace<Vertex>> CVXHull;

        public Part(string name, double mass, double volume, ConvexHull<Vertex, DefaultConvexFace<Vertex>> convexHull, Vertex centerOfMass)
        {
            Name = name;
            PartNodes = new List<string>();
            PartNodes.Add(name);
            Mass = mass;
            Volume = volume;
            //volume of sphere = (4/3)*Math.Pi*r*r*r
            var radius = Math.Pow(0.75 * volume / Math.PI, 1.0 / 3.0);
            AvgMomentofInertia = mass * radius * radius;
            CVXHull = convexHull;

            var com = new Vector(0, 0, 0);
            // find install direction by averaging all visible_DOF
            if (convexHull!=null)
                foreach (var pt in convexHull.Points)
                    com.AddInPlace(pt);
            //CenterOfMass = new Vertex(StarMath.divide(com.Position, convexHull.Points.Count(), 3));
            CenterOfMass = centerOfMass;
        }

        public Part()
        {
        }
    }

    public class SubAssembly : Part
    {
        public InstallAction Install;
        public List<Action> Actions;
        public InstallCharacterType InstallCharacter;
        private List<DefaultConvexFace<Vertex>> refFacesInCombined;

        public SubAssembly() { }
        public SubAssembly(Part refAssembly, Part movingAssembly, ConvexHull<Vertex, DefaultConvexFace<Vertex>> combinedCVXHull,
            InstallCharacterType InstallCharacter, List<DefaultConvexFace<Vertex>> refFacesInCombined)
        {
            Name = "subasm-" + Guid.NewGuid();
            PartNodes = new List<string>(refAssembly.PartNodes);
            PartNodes.AddRange(movingAssembly.PartNodes);
            Install = new InstallAction { Reference = refAssembly, Moving = movingAssembly };
            this.InstallCharacter = InstallCharacter;
            Mass = refAssembly.Mass + movingAssembly.Mass;
            Volume = refAssembly.Volume + movingAssembly.Volume;
            //volume of sphere = (4/3)*Math.Pi*r*r*r
            var radius = Math.Pow(0.75 * Volume / Math.PI, 1.0 / 3.0);
            AvgMomentofInertia = Mass * radius * radius;
            CVXHull = combinedCVXHull;
            this.refFacesInCombined = refFacesInCombined;
        }
        public SubAssembly(List<node> nodes, ConvexHull<Vertex, DefaultConvexFace<Vertex>> combinedCVXHull,
            double Mass, double Volume, Vertex centerOfMass)
        {
            Name = "subasm-" + Guid.NewGuid();
            PartNodes = nodes.Select(n => n.name).ToList();
            this.Mass = Mass ;
            this.Volume = Volume;
            //volume of sphere = (4/3)*Math.Pi*r*r*r
            var radius = Math.Pow(0.75 * Volume / Math.PI, 1.0 / 3.0);
            AvgMomentofInertia = Mass * radius * radius;
            CVXHull = combinedCVXHull;
            CenterOfMass = centerOfMass;
        }

    }

}
