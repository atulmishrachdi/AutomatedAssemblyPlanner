using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using System.Xml.Serialization;
using GraphSynth.Representation;
using AssemblyEvaluation;

namespace Assembly_Planner
{
    [Serializable]
    public class ProgramState
    {

        

        public List<double> DegreeOfFreedoms;
        public List<double> StablbiblityScores;

        [XmlIgnore]
        public Dictionary<string, List<TessellatedSolid>> Solids;
        [XmlIgnore]
        public Dictionary<string, List<TessellatedSolid>> SolidsNoFastener;
        [XmlIgnore]
        public Dictionary<string, List<TessellatedSolid>> SolidsNoFastenerSimplified;
        [XmlIgnore]
        public Dictionary<string, List<TessellatedSolid>> SimplifiedSolids;

        public List<XMLPair<string, List<SaveableSolid>>> SaveSolids;
        public List<XMLPair<string, List<SaveableSolid>>> SaveSolidsNoFastener;
        public List<XMLPair<string, List<SaveableSolid>>> SaveSolidsNoFastenerSimplified;
        public List<XMLPair<string, List<SaveableSolid>>> SaveSimplifiedSolids;

        [XmlIgnore]
        public Dictionary<string, double> SolidsMass;

        public SaveableDict<string, double> SaveSolidsMass;

        public SolidKeySaveDict<BoundingBox> BBoxes;
        public SolidKeySaveDict<BoundingCylinder> BCyls;

        [XmlIgnore]
        public SolidKeySaveDict<Partition[]> Parts;
        [XmlIgnore]
        public SolidKeySaveDict<PartitionAABB[]> PartsAB;


        public SaveableDict<string, List<string>> SucTasks;
        public SaveableDict<string, double> TaskTime;

        public List<List<string>> SucSubassems;
        public List<string> TempSucSubassem;

        [XmlIgnore]
        public SaveableDict<string, SubAssembly> InstTasks;
        [XmlIgnore]
        public SaveableDict<string, SubAssembly> SubAssemAndParts;

        public List<Part> RefWithOneNode;
        [XmlIgnore]
        public List<SubAssembly> RefPrec;
        [XmlIgnore]
        public List<SubAssembly> Movings;

        public SaveableDict<string, double[,]> TranslateToMagicBoxDic;
        public List<double[]> VertsOnCircle;


        public designGraph AssemblyGraph;
        public double StabilityWeightChosenByUser;
        public double UncertaintyWeightChosenByUser;
        public double MeshMagnifier;
        public double[] PointInMagicBox;
        public int BeamWidth;
        public bool DetectFasteners;
        public int AvailableWorkers;
        public int FastenersAreThreaded;
        public double StabilityScore;
        public bool RobustSolution;
        public List<int> globalDirPool;
        public List<double> allmtime;
        public List<double> allitime;
        public List<double> gpmovingtime;
        public List<double> gpinstalltime;
        public List<double> gpsecuretime;
        public List<double> gprotate;

        public void RealToSave(Dictionary<string, List<TessellatedSolid>> Real,
                               List<XMLPair<string, List<SaveableSolid>>> Save)
        {
            List<SaveableSolid> sList;
            Save.Clear();
            foreach (KeyValuePair<string, List<TessellatedSolid>> entry in Real)
            {
                sList = new List<SaveableSolid>();
                foreach(TessellatedSolid s in entry.Value)
                {
                    sList.Add(new SaveableSolid(s));
                }
                Save.Add(new XMLPair<string, List<SaveableSolid>> (entry.Key,sList));
            }
        }

        public void SaveToReal(List<XMLPair<string, List<SaveableSolid>>> Save,
                               Dictionary<string, List<TessellatedSolid>> Real)
        {
            List<TessellatedSolid> sList;
            //List<TessellatedSolid> tList;
            Real.Clear();
            foreach (XMLPair<string, List<SaveableSolid>> entry in Save)
            {
                sList = new List<TessellatedSolid>();
                foreach (SaveableSolid s in entry.Item2)
                {
                    //tList = TVGL.IOFunctions.IO.OpenFromString(s);
                    //foreach(TessellatedSolid t in tList)
                    //{
                    sList.Add(s.generate());
                    //}
                }
                Real[entry.Item1] = sList;
            }
        }



        public void CleanStart()
        {
            DegreeOfFreedoms = new List<double>();
            StablbiblityScores = new List<double>();
            Solids = new Dictionary<string, List<TessellatedSolid>>();
            SolidsNoFastener = new Dictionary<string, List<TessellatedSolid>>();
            SolidsNoFastenerSimplified = new Dictionary<string, List<TessellatedSolid>>();
            SimplifiedSolids = new Dictionary<string, List<TessellatedSolid>>();
            SaveSolids = new List<XMLPair<string, List<SaveableSolid>>>();
            SaveSolidsNoFastener = new List<XMLPair<string, List<SaveableSolid>>>();
            SaveSolidsNoFastenerSimplified = new List<XMLPair<string, List<SaveableSolid>>>();
            SaveSimplifiedSolids = new List<XMLPair<string, List<SaveableSolid>>>();

            SolidsMass = new Dictionary<string, double>();
            SaveSolidsMass = new SaveableDict<string, double>();

            BBoxes = new SolidKeySaveDict<BoundingBox>();
            BCyls = new SolidKeySaveDict<BoundingCylinder>();

            Parts = new SolidKeySaveDict<Partition[]>();
            PartsAB = new SolidKeySaveDict<PartitionAABB[]>();

            SucTasks = new SaveableDict<string, List<string>>();
            TaskTime = new SaveableDict<string, double>();
            SucSubassems = new List<List<string>>();
            TempSucSubassem = new List<string>();
            InstTasks = new SaveableDict<string, SubAssembly>();
            SubAssemAndParts = new SaveableDict<string, SubAssembly>();
            RefWithOneNode = new List<Part>();
            RefPrec =  new List<SubAssembly>();
            Movings = new List<SubAssembly>();
            TranslateToMagicBoxDic = new SaveableDict<string, double[,]>();
            VertsOnCircle = new List<double[]>();

        StabilityWeightChosenByUser = 0;
            UncertaintyWeightChosenByUser = 0;
            MeshMagnifier = 1;
            PointInMagicBox = new double[] {0.0,0.0,0.0};
            DetectFasteners = true;
            AvailableWorkers = 0;
            FastenersAreThreaded = 0; // 0: none, 1: all, 2: subset
            StabilityScore = 0;
            RobustSolution = false;
            globalDirPool = new List<int>();
            allmtime = new List<double>();
            allitime = new List<double>();
            gpmovingtime = new List<double>();
            gpinstalltime = new List<double>();
            gpsecuretime = new List<double>();
            gprotate = new List<double>();
        }

        public static ProgramState Load(string sourceFile)
        {
            XmlSerializer ser = new XmlSerializer(typeof(ProgramState));
            var reader = new StreamReader(sourceFile);
            var data = (ProgramState)ser.Deserialize(reader);

            data.Solids = new Dictionary<string, List<TessellatedSolid>>();
            data.SolidsNoFastener = new Dictionary<string, List<TessellatedSolid>>();
            data.SolidsNoFastenerSimplified = new Dictionary<string, List<TessellatedSolid>>();
            data.SimplifiedSolids = new Dictionary<string, List<TessellatedSolid>>();
            data.SolidsMass = new Dictionary<string, double>();

            data.SaveToReal(data.SaveSolids, data.Solids);
            data.SaveToReal(data.SaveSolidsNoFastener, data.SolidsNoFastener);
            data.SaveToReal(data.SaveSolidsNoFastenerSimplified,data.SolidsNoFastenerSimplified);
            data.SaveToReal(data.SaveSimplifiedSolids,data.SimplifiedSolids);

            data.SolidsMass = data.SaveSolidsMass.generate();


            BoundingGeometry.OrientedBoundingBoxDic = data.BBoxes.generate();
            BoundingGeometry.BoundingCylinderDic = data.BCyls.generate();
            PartitioningSolid.Partitions = data.Parts.generate();
            PartitioningSolid.PartitionsAABB = data.PartsAB.generate();


            OptimalOrientation.SucTasks = data.SucTasks.generate();
            OptimalOrientation.TaskTime = data.TaskTime.generate();
            OptimalOrientation.SucSubassems = data.SucSubassems;
            OptimalOrientation.TempSucSubassem = data.TempSucSubassem;
            OptimalOrientation.InstTasks = data.InstTasks.generate();
            OptimalOrientation.SubAssemAndParts = data.SubAssemAndParts.generate();
            OptimalOrientation.RefWithOneNode = data.RefWithOneNode;
            OptimalOrientation.RefPrec = data.RefPrec;
            OptimalOrientation.Movings = data.Movings;
            OptimalOrientation.TranslateToMagicBoxDic = data.TranslateToMagicBoxDic.generate();
            OptimalOrientation.VertsOnCircle = data.VertsOnCircle;

            return data;
        }

        public void Save(string destFile)
        {
            RealToSave(Solids, SaveSolids);
            RealToSave(SolidsNoFastener, SaveSolidsNoFastener);
            RealToSave(SolidsNoFastenerSimplified, SaveSolidsNoFastenerSimplified);
            RealToSave(SimplifiedSolids, SaveSimplifiedSolids);
            SaveSolidsMass = new SaveableDict<string, double>(SolidsMass);

            BBoxes = new SolidKeySaveDict<BoundingBox>(BoundingGeometry.OrientedBoundingBoxDic);
            BCyls = new SolidKeySaveDict<BoundingCylinder>(BoundingGeometry.BoundingCylinderDic);

            Parts = new SolidKeySaveDict<Partition[]>(PartitioningSolid.Partitions);
            PartsAB = new SolidKeySaveDict<PartitionAABB[]>(PartitioningSolid.PartitionsAABB);


            SucTasks = new SaveableDict<string, List<string>>(OptimalOrientation.SucTasks);
            TaskTime = new SaveableDict<string, double>(OptimalOrientation.TaskTime);
            SucSubassems = OptimalOrientation.SucSubassems;
            TempSucSubassem = OptimalOrientation.TempSucSubassem;
            InstTasks = new SaveableDict<string, SubAssembly>(OptimalOrientation.InstTasks);
            SubAssemAndParts = new SaveableDict<string, SubAssembly>(OptimalOrientation.SubAssemAndParts);
            RefWithOneNode = OptimalOrientation.RefWithOneNode;
            RefPrec = OptimalOrientation.RefPrec;
            Movings = OptimalOrientation.Movings;
            TranslateToMagicBoxDic = new SaveableDict<string, double[,]>(OptimalOrientation.TranslateToMagicBoxDic);
            VertsOnCircle = OptimalOrientation.VertsOnCircle;

            XmlSerializer ser = new XmlSerializer(typeof(ProgramState));
            var writer = new StreamWriter(destFile);
            ser.Serialize(writer, this);
        }

    }
}
