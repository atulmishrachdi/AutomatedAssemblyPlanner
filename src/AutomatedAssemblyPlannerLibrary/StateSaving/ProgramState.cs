using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using System.Xml.Serialization;
using GraphSynth.Representation;
using Assembly_Planner;

namespace Assembly_Planner
{
    [Serializable]
    public class ProgramState
    {

        public string inputDir;

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

        public List<double[]> disDirs;
        public List<double[]> disDirsWithFast;

        public SaveableDict<int, int> dirOps;
        public SaveableDict<int, int> dirOpsForPool;

        [XmlIgnore]
        public Dictionary<string, double> SolidsMass;

        public SaveableDict<string, double> SaveSolidsMass;

        [XmlIgnore]
        public SolidKeySaveDict<BoundingBox> BBoxes;
        [XmlIgnore]
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

		[XmlIgnore]
		public List<Part> RefWithOneNode;
        [XmlIgnore]
        public List<SubAssembly> RefPrec;
        [XmlIgnore]
        public List<SubAssembly> Movings;

        [XmlIgnore]
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
                    sList.AddRange(s.generate());
                }
                Real[entry.Item1] = sList;
            }
        }



        public ProgramState()
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

            disDirs = new List<double[]>();
            disDirsWithFast = new List<double[]>();

            dirOps = new SaveableDict<int, int>();
            dirOpsForPool = new SaveableDict<int, int>();

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

        public static void Load(string sourceFile, ref ProgramState state)
        {
			
            XmlSerializer ser = new XmlSerializer(typeof(ProgramState));
            var reader = new StreamReader(sourceFile);
            state = (ProgramState)ser.Deserialize(reader);

            state.Solids = new Dictionary<string, List<TessellatedSolid>>();
            state.SolidsNoFastener = new Dictionary<string, List<TessellatedSolid>>();
            state.SolidsNoFastenerSimplified = new Dictionary<string, List<TessellatedSolid>>();
            state.SimplifiedSolids = new Dictionary<string, List<TessellatedSolid>>();
            state.SolidsMass = new Dictionary<string, double>();

            state.SaveToReal(state.SaveSolids, state.Solids);
            state.SaveToReal(state.SaveSolidsNoFastener, state.SolidsNoFastener);
            state.SaveToReal(state.SaveSolidsNoFastenerSimplified, state.SolidsNoFastenerSimplified);
            state.SaveToReal(state.SaveSimplifiedSolids, state.SimplifiedSolids);

            state.SolidsMass = state.SaveSolidsMass.generate();

            BoundingGeometry.OrientedBoundingBoxDic = state.BBoxes.generate();
            BoundingGeometry.BoundingCylinderDic = state.BCyls.generate();
            PartitioningSolid.Partitions = state.Parts.generate();
            PartitioningSolid.PartitionsAABB = state.PartsAB.generate();

            OptimalOrientation.SucTasks = state.SucTasks.generate();
            OptimalOrientation.TaskTime = state.TaskTime.generate();
            OptimalOrientation.SucSubassems = state.SucSubassems;
            OptimalOrientation.TempSucSubassem = state.TempSucSubassem;
            OptimalOrientation.InstTasks = state.InstTasks.generate();
            OptimalOrientation.SubAssemAndParts = state.SubAssemAndParts.generate();
            OptimalOrientation.RefWithOneNode = state.RefWithOneNode;
            OptimalOrientation.RefPrec = state.RefPrec;
            OptimalOrientation.Movings = state.Movings;
            OptimalOrientation.TranslateToMagicBoxDic = state.TranslateToMagicBoxDic.generate();
            OptimalOrientation.VertsOnCircle = state.VertsOnCircle;

            DisassemblyDirections.Directions = state.disDirs;
            DisassemblyDirectionsWithFastener.Directions = state.disDirsWithFast;

            DisassemblyDirections.DirectionsAndOpposits = state.dirOps.generate();
            DisassemblyDirections.DirectionsAndOppositsForGlobalpool = state.dirOpsForPool.generate();

            reader.Close();
            
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

            disDirs = DisassemblyDirections.Directions;
            disDirsWithFast = DisassemblyDirectionsWithFastener.Directions;

            dirOps = new SaveableDict<int, int>(DisassemblyDirections.DirectionsAndOpposits);
            dirOpsForPool = new SaveableDict<int, int>(DisassemblyDirections.DirectionsAndOppositsForGlobalpool);

            XmlSerializer ser = new XmlSerializer(typeof(ProgramState));
            var writer = new StreamWriter(destFile);
            ser.Serialize(writer, this);

            SaveableSolid.saveAll();
            writer.Close();

        }

    }
}
