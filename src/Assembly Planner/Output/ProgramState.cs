using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using BaseClasses.Representation;
using System.Xml.Serialization;

namespace GraphSynth.OutputXML
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

        public Dictionary<string, List<SaveableSolid>> SaveSolids;
        public Dictionary<string, List<SaveableSolid>> SaveSolidsNoFastener;
        public Dictionary<string, List<SaveableSolid>> SaveSolidsNoFastenerSimplified;
        public Dictionary<string, List<SaveableSolid>> SaveSimplifiedSolids;

        public Dictionary<string, double> SolidsMass;
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
                               Dictionary<string, List<SaveableSolid>> Save)
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
                Save[entry.Key] = sList;
            }
        }

        public void SaveToReal(Dictionary<string, List<SaveableSolid>> Save,
                               Dictionary<string, List<TessellatedSolid>> Real)
        {
            List<TessellatedSolid> sList;
            Real.Clear();
            foreach (KeyValuePair<string, List<SaveableSolid>> entry in Save)
            {
                sList = new List<TessellatedSolid>();
                foreach (SaveableSolid s in entry.Value)
                {
                    sList.Add(s.generate());
                }
                Real[entry.Key] = sList;
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
            SaveSolids = new Dictionary<string, List<SaveableSolid>>();
            SaveSolidsNoFastener = new Dictionary<string, List<SaveableSolid>>();
            SaveSolidsNoFastenerSimplified = new Dictionary<string, List<SaveableSolid>>();
            SaveSimplifiedSolids = new Dictionary<string, List<SaveableSolid>>();
            SolidsMass = new Dictionary<string, double>();
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

        public void Load(string sourceFile)
        {
            XmlSerializer ser = new XmlSerializer(typeof(ProgramState));
            var reader = new StreamReader(sourceFile);
            var data = (ProgramState)ser.Deserialize(reader);

            DegreeOfFreedoms = data.DegreeOfFreedoms;
            StablbiblityScores = data.StablbiblityScores;
            Solids = new Dictionary<string, List<TessellatedSolid>>();
            SolidsNoFastener = new Dictionary<string, List<TessellatedSolid>>();
            SolidsNoFastenerSimplified = new Dictionary<string, List<TessellatedSolid>>();
            SimplifiedSolids = new Dictionary<string, List<TessellatedSolid>>();
            SaveSolids = data.SaveSolids;
            SaveSolidsNoFastener = data.SaveSolidsNoFastener;
            SaveSolidsNoFastenerSimplified = data.SaveSolidsNoFastenerSimplified;
            SaveSimplifiedSolids = data.SaveSimplifiedSolids;
            SolidsMass = data.SolidsMass;
            StabilityWeightChosenByUser = data.StabilityWeightChosenByUser;
            UncertaintyWeightChosenByUser = data.UncertaintyWeightChosenByUser;
            MeshMagnifier = data.MeshMagnifier;
            PointInMagicBox = data.PointInMagicBox;
            DetectFasteners = data.DetectFasteners;
            AvailableWorkers = data.AvailableWorkers;
            FastenersAreThreaded = data.FastenersAreThreaded;
            StabilityScore = data.StabilityScore;
            RobustSolution = data.RobustSolution;
            globalDirPool = data.globalDirPool;
            allmtime = data.allmtime;
            allitime = data.allitime;
            gpmovingtime = data.gpmovingtime;
            gpinstalltime = data.gpinstalltime;
            gpsecuretime = data.gpsecuretime;
            gprotate = data.gprotate;

            SaveToReal(SaveSolids, Solids);
            SaveToReal(SaveSolidsNoFastener, SolidsNoFastener);
            SaveToReal(SaveSolidsNoFastenerSimplified,SolidsNoFastenerSimplified);
            SaveToReal(SaveSimplifiedSolids,SimplifiedSolids);
        }

        public void Save(string destFile)
        {
            RealToSave(Solids, SaveSolids);
            RealToSave(SolidsNoFastener, SaveSolidsNoFastener);
            RealToSave(SolidsNoFastenerSimplified, SaveSolidsNoFastenerSimplified);
            RealToSave(SimplifiedSolids, SaveSimplifiedSolids);
            XmlSerializer ser = new XmlSerializer(typeof(ProgramState));
            var writer = new StreamWriter(destFile);
            ser.Serialize(writer, this);
        }

    }
}
