using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Assembly_Planner
{

    class DoFGraph
    {

        List<DoFPart> Parts;


        public void add(DoFPart partToAdd)
        {
            Parts.Add(partToAdd);
        }



        public void determineDoF()
        {
            combinePartChains();
            combineLockedParts();
        }



        private void combineLockedParts()
        {



        }

        private void revoluteViability(DoFLink theJoint)
        {



        }




        private void combinePartChains()
        {

            List<DoFPart> partPile = new List<DoFPart>();
            List<DoFPart> partChain;

            foreach (DoFPart thePart in Parts)
            {
                if (thePart.connections.Count<=2)
                {
                    partPile.Add(thePart);
                }
            }

            DoFPart chain;

            while (Parts.Count > 0)
            {
                partChain = grabChain(partPile[0],partPile);
                cullParts(partChain, Parts);
                chain = new DoFCompositePart(partChain);
                Parts.Add(chain);
            }

            
        }



        private List<DoFPart> grabChain(DoFPart chainStart, List<DoFPart> partPile)
        {

            List<DoFPart> preChain = new List<DoFPart>();
            List<DoFPart> postChain = new List<DoFPart>();

            bool loop = false;

            if (chainStart.connections.Count == 0)
            {
                preChain.Add(chainStart);
            }
            else
            {
                DoFPart partIter;
                DoFPart lastPart;

                if (chainStart.connections.Count == 2)
                {
                    partIter = chainStart.connections[0].otherPart(chainStart);
                    lastPart = chainStart;
                    while (partPile.Contains(partIter) && partIter!=chainStart)
                    {
                        preChain.Add(partIter);
                        if (partIter.connections.Count == 1)
                        {
                            break;
                        }
                        else
                        {
                            if (partIter.connections[0].otherPart(partIter) != lastPart)
                            {
                                lastPart = partIter;
                                partIter = partIter.connections[0].otherPart(partIter);
                            }
                            else
                            {
                                lastPart = partIter;
                                partIter = partIter.connections[1].otherPart(partIter);
                            }
                        }
                    }
                    if (partIter == chainStart)
                    {
                        loop = true;
                    }
                }

                if (loop == false)
                {
                    partIter = chainStart.connections[1].otherPart(chainStart);
                    lastPart = chainStart;
                    while (partPile.Contains(partIter))
                    {
                        preChain.Add(partIter);
                        if (partIter.connections.Count == 1)
                        {
                            break;
                        }
                        else
                        {
                            if (partIter.connections[0].otherPart(partIter) != lastPart)
                            {
                                lastPart = partIter;
                                partIter = partIter.connections[0].otherPart(partIter);
                            }
                            else
                            {
                                lastPart = partIter;
                                partIter = partIter.connections[1].otherPart(partIter);
                            }
                        }
                    }
                }
                
            }

            preChain.Reverse();
            preChain.Add(chainStart);
            List<DoFPart> result = preChain;
            result = (result.Concat(postChain)).ToList();

            return result;

        }





        private void cullParts(List<DoFPart> cullList, List<DoFPart> partList)
        {
            foreach(DoFPart cullPart in cullList)
            {
                partList.Remove(cullPart);
            }
        }



    }

}



