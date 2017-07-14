using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Planner
{
    [Serializable]
    public class SaveableDict<T1, T2>
    {
        public List<XMLPair<T1, T2>> Data;

        public SaveableDict(IDictionary<T1,T2> theDict)
        {
            foreach(KeyValuePair<T1,T2> p in theDict)
            {
                Data.Add(new XMLPair<T1, T2>(p.Key, p.Value));
            }
        }
        public SaveableDict()
        {

        }

        public Dictionary<T1,T2> generate()
        {
            Dictionary<T1, T2> result = new Dictionary<T1, T2>();
            foreach(XMLPair<T1,T2> p in Data)
            {
                result[p.Item1] = p.Item2;
            }
            return result;
        }
    }
}
