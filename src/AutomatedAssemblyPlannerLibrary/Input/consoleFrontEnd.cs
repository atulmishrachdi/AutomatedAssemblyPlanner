using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Assembly_Planner
{
    class consoleFrontEnd
    {

        public static string getPartsDirectory()
        {

            bool done = false;
            string theDir = Directory.GetCurrentDirectory();
            string theInput;

            while (!done)
            {

                Console.Write("\n\nEnter assembly directory path (relative):");
                Console.Write("\n>>");
                theInput=Console.ReadLine();
                try
                {
                    if (directoryHasModelFiles(theInput))
                    {
                        return theInput;
                    }
                    else
                    {
                        Console.Write("\n\nGiven directory does not contain valid part files");
                        lookForSomeModels(theInput);
                    }
                }
                catch (Exception)
                {
                    Console.Write("\n\nInvalid Path\n\n");
                }

            }

            return "Impossible Return Value";

        }

        private static bool directoryHasModelFiles(string theDirectory)
        {
            string[] files = Directory.GetFiles(theDirectory);
            string theExtension;
            int pos = 0;
            int lim = files.Length;
            while (pos < lim)
            {

                theExtension=grabExtension(files[pos]);
                theExtension=theExtension.ToLower();

                if ( theExtension.Equals("stl") ||
                    theExtension.Equals("3mf") ||
                    theExtension.Equals("off") ||
                    theExtension.Equals("ply"))
                {
                    return true;
                }
                pos++;
            }

            return false;

        }

        private static string grabExtension(string theFileName)
        {
            int pos = 0;
            int lim = theFileName.Length;
            int last=-1;
            while(pos < lim)
            {
                if (theFileName[pos] == '.')
                {
                    last = pos;
                }
                pos++;
            }

            if(last==lim-1 || last == -1)
            {
                return "";
            }
            else
            {
                return theFileName.Substring(last+1);
            }

        }

        private static void lookForSomeModels(string theDirectory)
        {
            Console.Write("\nSubdirectories containing valid parts files:\n");
            bool done = false;

            List<string> directories=new List<string>();
            directories.Add(theDirectory);
            List<string> directoryHolder=new List<string>();
            List<string> subDirectories= new List<string>();
            string holder;

            while(!done)
            {
                subDirectories.Clear();
                foreach(string dir in directories)
                {

                    directoryHolder.Clear();
                    directoryHolder = (Directory.GetDirectories(dir)).ToList<string>();

                    foreach(string subdir in directoryHolder)
                    {
                       
                        subDirectories.Add(subdir);

                    }

                }

                directories = subDirectories;

                if (directories.Count != 0)
                {
                    foreach (string dir in directories)
                    {
                        Console.Write("\n" + dir);

                    }
                }
                else
                {
                    done = true;
                }
                

            }
        }

    }
}
