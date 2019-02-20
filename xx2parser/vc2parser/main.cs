using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using xx2lib;

namespace vc2parser
{
    class main
    {
        static void Main(string[] args)
        {
            // Expect 2 parameters, the first of which is "shell"
            if (args.Length != 2 || args[0] != "shell")
                return;

            // Create a vc2parser and a proxy, and link them up
            Func<vc2parser> parser = (() => new vc2parser());

            //parser().Execute(args[1], @"c:\temp\outfile.json");
            //return;

            try
            {
                var pxy = new MergeProxy(args[1], parser);

                // Start the proxy
                pxy.Execute();
            }
            catch (Exception e)
            {
                File.WriteAllText(@"C:\temp\vc2parser.dump.txt", e.ToString());
                throw;
            }

            
        }
    }
}
