/*************************************************************************
 *     This file & class is part of the Object-Oriented Optimization
 *     Toolbox (or OOOT) Project
 *     Copyright 2010 Matthew Ira Campbell, PhD.
 *
 *     OOOT is free software: you can redistribute it and/or modify
 *     it under the terms of the GNU General public License as published by
 *     the Free Software Foundation, either version 3 of the License, or
 *     (at your option) any later version.
 *  
 *     OOOT is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU General public License for more details.
 *  
 *     You should have received a copy of the GNU General public License
 *     along with OOOT.  If not, see <http://www.gnu.org/licenses/>.
 *     
 *     Please find further details and contact information on OOOT
 *     at http://ooot.codeplex.com/.
 *************************************************************************/
using System;
using System.Collections.Generic;

namespace GraphSynth
{
    public class MO_optimizeSort : IComparer<List<double>>
    {
        public Boolean BetterThan(List<double> x, List<double> y)
        {
            return (-1 == Compare(x, y));
        }

        public int Compare(List<double> x, List<double> y)
        {
            if (x[0] > y[0])
            {
                return 1;
            }
            else return -1;
            if (x[0] < y[0])
            {
                return -1;
            }
            if (x[4] > y[4])
            {
                return 1;
            }
            return -1;
        }
    }
}