using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using TVGL.IOFunctions;
using System.Xml.Serialization;
using System.IO;
using System.Diagnostics;

namespace Assembly_Planner
{
    [Serializable]
    public class SaveableSolid
    {
        public static Dictionary<string, List<TessellatedSolid>> loadDict;
		public string FileName;
		TextWriterTraceListener writer = null;

        public SaveableSolid(TessellatedSolid theSolid)
        {
            FileName = theSolid.Name;
			if(FileName == "" || FileName == null)
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
			List<TessellatedSolid> result = new List<TessellatedSolid>();
            if (loadDict.ContainsKey(FileName))
            {
                result = loadDict[FileName];
            }
            else
            {
				if (writer == null) {
					Message.Verbosity = TVGL.VerbosityLevels.Everything;
					writer = new TextWriterTraceListener(Console.Error);
					Debug.Listeners.Add(writer);
				}

                var filePath = Program.state.inputDir + "\\intermediate\\" + FileName + ".xml";
                var fileStream = File.OpenRead(filePath);
                result = IO.Open(fileStream, filePath);
				if (result == null) {
					writer.Flush ();
					Console.Out.Flush ();
					throw new SystemException("SHOUT");
				}

				foreach (TessellatedSolid s in result) {
					s.Name = FileName;
				}

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
                var fileStream = File.OpenWrite(Program.state.inputDir+"/intermediate/" + p.Key + ".xml");
                foreach( TessellatedSolid s in p.Value)
                {
                    IO.Save(fileStream, s, FileType.TVGL);
                }
            }
        }

    }
}
