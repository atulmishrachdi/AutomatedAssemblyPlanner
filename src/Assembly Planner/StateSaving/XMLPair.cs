using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Planner
{
    [Serializable]
    public class XMLPair<T1, T2>
    {
        public XMLPair()
        { }
        public XMLPair(T1 first,T2 second)
        {
            Item1 = first;
            Item2 = second;
        }
        public T1 Item1;
        public T2 Item2;

    }
}
