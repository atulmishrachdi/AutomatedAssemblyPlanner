using System;
using System.Collections;
using System.IO;
using System.Text;
using GeometryReasoning;
using GraphSynth;

namespace AssemblyEvaluation
{

    /// <summary>
    /// This static class is used to interface with PARC code to find the path that the
    /// moving subassembly must follow to meet a given Install Action
    /// </summary>
    public static class PathDeterminationEvaluator
    {
        //[DllImport ("snpath.dll", EntryPoint = "_evalPath")]
        //public static extern double evalPath (string[] ref_parts, int nref, string[] mov_parts,
        //                                    int nmov, double[] installPt, double[] installVec);

        /// <summary>
        /// This functions simply finds the path length, which is to be used in the evaluation. 
        /// </summary>
        /// <param name="movingParts"></param>
        /// <param name="refParts"></param>
        /// <param name="insertionDirection"></param>
        /// <param name="insertionPoint"></param>
        /// <returns></returns>
        internal static double FindPathLength(string[] movingParts, string[] refParts, double[] insertionDirection,
            double[] insertionPoint)
        {
            /******* Sai's code to be called from here. **************/
            // NOTE : it is expected that the wrap_path script has been executed and a shared library
            // called libsnpath.so exists in /usr/local/lib. Otherwise DllImport will fail. - SN

            string[] movingPartsFullPath = new string[movingParts.Length];
            string[] refPartsFullPath = new string[refParts.Length];

            for (int i = 0; i < movingParts.Length; i++) {
                movingPartsFullPath[i] = "../input_Mechanic/" + movingParts[i] + ".STL";
            }

            for (int i = 0; i < refParts.Length; i++) {
                refPartsFullPath[i] = "../input_Mechanic/" + refParts[i] + ".STL";
            }

            if (Double.IsNaN(insertionDirection[0]) || Double.IsNaN(insertionDirection[1]) || Double.IsNaN(insertionDirection[2]))
            {
                Console.WriteLine("INSERTION DIRECTION IS NAN!!!!!");
                Console.WriteLine(insertionDirection[0] + " ... " + insertionDirection[1] + " ... " + insertionDirection[2]);
                insertionDirection[0] = 1.0;
                insertionDirection[1] = 0.0;
                insertionDirection[2] = 0.0;
            }
            
            return EvaluatePath.val(refPartsFullPath, movingPartsFullPath, insertionPoint, insertionDirection);

        }


        /// <summary>
        /// Finds the length of the path out to a sphere (starting from the insertion point) of a specified radius.
        /// </summary>
        /// <param name="movingParts">The moving parts.</param>
        /// <param name="refParts">The reference parts.</param>
        /// <param name="insertionDirection">The insertion direction.</param>
        /// <param name="insertionPoint">The insertion point, also the center of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <returns></returns>
        public static double FindPathLength(string[] movingParts, string[] refParts, double[] insertionDirection, double[] insertionPoint, double radius)
        {
            /******* Sai's code to be called from here. **************/
            // NOTE : it is expected that the wrap_path script has been executed and a shared library
            // called libsnpath.so exists in /usr/local/lib. Otherwise DllImport will fail. - SN

            return 0.0;// EvaluatePath.val(refParts, refParts.GetLength(0), movingParts, movingParts.GetLength(0), insertionPoint, insertionDirection);
        }


        /// <summary>
        /// Finds the length of the path out to a circle (starting from the insertion point) of a specified radius.
        /// </summary>
        /// <param name="movingParts">The moving parts.</param>
        /// <param name="refParts">The reference parts.</param>
        /// <param name="insertionDirection">The insertion direction.</param>
        /// <param name="insertionPoint">The insertion point, also the center of the circle - however this should be projected down to the ground lpan.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="groundPlaneNormal">The ground plane normal vector.</param>
        /// <param name="groundPlanePoint">The coordinates of some point on the ground plane.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static double FindPathLength(string[] movingParts, string[] refParts, double[] insertionDirection, double[] insertionPoint, double radius,
            double[] groundPlaneNormal, double[] groundPlanePoint)
        {
            /******* Sai's code to be called from here. **************/
            // NOTE : it is expected that the wrap_path script has been executed and a shared library
            // called libsnpath.so exists in /usr/local/lib. Otherwise DllImport will fail. - SN
            return 0.0;// EvaluatePath.val(refParts, refParts.GetLength(0), movingParts, movingParts.GetLength(0), insertionPoint, insertionDirection);
        }

        public static double FindTravelDistance(SubAssembly newSubAsm, Vector insertionDirection, Vertex insertionPoint)
        {
            double RiemannianDx, travelDistance;
            var sepHullDx = determineDistanceToSeparateHull(newSubAsm, insertionDirection);
            if (newSubAsm.InstallCharacter == InstallCharacterType.MovingIsInsideReference)
            {
#if SKIP_OMPL
                RiemannianDx = sepHullDx;
#else
                RiemannianDx = PathDeterminationEvaluator.FindPathLength(newSubAsm.Install.Moving.PartNodes.ToArray(),
                    newSubAsm.Install.Reference.PartNodes.ToArray(), newSubAsm.Install.InstallDirection, insertionPoint.Position); 
                Console.WriteLine("OMPL = "+RiemannianDx+", Sep. Hull = "+sepHullDx);
#endif
            }
            else RiemannianDx = sepHullDx;
            if (RiemannianDx < 0)
            {
                Console.WriteLine("Infeasible path found");
                return Constants.MaxPathForInfeasibleInstall;
            }
            else return sepHullDx;
            //else return RiemannianDx;

        }

        private static double determineDistanceToSeparateHull(SubAssembly newSubAsm, Vector insertionDirection)
        {
            var refMaxValue = STLGeometryFunctions.findMaxPlaneHeightInDirection(newSubAsm.Install.Reference.CVXHull.Points, insertionDirection);
            var refMinValue = STLGeometryFunctions.findMinPlaneHeightInDirection(newSubAsm.Install.Reference.CVXHull.Points, insertionDirection);

            var movingMaxValue = STLGeometryFunctions.findMaxPlaneHeightInDirection(newSubAsm.Install.Moving.CVXHull.Points, insertionDirection);
            var movingMinValue = STLGeometryFunctions.findMinPlaneHeightInDirection(newSubAsm.Install.Moving.CVXHull.Points, insertionDirection);

            var distanceToFree = Math.Abs(refMaxValue - movingMinValue);
            if (distanceToFree < 0) { distanceToFree = 0; throw new Exception("How is distance to free less than zero?"); }
            return distanceToFree + (movingMaxValue - movingMinValue) + (refMaxValue - refMinValue);
        }


        #region Functions to write plans to disk in a simplified format
        public static void SaveToDisk(string filename, AssemblyCandidate goal)
        {
            var fileStream = new FileStream(filename, FileMode.Create);
            var streamWriter = new StreamWriter(fileStream, Encoding.UTF8);

            WriteInstallActions(streamWriter, goal.Sequence.Subassemblies[0].Install);

            streamWriter.Flush(); streamWriter.Close();
            fileStream.Close();
        }

        private static void WriteInstallActions(StreamWriter streamWriter, InstallAction installAction)
        {
            if (installAction.Moving is SubAssembly) WriteInstallActions(streamWriter, ((SubAssembly)installAction.Moving).Install);
            if (installAction.Reference is SubAssembly) WriteInstallActions(streamWriter, ((SubAssembly)installAction.Reference).Install);
            streamWriter.WriteLine();
            streamWriter.WriteLine("mov=" + ConvertListToString(installAction.Moving.PartNodes));
            streamWriter.WriteLine("ref=" + ConvertListToString(installAction.Reference.PartNodes));
            streamWriter.WriteLine("dir=[" + ConvertListToString(installAction.InstallDirection) + "]");
            streamWriter.WriteLine("pt=[" + ConvertListToString(installAction.InstallPoint) + "]");
        }
        private static string ConvertListToString(IList list)
        {
            var result = "";
            foreach (var item in list)
            {
                result += item;
                result += ",";
            }
            result = result.TrimEnd(',');
            return result;
        }
        #endregion


    }
}
