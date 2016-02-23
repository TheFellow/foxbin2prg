using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using xx2lib;

namespace vc2parser
{
    struct lineinfo
    {
        public int begin, end, length;
    }

    class vc2parser : xx2parser
    {
        string[] lines;
        lineinfo[] info;

        public override bool Execute(string sourceFile, string outputFile)
        {
            if (!File.Exists(sourceFile)) return false;

            LoadFile(sourceFile);

            // Parse the source file


            return false;
        }

        private void LoadFile(string sourceFile)
        {
            lines = File.ReadAllLines(sourceFile);
            info = new lineinfo[lines.Length];

            int index = 0, offset = 0;

            foreach(string line in lines)
            {
                info[index].begin = offset;
                info[index].end = offset + line.Length + 1; // +2-1 for CRLF and inclusive length
                info[index].length = line.Length + 2; // CRLF

                offset += info[index].length;
                index++;
            }
        }
    }
}
