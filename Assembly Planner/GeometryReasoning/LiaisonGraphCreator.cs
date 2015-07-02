using GraphSynth.Representation;
using System.IO;
using System.Linq;
using System.Xml;
using AssemblyEvaluation;
using GraphSynth;

namespace GeometryReasoning
{
    internal static class LiaisonGraphCreator
    {
        internal static void Make(designGraph graph, string filename)
        {
            //filename as path   
            if (!File.Exists(filename)) return;
            ReadGraphFromXML(graph, filename, "");
            if (graph.nodes.Count == 0)
                ReadGraphFromXML(graph, filename, "iFAB");
        }

        private static void ReadGraphFromXML(designGraph graph, string filename, string ns)
        {
            using (XmlReader reader = XmlReader.Create(new StreamReader(filename)))
            {
                string path = Path.GetDirectoryName(filename);
                reader.ReadToFollowing("assemblyDetails", ns);

                while (reader.Read())
                {

                    reader.ReadToFollowing("assemblyDetail", ns);
                    if (reader.NodeType != XmlNodeType.None && reader.Name != "assemblyDetails")
                    {

                        //read connection type

                        //find part1
                        reader.ReadToFollowing("part1", ns);
                        string part1 = reader.ReadElementContentAsString();

                        //find part2
                        reader.ReadToFollowing("part2", ns);
                        string part2 = reader.ReadElementContentAsString();

                        //find the nodes so we can check if an arc 
                        node n1 = graph.nodes.Find(n => n.name == part1);
                        if (n1 == null) n1 = addNodeFromXML(graph, part1, path, ns);
                        node n2 = graph.nodes.Find(n => n.name == part2);
                        if (n2 == null) n2 = addNodeFromXML(graph, part2, path, ns);
                        var a1 = new arc();

                        //if there is not an arc between n1 and n2 add an arc
                        if (!(n1.arcsFrom.Exists(a => a.To == n2) || n1.arcsTo.Exists(a => a.From == n2)))
                        {
                            SearchIO.output("Making arc between: " + n1.name + " & " + n2.name);
                            graph.addArc(a1, n1, n2);
                        }
                        reader.Read();
                        string seamtype = reader.Name.Replace(ns + ":", "");
                        a1.localLabels.Add(seamtype);
                        if (!seamtype.Equals("incidentalContact"))
                        {
                            //read other connection information until we hit the end of the connection element
                            reader.Read();
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.EndElement)
                                {
                                    break;
                                }
                                else
                                {
                                    string connectType = reader.Name.Replace(ns + ":", "");
                                    //Console.WriteLine(reader.Name + "::" + reader.ReadElementContentAsString().Replace(' ', '_'));
                                    a1.localLabels.Add(connectType + "::" + reader.ReadElementContentAsString().Replace(' ', '_'));
                                }

                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        private static node addNodeFromXML(designGraph graph, string nodeName, string path, string ns)
        {
            graph.addNode(nodeName);
            node n = graph.nodes[graph.nodes.Count() - 1];
            using (XmlReader partreader = XmlReader.Create(new StreamReader(path + "/" + nodeName + ".xml")))
            {
                partreader.ReadToFollowing("length", ns);
                double length = partreader.ReadElementContentAsDouble();
                partreader.ReadToFollowing("width", ns);
                double width = partreader.ReadElementContentAsDouble();
                partreader.ReadToFollowing("height", ns);
                double height = partreader.ReadElementContentAsDouble();
                partreader.ReadToFollowing("volume", ns);
                double volume = partreader.ReadElementContentAsDouble();
                partreader.ReadToFollowing("weight", ns);
                double weight = partreader.ReadElementContentAsDouble();

                partreader.ReadToFollowing("transformation", ns);
                partreader.MoveToAttribute("m34", ns);
                double m34 = partreader.ReadContentAsDouble();
                partreader.MoveToAttribute("m24", ns);
                double m24 = partreader.ReadContentAsDouble();
                partreader.MoveToAttribute("m14", ns);
                double m14 = partreader.ReadContentAsDouble();
                n.localVariables.Add(Constants.TRANSLATION);
                n.localVariables.Add(m14);//x?
                n.localVariables.Add(m24);//y?
                n.localVariables.Add(m34);//z?

                n.localVariables.Add(Constants.BOXDIMENSIONS);
                n.localVariables.Add(length);
                n.localVariables.Add(width);
                n.localVariables.Add(height);
                n.localVariables.Add(Constants.WEIGHT);
                n.localVariables.Add(weight);
                n.localVariables.Add(Constants.VOLUME);
                n.localVariables.Add(volume);
                partreader.Close();
                n.X = m14;
                n.Y = m24;
                n.Z = m34;

            }
            return n;
        }

    }
}

