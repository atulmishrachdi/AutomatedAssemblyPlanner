using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Assembly_Planner
{
    [XmlRoot("parts_properties")]
    public class PartsProperties
    {
        [XmlElement("part_properties")]
        public List<PartProperties> parts { get; set; }

        internal void GenerateProperties()
        {
            this.parts = new List<PartProperties>();
            foreach (var solidName in Program.Solids.Keys)
            {
                var part = new PartProperties {Name = solidName, Mass = 0.0, Density = 0.0};
                if (Program.Solids[solidName].Count > 1)
                {
                    part.fastenerCertainty = 0.0;
                    this.parts.Add(part);
                    continue;
                }
                if (FastenerDetector.Fasteners != null)
                {
                    var fastener = FastenerDetector.Fasteners.Where(f => solidName == f.Solid.Name).ToList();
                    if (fastener.Any())
                    {
                        part.fastenerCertainty = fastener[0].Certainty;
                        this.parts.Add(part);
                        continue;
                    }
                }
                if (FastenerDetector.Nuts != null)
                {
                    var nut = FastenerDetector.Nuts.Where(n => solidName == n.Solid.Name).ToList();
                    if (nut.Any())
                    {
                        part.fastenerCertainty = nut[0].Certainty;
                        this.parts.Add(part);
                        continue;
                    }
                }
                if (FastenerDetector.Washers != null)
                {
                    var washer = FastenerDetector.Fasteners.Where(w => solidName == w.Solid.Name).ToList();
                    if (washer.Any())
                    {
                        part.fastenerCertainty = washer[0].Certainty;
                        this.parts.Add(part);
                        continue;
                    }
                }
                if (FastenerDetector.PotentialFastener != null)
                {
                    var smallPart = FastenerDetector.PotentialFastener.Where(f => solidName == f.Name).ToList();
                    if (smallPart.Any())
                    {
                        part.fastenerCertainty = 0.1;
                        this.parts.Add(part);
                        continue;
                    }
                }
                part.fastenerCertainty = 0.0;
                this.parts.Add(part);
            }
        }

    }
    public class PartProperties
    {
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("mass")]
        public double Mass { get; set; }

        [XmlElement("density")]
        public double Density { get; set; }

        [XmlElement("fastener_certainty")]
        public double fastenerCertainty { get; set; }


    }
}
