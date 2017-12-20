using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Assembly_Planner
{
    static class PartNameAnalysis
    {

        static internal float stringInclusionDistance( string term, string content )
        {

            string grid = "";
            float best = stringDistance(term, content, ref grid);
            float temp;

            int maxCharAdds = 2;
            int minChars = 3;

            int widthPos = minChars;
            int widthLim = term.Length + maxCharAdds;

            if (widthLim > content.Length)
            {
                widthLim = content.Length;
            }
            if (widthPos > content.Length)
            {
                widthPos = content.Length;
            }

            int pos;
            int lim;
            string match="";
            

            while(widthPos <= widthLim)
            {
                pos = 0;
                lim = content.Length - widthPos;
                while (pos < lim)
                {
                    temp = stringDistance(term, content.Substring(pos, widthPos),ref grid);

                    /*if (term.Equals(content.Substring(pos, widthPos)))
                    {
                        Console.WriteLine("-->"+content.Substring(pos, widthPos));
                        Console.Write(grid);
                        Console.WriteLine(best.ToString());
                    }*/
                    
                    if (temp < best)
                    {
                        match = content.Substring(pos, widthPos);
                        best = temp;
                        //Console.Write(grid);
                        //Console.WriteLine(best.ToString());
                        
                    }
                    pos++;
                }
                widthPos++;
            }
            //Console.Write("With term '" + term + "' and content '" + content + "'\n   yeilded distance : " 
                         // + best.ToString() + " with match " + match + "\n\n\n");
            return best;
        }

        static float stringDistance( string term, string content, ref string grid)
        {

            List <float> diffGrid = new List<float>((term.Length+1) * (content.Length+1));

            float delCost = 0.8f;
            float insCost = 2.0f;
            float subCost = 2.0f;

            int termPos = 0;
            int termLim = term.Length;
            int contentPos = 0;
            int contentLim = content.Length;
            float cost;

            

            while(termPos <= termLim)
            {
                contentPos = 0;
                while (contentPos <= contentLim)
                {

                    if( (termPos > 0) && (contentPos > 0) )
                    {
                        if (term[termPos-1] == content[contentPos-1])
                        {
                            cost = 0;
                        }
                        else
                        {
                            if (term[termPos - 1] == 'a' &&
                                term[termPos - 1] == 'e' &&
                                term[termPos - 1] == 'i' &&
                                term[termPos - 1] == 'o' &&
                                term[termPos - 1] == 'u' &&
                                term[termPos - 1] == 'y' 
                                )
                            {
                                cost = 0.4f;
                            }
                            else
                            {
                                cost = 1.0f;
                            }
                        }
                    }
                    else
                    {
                        cost = 1;
                    }
                    
                    
                    if (contentPos == 0)
                    {
                        diffGrid.Add( termPos * delCost );
                    }
                    else if (termPos == 0)
                    {
                        diffGrid.Add( contentPos * insCost );
                    }
                    else
                    {
                        diffGrid.Add( 
                            Math.Min( diffGrid[contentPos + (termPos-1) * (contentLim+1)] + delCost,
                                  Math.Min ( diffGrid[contentPos - 1  + termPos * (contentLim+1)] + insCost,
                                             diffGrid[contentPos - 1 + (termPos-1) * (contentLim+1)] + subCost*cost
                                           )
                            )
                        );
                    }
                    contentPos++;
                }
                termPos++;
            }
            grid = printGrid(diffGrid, contentLim + 1);
            float result = diffGrid.Last()/(2.0f*Math.Max(term.Count(),content.Count()));
            return result;
        }


        static string printGrid(List<float> grid, int width)
        {
            string result="";
            int lPos = 0;
            result = string.Concat( result,"\n-");
            while (lPos < width)
            {
                result= String.Concat(result,"--");
                lPos++;
            }
            lPos = 0;
            while (lPos < grid.Count)
            {
                if(lPos%width == 0)
                {
                    result = String.Concat(result,"\n| ");
                }
                result = String.Concat(result,grid[lPos].ToString()+" ");
                lPos++;
            }
            return result;
        }

        static internal void findCommonPreSuffixes(List<string> stringList, ref int preCutoff, ref int sufCutoff)
        {

            int pos = 1;
            int prePos = 0;
            int preLim = stringList[0].Count();
            int lim = stringList.Count;
            while (pos < lim)
            {
                if (stringList[pos].Count() < preLim)
                {
                    preLim = stringList[pos].Count();
                }
                prePos = 0;
                while (prePos < preLim)
                {
                    if (stringList[pos][prePos] != stringList[0][prePos])
                    {
                        preLim = prePos;
                        break;
                    }
                    prePos++;
                }
                pos++;
            }
            int sufPos = 0;
            int sufLim = stringList[0].Count();
            pos = 1;
            while (pos < lim)
            {
                if (stringList[pos].Count() < sufLim)
                {
                   sufLim = stringList[pos].Count();
                }
                sufPos = 0;
                while (sufPos < sufLim)
                {
                    if (stringList[pos][stringList[pos].Count() - 1 - sufPos] != stringList[0][stringList[0].Count() - 1 - sufPos])
                    {
                        sufLim = sufPos;
                        break;
                    }
                   sufPos++;
                }
                pos++;
            }

            preCutoff = preLim;
            sufCutoff = sufLim;

        }


        internal static float SolidHasFastenerKeyword(TVGL.TessellatedSolid solid, int preCutoff, int postCutoff)
        {
            string[] keyWordList ={ "screw",
                                    "washer",
                                    "bolt",
                                    "nut",
                                    "rivet",
                                    "grommet",
                                    "fastener"
            };
            int pos = 0;
            int lim = keyWordList.Length;
            float best=1000.0f;
            while (pos < lim)
            {
                //Console.WriteLine(solid.Name);
                best = Math.Min ( best,
                                  PartNameAnalysis.stringInclusionDistance(
                                      keyWordList[pos].ToLower(),
                                      (solid.Name.ToLower()).Substring(preCutoff, solid.Name.Count() - postCutoff - preCutoff)
                                  )
                );
                pos++;
            }

            return best;
        }


    }
}


