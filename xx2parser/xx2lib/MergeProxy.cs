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
        private string flagFile;
        Func<string, string, bool> parse;

        public MergeProxy(string flagFile, Func<string, string, bool> parse)
        {
            this.flagFile = flagFile;
            this.parse = parse;
        }

        public void Execute()
        {
            // Create the flag file
            File.Create(flagFile);

            // Stay in a loop and delegate to events
            string line;

            while((line = Console.In.ReadLine()) != "end" && !string.IsNullOrEmpty(line))
            {
                string sourceFile = line;
                string outputFile = Console.In.ReadLine();
                bool success = true;

                // Invoke the delegate
                success = parse(sourceFile, outputFile);

                if (success)
                    success = File.Exists(outputFile);

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
            }
        }
    }
}
