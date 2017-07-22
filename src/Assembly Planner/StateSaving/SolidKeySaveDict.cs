using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace Assembly_Planner
{
    [Serializable]
    public class SolidKeySaveDict<Val>
    {
        public List<XMLPair<SaveableSolid, Val>> Data;

        public SolidKeySaveDict(IDictionary<TessellatedSolid,Val> theDict)
        {
            Data = new List<XMLPair<SaveableSolid, Val>>();
            if(theDict == null)
            {
                return;
            }
            foreach (KeyValuePair<TessellatedSolid, Val> p in theDict)
            {
                Data.Add(new XMLPair<SaveableSolid, Val>(new SaveableSolid(p.Key), p.Value));
            }
        }


        public SolidKeySaveDict()
        {
            Data = new List<XMLPair<SaveableSolid, Val>>();
        }

        public Dictionary<TessellatedSolid, Val> generate()
        {
            Dictionary<TessellatedSolid, Val> result = new Dictionary<TessellatedSolid, Val>();
            List<TessellatedSolid> tList = new List<TessellatedSolid>();
            foreach (XMLPair<SaveableSolid, Val> p in Data)
            {
                tList = p.Item1.generate();
                foreach(TessellatedSolid t in tList)
                {
                    result[t] = p.Item2;
                }
            }
            return result;
        }
    }

}
