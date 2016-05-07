using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;
using TVGL;

namespace Assembly_Planner
{
    class SpringDetector
    {

        internal static void DetectSprings(Dictionary<string, List<TessellatedSolid>> solids)
        {
            for (var i = 0; i < solids.Count; i++)
            {
                if (solids.Values.ToList()[i].Count > 1) continue;
                if (!Run(solids.Values.ToList()[i][0])) continue;
                solids.Remove(solids.Keys.ToList()[i]);
                i--;
            }
        }
        internal static bool Run(TessellatedSolid solid)
        {
            // take 100 random faces 
            var rnd = new Random();
            var rndVertices = new List<Vertex>();
            for (var i = 0; i < Math.Floor(solid.Vertices.Count() / 50.0); i++)
            {
                var rInd = rnd.Next(0, solid.Vertices.Count());
                rndVertices.Add(solid.Vertices[rInd]);
            }
            var a = new List<int>();
            var possibleCylinders = new Dictionary<double[], double>();
            var radii = new List<double>();
            foreach (var ver in rndVertices)
            {
                var startingVer = ver;
                var shortestEdge = ver.Edges.First(e => e.Length == ver.Edges.Min(e1 => e1.Length));
                Vertex to;
                if (shortestEdge.From == ver) to = shortestEdge.To;
                else to = shortestEdge.From;
                var counter = 0;
                var edge = shortestEdge;
                var circleVertcs = new List<Vertex> { startingVer };
                while ((startingVer != to) && counter < 200)
                {
                    counter++;
                    var from = to;
                    var otherEdges = from.Edges.Where(e => e != edge).ToList();
                    var otherEdge = otherEdges.First(e => e.Length == otherEdges.Min(e1 => e1.Length));
                    circleVertcs.Add(from);
                    if (otherEdge.From == from) to = otherEdge.To;
                    else to = otherEdge.From;
                    edge = otherEdge;
                }
                a.Add(circleVertcs.Count());
                if (circleVertcs.Count() < 14) continue;
                double radius;
                double[] center;
                if (!CircleDetector(circleVertcs, out radius, out center)) continue;
                radii.Add(radius);
            }
            radii.Sort();
            if (radii.Count < 40) return false;
            // get rid of 10 percent on the top and bottom. 
            var fifteenPer = (int)Math.Floor(radii.Count() * 0.15);
            if (((radii[radii.Count - fifteenPer - 1] - radii[fifteenPer]) / radii[fifteenPer]) > 0.15)
                return false;
            return true;
        }

        private static bool CircleDetector(List<Vertex> circleVertcs, out double radius, out double[] center)
        {
            var rnd = new Random();
            radius = 0.0;
            center = null;
            var rndVertices = new List<Vertex>();
            for (int i = 0; i < 20; i++)
            {
                var rInd = rnd.Next(0, circleVertcs.Count());
                rndVertices.Add(circleVertcs[rInd]);
            }
            var lines = new Dictionary<Vertex[], double>();
            var circleVertcsHash = new HashSet<Vertex>(circleVertcs);
            foreach (var ver in rndVertices)
            {
                var maxDis = 0.0;
                Vertex ver2 = null;
                foreach (var refVer in circleVertcsHash)
                {
                    var dis = DistanceBetweenTwoVertices(ver.Position, refVer.Position);
                    if (dis <= maxDis) continue;
                    ver2 = refVer;
                    maxDis = dis;
                }
                lines.Add(new[] { ver, ver2 }, maxDis);
            }
            // all the lines have the same length, it is a cylinder
            var sortedLength = lines.Values.ToList();
            sortedLength.Sort();
            if (((sortedLength[sortedLength.Count - 3] - sortedLength[2]) / sortedLength[0]) > 0.005) return false;
            radius = sortedLength.Average() / 2.0;
            center = centerCalculater(lines.Keys.ToList());
            return true;
        }

        private static double[] centerCalculater(List<Vertex[]> lines)
        {
            var centers = new[] { 0.0, 0.0, 0.0 };
            foreach (var line in lines)
            {
                centers = centers.add((line[0].Position.add(line[1].Position)).divide(2.0));
            }
            return centers.divide(lines.Count());
        }

        public static double DistanceBetweenTwoVertices(double[] vertex1, double[] vertex2)
        {
            return
                Math.Sqrt((Math.Pow(vertex1[0] - vertex2[0], 2)) +
                          (Math.Pow(vertex1[1] - vertex2[1], 2)) +
                          (Math.Pow(vertex1[2] - vertex2[2], 2)));
        }

    }
}
