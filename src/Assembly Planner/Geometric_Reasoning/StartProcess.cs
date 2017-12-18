using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using BaseClasses;


namespace Geometric_Reasoning
{
    public class StartProcess
    {
        public static List<double[]> Directions = new List<double[]>();
        public static Dictionary<int, int> DirectionsAndOpposits = new Dictionary<int, int>();
        public static Dictionary<int, int> DirectionsAndOppositsForGlobalpool = new Dictionary<int, int>();
        public static double[] PointInMagicBox = { 0, 0, 0.0 };
        public static Dictionary<int, HashSet<int>> DirectionsAndSame;
        internal static Dictionary<int, List<Component[]>> NonAdjacentBlocking = new Dictionary<int, List<Component[]>>(); //Component[0] is blocked by Component[1]
        public static Dictionary<TessellatedSolid, List<PrimitiveSurface>> SolidPrimitive =
            new Dictionary<TessellatedSolid, List<PrimitiveSurface>>();
        public static Dictionary<string, List<TessellatedSolid>> Solids;
        public static Dictionary<string, List<TessellatedSolid>> SolidsNoFastener;
        public static double MeshMagnifier;
        public static List<TessellatedSolid> PartsWithOneGeom;
        static void Main(string[] args)
        {
            // Generate a good number of directions on the surface of a sphere
            //------------------------------------------------------------------------------------------
            //SimplifySolids(solids);
            Directions = IcosahedronPro.DirectionGeneration(1);
            Directions = new List<double[]>(Directions);
            FindingOppositeDirections();
            // Creating Bounding Geometries for every solid
            //------------------------------------------------------------------------------------------
            //Bridge.StatusReporter.ReportStatusMessage("Creating Bounding Geometries ... ", 1);
            BoundingGeometry.OrientedBoundingBoxDic = new Dictionary<TessellatedSolid, BoundingBox>();
            BoundingGeometry.BoundingCylinderDic = new Dictionary<TessellatedSolid, BoundingCylinder>();
            BoundingGeometry.CreateOBB2(Solids);
            BoundingGeometry.CreateBoundingCylinder(Solids);
            //Bridge.StatusReporter.PrintMessage("BOUNDING GEOMETRIES ARE SUCCESSFULLY CREATED.", 0.5f);

            // Detecting Springs
            //SpringDetector.DetectSprings(solids);

            // Primitive Classification
            //------------------------------------------------------------------------------------------
            // what parts to be classified?
            var partsForPC = BlockingDetermination.PartsTobeClassifiedIntoPrimitives(Solids);
            SolidPrimitive = BlockingDetermination.PrimitiveMaker(partsForPC);
        }
        private static void FindingOppositeDirections()
        {
            DirectionsAndOpposits = new Dictionary<int, int>();
            for (int i = 0; i < Directions.Count; i++)
            {
                var dir = Directions[i];
                var oppos = Directions.First(d => d[0] == -dir[0] && d[1] == -dir[1] && d[2] == -dir[2]);
                DirectionsAndOpposits.Add(i, Directions.IndexOf(oppos));
            }
        }
        //public string ReturnPath()
        //{
        //    string folder = Environment.CurrentDirectory;
        //    return folder;
        //}
    }
}
