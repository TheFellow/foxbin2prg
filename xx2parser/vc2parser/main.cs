using System;
using System.Collections.Generic;
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
            var vc2 = new vc2parser();
            var pxy = new MergeProxy(args[1], vc2.Execute);

            // Start the proxy
            pxy.Execute();
        }
    }
}
