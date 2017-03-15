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
                var part = new PartProperties
                {
                    Name = solidName,
                    Mass = 0.0,
                    SurfaceArea = Program.Solids[solidName].Sum(s => s.SurfaceArea)/Math.Pow(Program.MeshMagnifier, 2.0),
                    Volume = Program.Solids[solidName].Sum(s => s.Volume)/Math.Pow(Program.MeshMagnifier, 3.0)
                };
                if (Program.Solids[solidName].Count > 1)
                {
                    part.FastenerCertainty = 0.0;
                    this.parts.Add(part);
                    continue;
                }
                if (FastenerDetector.Fasteners != null)
                {
                    var fastener = FastenerDetector.Fasteners.Where(f => solidName == f.Solid.Name).ToList();
                    if (fastener.Any())
                    {
                        part.FastenerCertainty = fastener[0].Certainty > 1 ? 1 : fastener[0].Certainty;
                        this.parts.Add(part);
                        continue;
                    }
                }
                if (FastenerDetector.Nuts != null)
                {
                    var nut = FastenerDetector.Nuts.Where(n => solidName == n.Solid.Name).ToList();
                    if (nut.Any())
                    {
                        part.FastenerCertainty = nut[0].Certainty > 1 ? 1 : nut[0].Certainty;
                        this.parts.Add(part);
                        continue;
                    }
                }
                if (FastenerDetector.Washers != null)
                {
                    var washer = FastenerDetector.Fasteners.Where(w => solidName == w.Solid.Name).ToList();
                    if (washer.Any())
                    {
                        part.FastenerCertainty = washer[0].Certainty > 1 ? 1 : washer[0].Certainty;
                        this.parts.Add(part);
                        continue;
                    }
                }
                var smallPart = FastenerDetector.PotentialFastener.Keys.Where(sp => solidName == sp.Name).ToList();
                if (smallPart.Any())
                {
                    part.FastenerCertainty = FastenerDetector.PotentialFastener[smallPart[0]] > 1
                        ? 1
                        : FastenerDetector.PotentialFastener[smallPart[0]];
                    this.parts.Add(part);
                    continue;
                }
                part.FastenerCertainty = 0.0;
                this.parts.Add(part);
            }
        }

    }
    /// <summary>
    /// Class PartProperties.
    /// vaolume is in cubic mm
    /// and mass is in grams
    /// Braxton, make sure the conversion is correct here.
    /// </summary>
    public class PartProperties
    {
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("mass")]
        public double Mass { get; set; }

        [XmlElement("volume")]
        public double Volume { get; set; }

        [XmlElement("surface_area")]
        public double SurfaceArea { get; set; }


        [XmlElement("fastener_certainty")]
        public double FastenerCertainty { get; set; }


    }
}
