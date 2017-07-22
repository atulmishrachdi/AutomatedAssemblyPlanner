using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using TVGL.IOFunctions;
using System.Xml.Serialization;
using System.IO;

namespace Assembly_Planner
{
    [Serializable]
    public class SaveableSolid
    {
        public static Dictionary<string, List<TessellatedSolid>> loadDict;
        public string FileName;

        public SaveableSolid(TessellatedSolid theSolid)
        {
            FileName = theSolid.Name;
            if(FileName == "")
            {
                throw new SystemException("SCREAM");
            }
            if(loadDict == null)
            {
                loadDict = new Dictionary<string, List<TessellatedSolid>>();
            }
            if (loadDict.ContainsKey(theSolid.Name)){
                if (loadDict[FileName].Contains(theSolid))
                {
                    return;
                }
                loadDict[FileName].Add(theSolid);
            }
            FileName = theSolid.Name;
            List<TessellatedSolid> tList = new List<TessellatedSolid>();
            tList.Add(theSolid);
            loadDict[FileName] = tList;
        }

        public SaveableSolid()
        {
            FileName = "";
        }

        public List<TessellatedSolid> generate()
        {
            if (loadDict == null)
            {
                loadDict = new Dictionary<string, List<TessellatedSolid>>();
            }
            List<TessellatedSolid> result;
            if (loadDict.ContainsKey(FileName))
            {
                result = loadDict[FileName];
            }
            else
            {
                var fileStream = File.OpenRead("workspace/" + FileName+".tvgl");
                result = IO.Open(fileStream, FileName + ".tvgl");
                loadDict[FileName] = result;
            }
            return result;
        }

        public static void saveAll()
        {
            if (loadDict == null)
            {
                loadDict = new Dictionary<string, List<TessellatedSolid>>();
            }
            foreach (KeyValuePair<string,List<TessellatedSolid>> p in loadDict)
            {
                var fileStream = File.OpenWrite("workspace/" + p.Key + ".tvgl");
                foreach( TessellatedSolid s in p.Value)
                {
                    IO.Save(fileStream, s, FileType.TVGL);
                }
            }
        }

    }
}
