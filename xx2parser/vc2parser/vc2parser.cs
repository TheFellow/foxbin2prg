using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using xx2lib;

namespace vc2parser
{
    class vc2parser : xx2parser
    {
        public override bool Execute(string sourceFile, string outputFile)
        {
            if (!File.Exists(sourceFile)) return false;



            return false;
        }
    }
}
