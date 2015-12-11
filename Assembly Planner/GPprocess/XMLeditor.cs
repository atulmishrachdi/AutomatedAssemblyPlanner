using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Xml.Linq;


namespace GPprocess
{
    public class XMLeditor
    {
        public static void ReadInEvaluationConstants(double[,] data, string filename)
        {

            XmlSerializer deserializer = new XmlSerializer(typeof(XMLeditor));
            TextReader reader = new StreamReader(filename);
            object obj = deserializer.Deserialize(reader);
            XMLeditor XmlData = (XMLeditor)obj;
            reader.Close();

            //{ return false; }
            return;
        }
        public static void WriteEvaluationConstants(object data, string filename)
        {


            XmlSerializer serializer = new XmlSerializer(data.GetType());
            TextWriter writer = new StreamWriter(filename);
            serializer.Serialize(writer, data);
            return;

        }
        public static void DODO()
        {

            string csv = File.ReadAllText("csvexample.csv");
            XDocument doc = ConversorCsvXml.ConvertCsvToXML(csv, new[] { "," });
            doc.Save("outputxml.xml");
            Console.WriteLine(doc.Declaration);
            foreach (XElement c in doc.Elements())
            {
                Console.WriteLine(c);
            }
            Console.ReadLine();
        
        }
        public static void DODO(bool y)
        {
            string csvString = @"GREAL,Great Lakes Food Market,Howard Snyder,Marketing Manager,(503) 555-7555,2732 Baker Blvd.,Eugene,OR,97403,USA
HUNGC,Hungry Coyote Import Store,Yoshi Latimer,Sales Representative,(503) 555-6874,City Center Plaza 516 Main St.,Elgin,OR,97827,USA
LAZYK,Lazy K Kountry Store,John Steel,Marketing Manager,(509) 555-7969,12 Orchestra Terrace,Walla Walla,WA,99362,USA
LETSS,Let's Stop N Shop,Jaime Yorres,Owner,(415) 555-5938,87 Polk St. Suite 5,San Francisco,CA,94117,USA";
            File.WriteAllText("cust.csv", csvString);

            // Read into an array of strings.
            string[] source = File.ReadAllLines("cust.csv");
            XElement cust = new XElement("Root",
                from str in source
                let fields = str.Split(',')
                select new XElement("Customer",
                    new XAttribute("CustomerID", fields[0]),
                    new XElement("CompanyName", fields[1]),
                    new XElement("ContactName", fields[2]),
                    new XElement("ContactTitle", fields[3]),
                    new XElement("Phone", fields[4]),
                    new XElement("FullAddress",
                        new XElement("Address", fields[5]),
                        new XElement("City", fields[6]),
                        new XElement("Region", fields[7]),
                        new XElement("PostalCode", fields[8]),
                        new XElement("Country", fields[9])
                    )
                )
            );
            Console.WriteLine(cust);
        }
        public static XDocument ConvertCsvToXML(string csvString, string[] separatorField)
        {
            //split the rows
            var sep = new[] { "\r\n" };
            string[] rows = csvString.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            //Create the declaration
            var xsurvey = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"));
            var xroot = new XElement("root"); //Create the root
            for (int i = 0; i < rows.Length; i++)
            {
                //Create each row
                if (i > 0)
                {
                    xroot.Add(rowCreator(rows[i], rows[0], separatorField));
                }
            }
            xsurvey.Add(xroot);
            return xsurvey;
        }

        /// <summary>
        /// Private. Take a csv line and convert in a row - var node
        /// with the fields values as attributes. 
        /// <param name=""row"" />csv row to process</param />
        /// <param name=""firstRow"" />First row with the fields names</param />
        /// <param name=""separatorField"" />separator string use in the csv fields</param />
        /// </summary></returns />
        private static XElement rowCreator(string row,
                       string firstRow, string[] separatorField)
        {

            string[] temp = row.Split(separatorField, StringSplitOptions.None);
            string[] names = firstRow.Split(separatorField, StringSplitOptions.None);
            var xrow = new XElement("row");
            for (int i = 0; i < temp.Length; i++)
            {
                //Create the element var and Attributes with the field name and value
                var xvar = new XElement("var",
                                        new XAttribute("name", names[i]),
                                        new XAttribute("value", temp[i]));
                xrow.Add(xvar);
            }
            return xrow;
        }
    }
}
