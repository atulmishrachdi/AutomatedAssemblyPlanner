using System;
using System.Collections.Generic;
using System.Text;
using GraphSynth;
using TVGL;
using System.IO;
using System.Xml;
using System.Xml.Serialization;


[Serializable]
[XmlRoot]
public struct partTableEntry
{
    public string Name;
    public bool IsFastener;
    public double Certainty;
    public double Volume;
    public bool IsHollow;
    public double Thickness;
    public double Density;
    public double Mass;

};

namespace Assembly_Planner
{
    class PartTableSaving
    {        
        public static void save(Dictionary<string, List<TessellatedSolid>> solids)
        {
            List<partTableEntry> theTable=new List<partTableEntry>();
            partTableEntry theEntry;
            foreach(KeyValuePair<string, List<TessellatedSolid>> entry in solids)
            {
                theEntry = new partTableEntry();
                theEntry.Name = entry.Key;
                theEntry.IsFastener = false;
                theEntry.Certainty = 0.5;
                theEntry.Volume = 0;
                theEntry.IsHollow = false;
                theEntry.Thickness = 0;
                theEntry.Density = 0;
                theEntry.Mass = 0;
                theTable.Add(theEntry);
            }
            SaveTable("partsTable.xml", theTable);
        }

        protected static string SerializeGraphToXml(List<partTableEntry> solids)
        {
            try
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    NewLineOnAttributes = true,
                    CloseOutput = true,
                    OmitXmlDeclaration = true
                };
                var saveString = new StringBuilder();
                var saveXML = XmlWriter.Create(saveString, settings);
                var graphSerializer = new XmlSerializer(typeof(List<partTableEntry>));
                graphSerializer.Serialize(saveXML, solids);
                return (saveString.ToString());
            }
            catch (Exception ioe)
            {
                SearchIO.output("***XML Serialization Error***");
                SearchIO.output(ioe.ToString());
                return null;
            }
        }


        protected static void SaveTable(string filename, List<partTableEntry> solids)
        {
            StreamWriter tableWriter = null;
            try
            {
                tableWriter = new StreamWriter(filename);
                var s = SerializeGraphToXml(solids);
                if (s != null) tableWriter.Write(s);
            }
            catch (FileNotFoundException fnfe)
            {
                SearchIO.output("***Error Writing to File***");
                SearchIO.output(fnfe.ToString());
            }
            finally
            {
                if (tableWriter != null) tableWriter.Close();
            }
        }



    }
}


