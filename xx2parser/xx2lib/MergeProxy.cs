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
        private readonly string debugFile = @"c:\temp\debug.txt";
        private bool createDebugFile = false;
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

            if (createDebugFile) File.WriteAllText(debugFile, "Enter Execute()");

            while((line = Console.In.ReadLine()) != "end" && !string.IsNullOrEmpty(line))
            {
                if (createDebugFile) File.AppendAllText(debugFile, "\nBegin Loop");

                string sourceFile = line;
                string encoding = Console.In.ReadLine();
                string outputFile = Console.In.ReadLine();
                bool success = true;

                // Sanitize the ridiculous extra slashes in the file path
                sourceFile = sourceFile.Replace(@"\\", @"\");
                outputFile = outputFile.Replace(@"\\", @"\");

                if (createDebugFile) File.AppendAllText(debugFile, " source: " + sourceFile);
                if (createDebugFile) File.AppendAllText(debugFile, " target: " + outputFile);

                // Invoke the delegate
                success = parser().Execute(sourceFile, outputFile);

                if (success)
                    success = File.Exists(outputFile);

                if (createDebugFile) File.AppendAllText(debugFile, $"\n success = {success}");

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

                if (createDebugFile) File.AppendAllText(debugFile, "\nEnd Loop");
            }
        }
    }
}
