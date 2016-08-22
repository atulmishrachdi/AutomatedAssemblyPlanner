using GraphSynth;
using GraphSynth.Representation;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace AssemblyEvaluation
{
    public class AssemblyCandidate : candidate
    {
        public AssemblySequence Sequence;

        public AssemblyCandidate()
        {
        }

        public AssemblyCandidate(candidate c)
        {
            activeRuleSetIndex = c.activeRuleSetIndex;
            graph = c.graph;
            foreach (var d in c.prevStates)
                prevStates.Add(d);
            foreach (var opt in c.recipe)
                recipe.Add(opt);
            foreach (var f in c.performanceParams)
                performanceParams.Add(f);
            foreach (var f in c.designParameters)
                designParameters.Add(f);
            foreach (var a in c.GenerationStatus)
                GenerationStatus.Add(a);

            Sequence = new AssemblySequence();
        }

        public string TotalTime
        {
            get { return new TimeSpan(((long)(10000000 * f3))).ToString(); }
          //  set { f3 = TimeSpan.Parse(value).TotalSeconds; }
        }
        public string MakeSpan
        {
            get { return new TimeSpan(((long)(10000000 * performanceParams.Last()))).ToString(); }
           // set { performanceParams.Last() = TimeSpan.Parse(value).TotalSeconds; }
        }
        public double TimeScore //OrderScore
        {
            get { return f0; }
            set { f0 = value; }
        }
        public double AccessibilityScore
        {
            get { return f1; }
            set { f1 = value; }
        }
        public double StabilityScore
        {
            get { return f2; }
            set { f2 = value; }
        }

        //public double PathLength
        //{
        //    get { return performanceParams[5]; }
        //    set
        //    {
        //        while (performanceParams.Count <= 5) performanceParams.Add(double.NaN);
        //        performanceParams[5] = value;
        //    }
        //}


        public override candidate copy()
        {
            var ac = new AssemblyCandidate(base.copy());
            ac.Sequence = Sequence.copy();
            return ac;
        }
        public void SaveToDisk(string filename)
        {
            // c1.graph.checkForRepeatNames();
            StreamWriter candidateWriter = null;
            try
            {
                candidateWriter = new StreamWriter(filename);
                var candidateSerializer = new XmlSerializer(typeof(AssemblyCandidate));
                candidateSerializer.Serialize(candidateWriter, this);
            }
            catch (Exception ioe)
            {
                SearchIO.output("***XML Serialization Error***");
                SearchIO.output(ioe.ToString());
            }
            finally
            {
                if (candidateWriter != null) candidateWriter.Close();
            }
        }

    }
}
