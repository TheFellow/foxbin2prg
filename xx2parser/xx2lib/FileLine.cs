using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xx2lib
{
    public struct FileLine
    {
        public string line;
        public int begOffset, endOffset, length;

        public FileLine(string line, int begOffset, int endOffset, int length)
        {
            this.line = line;
            this.begOffset = begOffset;
            this.endOffset = endOffset;
            this.length = length;
        }
    }
}
