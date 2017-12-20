using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Planner
{
    class DoFPart
    {

        internal List<DoFLink> connections;
        
        
        virtual public Boolean isComposite()
        {
            return false;
        }



    }

}
