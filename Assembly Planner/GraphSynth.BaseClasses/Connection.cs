/*************************************************************************
 *     This arc file & class is part of the GraphSynth.BaseClasses Project
 *     which is the foundation of the GraphSynth Application.
 *     GraphSynth.BaseClasses is protected and copyright under the MIT
 *     License.
 *     Copyright (c) 2011 Matthew Ira Campbell, PhD.
 *
 *     Permission is hereby granted, free of charge, to any person obtain-
 *     ing a copy of this software and associated documentation files 
 *     (the "Software"), to deal in the Software without restriction, incl-
 *     uding without limitation the rights to use, copy, modify, merge, 
 *     publish, distribute, sublicense, and/or sell copies of the Software, 
 *     and to permit persons to whom the Software is furnished to do so, 
 *     subject to the following conditions:
 *     
 *     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
 *     EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
 *     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGE-
 *     MENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
 *     FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
 *     CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
 *     WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *     
 *     Please find further details and contact information on GraphSynth
 *     at http://www.GraphSynth.com.
 *************************************************************************/
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GraphSynth.Representation
{
    public enum ConnectionTypeEnum
    {
        Loose, 
        Tight
        // just a brainstorm right now...
    }

    public class connection : arc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="edge"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public connection(string name = "e") : base(name) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="edge"/> class.
        /// </summary>
        public connection() { }
        /// <summary>
        ///   Copies this instance of an arc and returns the copy.
        /// </summary>
        /// <returns>the copy of the arc.</returns>
        public override arc copy()
        {
            var copyOfEdge = new connection();
            base.copy(copyOfEdge);

            return copyOfEdge;
        }

        public List<int> InfiniteDirections;

        public List<int> FiniteDirections;

        public List<Fastener> Fasteners;

        public double Certainty;

        public ConnectionTypeEnum ConnectionType;
 
    }
}