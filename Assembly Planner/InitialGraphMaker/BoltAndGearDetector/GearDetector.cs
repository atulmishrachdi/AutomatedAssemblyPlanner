using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;
using TVGL;

namespace Assembly_Planner
{
    class GearDetector
    {
        public static List<Gear> Run(List<TessellatedSolid> solids, bool estimate = false)
        {
            // Fast and inaccurate results vs Slow but accurate
            // estimate: inaccurate but fast
            if (!estimate)
            {
                var gears = solids.Select(GearPolynomialTrend.PolynomialTrendDetector).Where(gear => gear != null).ToList();
                return gears;
            }
            // This can be my old gear detector approach
            return null;
        }


        internal static List<int> UpdateRemovalDirectionsIfGearMate(TessellatedSolid solid1, TessellatedSolid solid2,
            List<Gear> gears, List<int> localDirInd)
        {
            // "gear1" is always "Reference" and "gear2" is always "Moving"
            var gear1 = gears.First(g => g.Solid == solid1);
            var gear2 = gears.First(g => g.Solid == solid2);
            if (gear1 == null || gear2 == null) return localDirInd;
            // now it is a gear mate
            var rd = DisassemblyDirections.Directions.First(d => Math.Abs(d.dotProduct(gear1.Axis) - 1) < 0.01);
            var ind1 = DisassemblyDirections.Directions.IndexOf(rd);
            var ind2 = DisassemblyDirections.Directions.IndexOf(rd.multiply(-1.0));

            if (Math.Abs(gear1.Axis.dotProduct(gear2.Axis)) < 0.001)
            {
                gear1.Type = GearType.Bevel;
                gear2.Type = GearType.Bevel;
                if (!localDirInd.Contains(ind1)) localDirInd.Add(ind1);
                if (!localDirInd.Contains(ind2)) localDirInd.Add(ind2);
                var rd2 = DisassemblyDirections.Directions.First(d => Math.Abs(d.dotProduct(gear2.Axis) - 1) < 0.01);
                var ind21 = DisassemblyDirections.Directions.IndexOf(rd2);
                var ind22 = DisassemblyDirections.Directions.IndexOf(rd2.multiply(-1.0));
                if (!localDirInd.Contains(ind21)) localDirInd.Add(ind1);
                if (!localDirInd.Contains(ind22)) localDirInd.Add(ind2);
            }
            // add two simple removal direction if they are not already in the list:
            if (gear1.Type == GearType.Internal || gear2.Type == GearType.Internal)
            {
                if (!localDirInd.Contains(ind1)) localDirInd.Add(ind1);
                if (!localDirInd.Contains(ind2)) localDirInd.Add(ind2);
                return localDirInd;
            }
            // if they are simple gears: spur:
            if (!localDirInd.Contains(ind1)) localDirInd.Add(ind1);
            if (!localDirInd.Contains(ind2)) localDirInd.Add(ind2);
            var oneMore =
                DisassemblyDirections.Directions.First(
                    d => Math.Abs(d.dotProduct(gear2.PointOnAxis.subtract(gear1.PointOnAxis)) - 1) < 0.01);
            var indOneMore = DisassemblyDirections.Directions.IndexOf(oneMore);
            if (!localDirInd.Contains(indOneMore)) localDirInd.Add(indOneMore);
            return localDirInd;
        }
    }
}
