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
            if (loadDict.ContainsKey(theSolid.FileName)){
                if (loadDict[FileName].Contains(theSolid))
                {
                    return;
                }
                loadDict[FileName].Add(theSolid);
            }
            FileName = theSolid.FileName;
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
            List<TessellatedSolid> result;
            if (loadDict.ContainsKey(FileName))
            {
                result = loadDict[FileName];
            }
            else
            {
                var fileStream = File.OpenRead(FileName);
                result = IO.Open(fileStream, FileName);
                loadDict[FileName] = result;
            }
            return result;
        }

    }
}
