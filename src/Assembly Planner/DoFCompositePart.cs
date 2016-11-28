using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Planner
{
    class DoFCompositePart : DoFPart
    {
        List<DoFPart> Components;

        internal uint DoFNumber;

        internal bool isLoop;

        virtual public Boolean isComposite()
        {
            return true;
        }



        public DoFCompositePart(List <DoFPart> components)
        {

            Components = components;

            int pos = 0;
            int lim = Components.Count-1;
            uint DoFAcc = 0;

            bool done = false;
            bool open = false;

            while (pos<lim && !done)
            {
                switch (Components[pos].connections.Count)
                {
                    case 0:
                        DoFAcc += 6;
                        done = true;
                        break;
                    case 1:
                        if (Components[pos].connections[0].otherPart(Components[pos]) == Components[pos + 1])
                        {
                            open = true;
                            DoFAcc += Components[pos].connections[0].defaultMaxDoF();
                        }
                        else
                        {
                            open = true;
                            done = true;
                        }
                        break;
                    case 2:
                        if(Components[pos].connections[0].otherPart(Components[pos]) == Components[pos+1])
                        {
                            DoFAcc += Components[pos].connections[0].defaultMaxDoF();
                        }
                        else
                        {
                            DoFAcc += Components[pos].connections[1].defaultMaxDoF();
                        }
                        if (Components[pos].connections[0].otherPart(Components[pos]) ==
                            Components[pos].connections[1].otherPart(Components[pos]))
                        {
                            isLoop = true;
                            done = true;
                        }
                        break;
                }
                
                pos++;

            }

            if (open)
            {
                DoFAcc += 6;
            }

            DoFNumber = DoFAcc;
            
        }





    }
}


