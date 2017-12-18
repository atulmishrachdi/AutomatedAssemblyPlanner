
using System;
using System.IO;
using System.Xml.Serialization;
namespace BaseClasses.AssemblyEvaluation
{

    public enum ConnectType
    {
        rectilinear = 1,
        radial = 2,
        loose_contact = 3,
        unknown = 4,
        strongly_connected = 5,
        unknown_connection = 6
    };

    public enum InstallCharacterType
    {
        /// <summary>
        /// The moving part is fully inside the reference part
        /// </summary>
        MovingIsInsideReference = 3,
        /// <summary>
        /// The moving part is on the outside or near the surface of reference part
        /// </summary>
        MovingIsOnOutsideOfReference = 2,
        /// <summary>
        /// The moving part and the reference part are similar in size
        /// </summary>
        MovingReferenceSimiliar = 1,
        /// <summary>
        /// The reference part and the moving are similiar but they should be switched (moving is harder to move).
        /// </summary>
        ReferenceMovingSimiliarSwitch = -1,
        /// <summary>
        /// The reference part is on the outside or near the surface of moving part - they should be switched.
        /// </summary>
        ReferenceIsOnOutsideOfMoving = -2,
        /// <summary>
        /// The reference part is fully inside the moving part - they should be switched.
        /// </summary>
        ReferenceIsInsideMoving = -3,
        Unknown = 0
    }


    public class Constants
    {

        // Weights for differne evalutations
        //
        public static double Innerstabilityweight = 10;

        public static double Installtimeweight = 0;
        public static double Movingtimeweight = 0;
        public static double Rotatetimeweight = 0;



        public static Constants Values;
        public string CADAssemblyFileName = "input.sat";
        public string AssemblyXMLFileName = "input.xml";

        /* ants for determining which subassembly is moving and which is the reference. */
        public double CVXFormerFaceConfidence = 0.5;
        public double CVXOnInsideThreshold = 0.05;
        public double CVXOnOutsideThreshold = 0.5;



        // Global ants for gxml
        public int VISIBLE_DOF = -1000;         // visible DOF Tag
        public int INVISIBLE_DOF = -2000;        // invisible DOF Tag
        public int CLASH_LOCATION = -4000;      // clash location Tag
        public int CONCENTRIC_DOF = -3000;     // concentric DOF Tag
        public int EVALUATION = -1001;        // evaluation DOF Tag
        public int BOXDIMENSIONS = -5000; //??
        public int WEIGHT = -6000;
        public int VOLUME = -6001;
        public int CENTEROFMASS = -6005;
        public int TRANSLATION = -7000;
        public int ORDERSCORE = -8000;


        public double MaxForce = 180; // this is in Newtons, similiar to 40 lbs.
        public double StoppingDistance = 0.01; // in m (or 10 mm) distance to stop any object
        public double MaxTravelSpeed = 1.0; // 1 meter per second
        public double MaxInsertionSpeed = 0.2; // m per second
        public double NearlyParallelFace = 0.03; //equivalent to 88.85 degrees //0.02
        public double NearlyOnLine = 0.005; // about 178.8 degrees  0.0005
        public double MaxPathForInfeasibleInstall = 999999.99999;
        public double SameWithinError = 1e-9;
        public double MinInterfaceSuccessRate = 0.8;
        public double boltinsertSpeed = 2; // just a guess. the unit is mm/s

        //public static bool ReadInEvaluationants(string filename = "Evaluationants.xml")
        //{
        //    try
        //    {
        //        XmlSerializer deserializer = new XmlSerializer(typeof(ants));
        //        TextReader reader = new StreamReader(filename);
        //        object obj = deserializer.Deserialize(reader);
        //        ants XmlData = (ants)obj;
        //        reader.Close();
        //    }
        //    catch (Exception e) { return false; }
        //    return true;
        //}
        //public bool WriteEvaluationants(string filename = "Evaluationants.xml")
        //{
        //    try
        //    {
        //        XmlSerializer serializer = new XmlSerializer(typeof(ants));
        //        using (TextWriter writer = new StreamWriter(filename))
        //        {
        //            serializer.Serialize(writer, this);
        //        }
        //    }
        //    catch (Exception e) { return false; }
        //    return true;
        //}
        public static void SaveXML(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Create))
            {
                XmlSerializer XML = new XmlSerializer(typeof(Constants));
                XML.Serialize(stream, Constants.Values);
            }
        }

        public static void LoadXML(string filename)
        {
            // try
            //  {
            XmlSerializer deserializer = new XmlSerializer(typeof(Constants));
            TextReader reader = new StreamReader(filename);
            Constants.Values = (Constants)deserializer.Deserialize(reader);
            reader.Close();
            //return class1;
            // }
            // catch (Exception e) { return false; }

        }
    }
}
