namespace GraphSynth.Representation
{
    public enum FastenerTypeEnum
    {
        Bolt,
        Screw,
        OtherThreadedPost,
        Pin,
        Rivet,
        Clip,
        Adhesive,
        Weld,
    }
    public class Fastener
    {
        public int RemovalDirection;
        public double OverallLength;
        public double EngagedLength;
        public double Diameter;
        public double Mass;
        public double[] AccessPosition;
        public double[] AccessDirection;
        public double AccessCylinderRadius;
        public FastenerTypeEnum FastenerType;
    }
}