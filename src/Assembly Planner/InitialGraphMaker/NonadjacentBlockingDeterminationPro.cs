using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphSynth.Representation;
using StarMathLib;
using TVGL;
using TVGL;

namespace Assembly_Planner
{
    internal class NonadjacentBlockingDeterminationPro
    {
        internal static void Run(designGraph graph,
            List<TessellatedSolid> solids, List<int> gDir)
        {
            foreach (var dir in gDir)
            {
                var direction = DisassemblyDirections.Directions[dir];
                var blockingsForDirection = new List<NonAdjacentBlockings>();
                foreach (var solid in solids.Where(s => graph.nodes.Any(n => n.name == s.Name)))
                {
                    var movingProj = _3Dto2D.Get2DProjectionPoints(solid.Vertices, direction);
                    /*var edgeList1 = new List<double[]>();
                    foreach (var edge in solid.Edges)
                    {
                        var edge2D = new double[][2];
                        edge2D[0] = edge.From.Position;
                        edge2D[1] = edge.To.Position;
                        edgeList1.Add(edge2D);
                    }*/
                    var moving2D = new _3Dto2D { ThreeD = solid, Points = movingProj, Edges = _3Dto2D.Get2DEdges(solid, movingProj) };
                    foreach (var solidBlocking in 
                        solids.Where(s => graph.nodes.Any(n => n.name == s.Name) // it is not fastener
                                          && s != solid // it is not the same as current solid 
                                          &&
                                          !graph.arcs.Any(a => // there is no arc between the current and the candidate
                                              (a.From.name == solid.Name && a.To.name == s.Name) ||
                                              (a.From.name == s.Name && a.To.name == solid.Name))))
                    {
                        var referenceProj = _3Dto2D.Get2DProjectionPoints(solidBlocking.Vertices, direction);
                        var reference2D = new _3Dto2D { ThreeD = solidBlocking, Points = referenceProj, Edges = _3Dto2D.Get2DEdges(solidBlocking, referenceProj) };
                        var blocked = IsItBlocked(moving2D,reference2D);
                        if (blocked)
                        {
                            blockingsForDirection.Add(new NonAdjacentBlockings
                            {
                                blockingSolids = new[] {solid, solidBlocking}
                            });
                        }
                    }
                }
                NonadjacentBlockingDetermination.NonAdjacentBlocking.Add(dir, blockingsForDirection);
            }
        }

        internal static bool IsItBlocked(_3Dto2D moving2D, _3Dto2D reference2D)
        {
            return moving2D.Edges.Any(movEdge => reference2D.Edges.Any(refEdge => DoIntersect(movEdge, refEdge)));
        }

        public static bool DoIntersect(Point[] movEdge, Point[] refEdge)
        {
            var p1 = movEdge[0];
            var q1 = movEdge[1];
            var p2 = refEdge[0];
            var q2 = refEdge[1];

            // Find the four orientations
            var o1 = Orientation(p1, q1, p2);
            var o2 = Orientation(p1, q1, q2);
            var o3 = Orientation(p2, q2, p1);
            var o4 = Orientation(p2, q2, q1);
            
            if (o1 != o2 && o3 != o4) return true;
            if (o1 == 0 && OnSegment(p1, p2, q1)) return true;
            if (o2 == 0 && OnSegment(p1, q2, q1)) return true;
            if (o3 == 0 && OnSegment(p2, p1, q2)) return true;
            if (o4 == 0 && OnSegment(p2, q1, q2)) return true;
            return false;
        }

        static bool  OnSegment(Point p, Point q, Point r)
        {
            if (q.Position2D[0] <= Math.Max(p.Position2D[0], r.Position2D[0]) && q.Position2D[0] >= Math.Min(p.Position2D[0], r.Position2D[0]) &&
                q.Position2D[1] <= Math.Max(p.Position2D[1], r.Position2D[1]) && q.Position2D[1] >= Math.Min(p.Position2D[1], r.Position2D[1]))
                return true;

            return false;
        }

        static int Orientation(Point p, Point q, Point r)
        {
            var val = (q.Position2D[1] - p.Position2D[1]) * (r.Position2D[0] - q.Position2D[0]) -
                      (q.Position2D[0] - p.Position2D[0]) * (r.Position2D[1] - q.Position2D[1]);

            if (Math.Abs(val) < 1e-5) return 0;  // colinear
            return (val > 0) ? 1 : 2;
        }
    }
}
