using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Fastener_Detection
{
    [XmlRoot("parts_properties")]
    public class PartsProperties
    {
        [XmlElement("part_properties")]
        public List<PartProperties> parts { get; set; }

        public void GenerateProperties()
        {
            var solids = Geometric_Reasoning.StartProcess.Solids;
            this.parts = new List<PartProperties>();
            foreach (var solidName in solids.Keys)
            {
                var part = new PartProperties
                {
                    Name = solidName,
                    Mass = 0.0,
                    SurfaceArea = solids[solidName].Sum(s => s.SurfaceArea)/Math.Pow(Geometric_Reasoning.StartProcess.MeshMagnifier, 2.0),
                    Volume = solids[solidName].Sum(s => s.Volume)/Math.Pow(Geometric_Reasoning.StartProcess.MeshMagnifier, 3.0)
                };
                if (solids[solidName].Count > 1)
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
