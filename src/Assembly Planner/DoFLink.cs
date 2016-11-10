using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assembly_Planner
{

    enum DoFLinkType
    {
        Fused,
        Prismatic,
        Revolute,
        Helical,
        Gear,
        Cylindrical,
        Planar,
        Sphereical
    }
    

    abstract class DoFLink
    {

        private DoFPart PartA;
        private DoFPart PartB;
        private DoFLinkType Type;

        internal uint DoFNumber;


        public DoFLinkType getType()
        {
            return Type;
        }

        public uint getDoFNumber()
        {
            return DoFNumber;
        }


        public DoFPart otherPart(DoFPart callingPart)
        {
            if (callingPart == PartA)
            {
                return PartB;
            }
            else
            {
                return PartA;
            }
        }

        public uint defaultMaxDoF()
        {
            switch (Type)
            {
                case DoFLinkType.Fused:
                    return 0;
                case DoFLinkType.Prismatic:
                    return 1;
                case DoFLinkType.Revolute:
                    return 1;
                case DoFLinkType.Helical:
                    return 1;
                case DoFLinkType.Gear:
                    return 1;
                case DoFLinkType.Cylindrical:
                    return 2;
                case DoFLinkType.Planar:
                    return 3;
                case DoFLinkType.Sphereical:
                    return 3;
                default:
                    return 6;
            }
        }

        abstract public Boolean allowsForTranslation();

        abstract public Boolean allowsForRotation(); 


        public DoFLink(DoFPart partA, DoFPart partB, DoFLinkType linkType)
        {
            Type = linkType;
            PartA = partA;
            PartB = partB;
            DoFNumber = 0;
        }


    }

}


