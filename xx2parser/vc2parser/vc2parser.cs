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

        public int length => lines.Length;
        public int lastLine => lines.Length - 1;
        public string line => lines[index];
        public LocationSpan currentLineLocationSpan => new LocationSpan { startRow = index + 1, startCol = 0, endRow = index + 1, endCol = info[index].length };
        public Span currentLineSpan => new Span { begin = info[index].begin, end = info[index].end };

        int index = 0;

        #region Constants

        public const string defineClass = @"DEFINE CLASS ";
        public const string externalClassHeader = @"*-- EXTERNAL_CLASS ";
        public const string externalClass = @"*< EXTERNAL_CLASS: ";
        public const string objectDataHeader = @"	*-- OBJECTDATA ";
        public const string objectData = @"	*< OBJECTDATA: ";
        public const string addObject = @"	ADD OBJECT";

        #endregion

        public override bool Execute(string sourceFile, string outputFile)
        {
            if (!File.Exists(sourceFile)) return false;

            LoadFile(sourceFile);

            // Parse the source file
            var vc2 = Parse(sourceFile);

            // Export the parsed json
            vc2.Serialize(outputFile);

            return true;
        }

        internal xx2file Parse(string sourceFile)
        {
            // Always start at the beginning!
            index = 0;

            var vc2 = new xx2file
            {
                name = sourceFile,
                locationSpan = new LocationSpan
                {
                    startRow = 1,
                    startCol = 0,
                    endRow = length,
                    endCol = info[lastLine].length
                }
            };

            // Determine file type and parse accordingly
            ParseVC2Header(vc2);

            if (line.StartsWith(externalClass))
            {
                ParseVC2ClassLib(vc2);
            }
            else
            {
                while(index <= lastLine && line.StartsWith(defineClass))
                {
                    ParseVC2Class(vc2);
                }
            }

            ParseVC2Footer(vc2);

            return vc2;
        }

        private void ParseVC2ClassLib(xx2file vc2)
        {
            while (line.StartsWith(externalClass))
            {
                int start = line.IndexOf('"') + 1;
                int end = line.IndexOf('"', start);
                string name = line.Substring(start, end - start);

                vc2.AddLeaf("class", name, currentLineLocationSpan, currentLineSpan);

                index++;
            }
        }

        private void ParseVC2Class(xx2file vc2)
        {
            throw new NotImplementedException();
        }

        private void ParseVC2Header(xx2file vc2)
        {
            while (line.StartsWith("*") && !line.StartsWith(externalClass)) index++;

            vc2.AddLeaf("vc2 header", "file header",
                new LocationSpan { startRow = 1, startCol = 0, endRow = index, endCol = info[index - 1].length },
                new Span { begin = 0, end = info[index - 1].end }
                );
        }

        private void ParseVC2Footer(xx2file vc2)
        {
            if (index < length - 1)
            {
                vc2.footerSpan = new Span { begin = info[index].begin, end = info[lastLine].end };
            }
        }

        private void LoadFile(string sourceFile)
        {
            lines = File.ReadAllLines(sourceFile);
            info = new lineinfo[lines.Length];

            int index = 0, offset = 0;

            foreach(string line in lines)
            {
                info[index].begin = offset;
                info[index].end = offset + line.Length - 1 + ((index == lastLine) ? 0 : 2); // +2-1 for CRLF and inclusive length
                info[index].length = line.Length + ((index == lastLine) ? 0 : 2); // CRLF

                offset += info[index].length;
                index++;
            }
        }
    }
}
