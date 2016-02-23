using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xx2lib
{
    public class MergeProxy
    {
        private string debugFile = @"c:\temp\debug.txt";
        private string flagFile;
        Func<xx2parser> parser;

        public MergeProxy(string flagFile, Func<xx2parser> parser)
        {
            this.flagFile = flagFile;
            this.parser = parser;
        }

        public void Execute()
        {
            // Create the flag file
            File.Create(flagFile);

            // Stay in a loop and delegate to events
            string line;

            //File.WriteAllText(debugFile, "Enter Execute()");

            while((line = Console.In.ReadLine()) != "end" && !string.IsNullOrEmpty(line))
            {
                //File.AppendAllText(debugFile, "\nBegin Loop");

                string sourceFile = line;
                string outputFile = Console.In.ReadLine();
                bool success = true;

                //File.AppendAllText(debugFile, " source: " + sourceFile);
                //File.AppendAllText(debugFile, " target: " + outputFile);

                // Invoke the delegate
                success = parser().Execute(sourceFile, outputFile);

                if (success)
                    success = File.Exists(outputFile);

                //File.AppendAllText(debugFile, $"\n success = {success}");

                // Respond to the merge tool
                if (success)
                {
                    Console.Out.WriteLine("OK");
                }
                else
                {
                    Console.Out.WriteLine("KO");
                    break;
                }

                //File.AppendAllText(debugFile, "\nEnd Loop");
            }
        }
    }
}
