using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geometric_Reasoning;
using TVGL;

namespace Fastener_Detection
{
    class StartProcess
    {
        internal static int Threaded = new int();
        static void Main(string[] args)
        {
            var partsWithOneGeom =  Geometric_Reasoning.StartProcess.PartsWithOneGeom;
            partsWithOneGeom = new List<TessellatedSolid>();
            foreach (var subAssem in Geometric_Reasoning.StartProcess.Solids.Values)
                if (subAssem.Count == 1)
                    partsWithOneGeom.Add(subAssem[0]);
            // From repeated parts take only one of them:
            //------------------------------------------------------------------------------------------
            var multipleRefs = DuplicatePartsDetector(partsWithOneGeom);

            // Detect fasteners
            //------------------------------------------------------------------------------------------
            FastenerDetector.Run(Geometric_Reasoning.StartProcess.SolidPrimitive, multipleRefs, Threaded, false);
        }

        private static Dictionary<TessellatedSolid, List<TessellatedSolid>> DuplicatePartsDetector(List<TessellatedSolid> solids)
        {
            // If the number of vertcies and number of faces are exactly the same and also the volumes are equal.
            // Not only we need to detect the repeated parts, but also we need to store their transformation matrix
            // We need the transformatiuon matrix to transform information we get from primitive classification.
            // Is it really worth it? yes. Because we will most likely detect fasteners after this step, so we will
            // have a lot of similar parts.

            // When we are detecting duplicate parts, we will only do it for the parts with one geomtery. Why?
            //  because these duplicates are only used in fastener detection. And fasteners cannot be seen in
            //  parts with more than one geometry
            //Bridge.StatusReporter.ReportStatusMessage("Detecting Duplicated Solids ...", 1);
            //Bridge.StatusReporter.ReportProgress(0);
            var multipleRefs = new Dictionary<TessellatedSolid, List<TessellatedSolid>>();
            for (var i = 0; i < solids.Count; i++)
            {
                var solid = solids[i];
                var exist = multipleRefs.Keys.Where(
                    k =>
                        (Math.Abs(k.Faces.Count() - solid.Faces.Count()) / Math.Max(k.Faces.Count(), solid.Faces.Count()) < 0.01) &&
                        Math.Abs(k.Vertices.Count() - solid.Vertices.Count()) / Math.Max(k.Vertices.Count(), solid.Vertices.Count()) < 0.01 &&
                        (Math.Max(k.SurfaceArea, solid.SurfaceArea) - Math.Min(k.SurfaceArea, solid.SurfaceArea)) /
                        Math.Max(k.SurfaceArea, solid.SurfaceArea) < 0.001).ToList();
                if (exist.Count == 0)
                {
                    var d = new List<TessellatedSolid>();
                    multipleRefs.Add(solid, d);
                }
                else
                {
                    multipleRefs[exist[0]].Add(solid);
                }
            }
            return multipleRefs;
        }
    }
}
