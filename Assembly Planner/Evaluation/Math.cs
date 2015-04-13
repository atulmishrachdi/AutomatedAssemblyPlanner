/*************************************************************************
 *     This file & class is part of the MIConvexHull Library Project. 
 *     Copyright 2010 Matthew Ira Campbell, PhD.
 *
 *     MIConvexHull is free software: you can redistribute it and/or modify
 *     it under the terms of the GNU General Public License as published by
 *     the Free Software Foundation, either version 3 of the License, or
 *     (at your option) any later version.
 *  
 *     MIConvexHull is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU General Public License for more details.
 *  
 *     You should have received a copy of the GNU General Public License
 *     along with MIConvexHull.  If not, see <http://www.gnu.org/licenses/>.
 *     
 *     Please find further details and contact information on GraphSynth
 *     at http://miconvexhull.codeplex.com
 *************************************************************************/
using MIConvexHull;
using StarMathLib;

namespace AssemblyEvaluation
{
    public class Vertex : IVertex
    {
        public double[] Position { get; set; }
        public Vertex(double x, double y, double z)
        {
            Position = new double[] { x, y, z };
        }

        public Vertex(double[] position)
        {
            Position = new double[] { position[0], position[1], position[2] };
        }
        internal bool Same(IVertex vertex)
        {
            return (Position[0] == vertex.Position[0]
                && Position[1] == vertex.Position[1]
                && Position[2] == vertex.Position[2]);
        }
        internal Vector MakeVectorTo(IVertex vertex)
        {
            return (new Vector(
                vertex.Position[0] - Position[0],
                vertex.Position[1] - Position[1],
                vertex.Position[2] - Position[2]));
        }
        internal Vector MakeVectorToRayStart(Ray ray)
        {
            return (new Vector(
                ray.Position[0] - Position[0],
                ray.Position[1] - Position[1],
                ray.Position[2] - Position[2]));
        }
    }
    public class Vector : Vertex
    {
        public Vector(double x, double y, double z)
            : base(x, y, z)
        { }
        public Vector(double[] position)
            : base(position)
        { }
        internal void NormalizeInPlace()
        {
            StarMath.normalizeInPlace(Position, 3);
        }

        internal void AddInPlace(Vertex dir)
        {
            Position[0] += dir.Position[0];
            Position[1] += dir.Position[1];
            Position[2] += dir.Position[2];
        }

    }
    public class Ray : Vertex
    {
        public double[] Direction { get; set; }

        public Ray(Vertex start, Vector direction)
            : base(start.Position[0], start.Position[1], start.Position[2])
        {
            Direction = new double[] { direction.Position[0], direction.Position[1], direction.Position[2] };
            StarMath.normalizeInPlace(Direction, 3);
        }
    }
}
