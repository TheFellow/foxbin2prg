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

        public override string ToString() => $"begin: {begin} end: {end} length: {length}";
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
        public const string endDefineClass = @"ENDDEFINE";
        public const string externalClassHeader = @"*-- EXTERNAL_CLASS ";
        public const string externalClass = @"*< EXTERNAL_CLASS: ";
        public const string objectDataHeader = @"	*-- OBJECTDATA ";
        public const string objectData = @"	*< OBJECTDATA: ";
        public const string addObject = @"	ADD OBJECT ";
        public const string endObject = @"		*< END OBJECT: ";
        public const string definedPropArrayMethod = @"	*<DefinedPropArrayMethod>";
        public const string pemMethod = @"		*m: ";
        public const string pemProperty = @"		*p: ";
        public const string pemArray = @"		*a: ";
        public const string hiddenPropList = @"	HIDDEN ";
        public const string protectedPropList = @"	PROTECTED ";
        public const string protectedProc = @"	PROTECTED PROCEDURE";
        public const string hiddenProc = @"	HIDDEN PROCEDURE";
        public const string procDef = @"	PROCEDURE ";
        public const string endProc = @"	ENDPROC";

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

        private void ParseVC2Class(xx2file vc2)
        {
            int headerStartIndex = index;

            var ctr = vc2.AddChild("class", line.Split()[2], currentLineLocationSpan);

            // Consume the header
            while (line.Trim() != string.Empty) index++;
            while (line.Trim() == string.Empty) index++;

            // set container header
            ctr.headerSpan = new Span { begin = info[headerStartIndex].begin, end = info[index - 1].end };

            // Check for Object Data
            if (line.StartsWith(objectDataHeader)) ParseVC2ObjectData(ctr);

            // Check for Defined Prop Array Method
            if (line.StartsWith(definedPropArrayMethod)) ParseVC2DefinedPropArrayMethod(ctr);

            // Check for Hidden/Protected/property sheet value
            if ((line.StartsWith(hiddenPropList) || line.StartsWith(protectedPropList) || line[line.IndexOf(' ') + 1] == '=')
                    && !(line.StartsWith(hiddenProc) || line.StartsWith(protectedProc)))
                ParseVC2PropertyValues(ctr);

            // Check for Add Object(s)
            if (line.StartsWith(addObject)) ParseVC2AddObjects(ctr);

            // Check for procedures
            if (line.StartsWith(procDef) || line.StartsWith(hiddenProc) || line.StartsWith(protectedProc)) ParseVC2Procedures(ctr);

            if (line == endDefineClass)
            {
                int startIndex = index++; // Skip the enddefine
                while (index < length && line.Trim() == string.Empty) index++;

                ctr.locationSpan.endRow = index;
                ctr.locationSpan.endCol = info[index - 1].length;
                ctr.footerSpan = new Span { begin = info[startIndex].begin, end = info[index - 1].end };
            }
        }

        private void ParseVC2Procedures(xx2container ctr)
        {
            var meths = ctr.AddChild("Methods", "Method definitions",
                currentLineLocationSpan,
                new Span { begin = info[index].begin, end = info[index].begin - 1 });

            while(line.StartsWith(procDef) || line.StartsWith(hiddenProc) || line.StartsWith(protectedProc))
            {
                // Determine the procedure name
                string procName;

                if (line.StartsWith(procDef))
                    procName = line.Trim().Split()[1];
                else
                    procName = line.Trim().Split()[2];

                int startIndex = index;

                while (!line.StartsWith(endProc)) index++;

                index++; // Skip the ENDPROC line
                while (line.Trim() == string.Empty) index++;

                meths.AddLeaf("Method", procName,
                    new LocationSpan { startRow = startIndex + 1, startCol = 0, endRow = index, endCol = info[index - 1].length },
                    new Span { begin = info[startIndex].begin, end = info[index - 1].end });
            }

            // Finish meths.
            // Note: No footer
            meths.locationSpan.endRow = index;
            meths.locationSpan.endCol = info[index - 1].length;
        }

        private void ParseVC2AddObjects(xx2container ctr)
        {
            var add = ctr.AddChild("Member objects", "Member objects",
                currentLineLocationSpan,
                new Span { begin = info[index].begin, end = info[index].begin - 1 });

            while (line.StartsWith(addObject))
            {
                int begin = line.IndexOf('\'') + 1;
                int end = line.IndexOf('\'', begin);
                string name = line.Substring(begin, end - begin);

                var ctrl = add.AddChild("Member", name, currentLineLocationSpan, currentLineSpan);
                index++; // Skip header

                while (!line.StartsWith(endObject))
                {
                    string propName = line.Substring(0, line.IndexOf('=')).Trim();
                    ctrl.AddLeaf("Property", propName, currentLineLocationSpan, currentLineSpan);
                    index++;
                }

                int startIndex = index;
                index++;
                while (line.Trim() == string.Empty) index++;

                ctrl.locationSpan.endRow = index;
                ctrl.locationSpan.endCol = info[index - 1].length;

                ctrl.footerSpan = new Span { begin = info[startIndex].begin, end = info[index - 1].end };
            }

            // Finish add info
            // Note: No footer span
            add.locationSpan.endRow = index;
            add.locationSpan.endCol = info[index - 1].length;
        }

        private void ParseVC2PropertyValues(xx2container ctr)
        {
            var props = ctr.AddChild("Properties", "Property values", currentLineLocationSpan, new Span { begin = info[index].begin, end=info[index].begin - 1 });

            // Check for HIDDEN property list
            if (line.StartsWith(hiddenPropList))
            {
                props.AddLeaf("Hidden", "Property list", currentLineLocationSpan, currentLineSpan);
                index++;
            }

            // Check for PROTECTED property list
            if (line.StartsWith(protectedPropList))
            {
                props.AddLeaf("Protected", "Property list", currentLineLocationSpan, currentLineSpan);
                index++;
            }

            while(line.Trim() != string.Empty)
            {
                string name = line.Substring(0, line.IndexOf(' ')).Trim();

                if(name.ToUpperInvariant() == "_MemberData".ToUpperInvariant())
                {
                    int startIndex = index;
                    while (!line.ToLowerInvariant().Contains(@"</VFPData>".ToLowerInvariant())) index++;

                    props.AddLeaf(
                        "Property",
                        name,
                        new LocationSpan { startRow = startIndex + 1, startCol = 0, endRow = index + 1, endCol = info[index - 1].length },
                        new Span { begin = info[startIndex].begin, end = info[index - 1].end });
                }
                else
                {
                    props.AddLeaf(
                        "Property",
                        name,
                        currentLineLocationSpan,
                        currentLineSpan
                        );
                }

                index++;
            }

            // Wrap up props
            int footerStartIndex = index;
            while (line.Trim() == string.Empty) index++;

            props.locationSpan.endRow = index;
            props.locationSpan.endCol = info[index - 1].length;

            props.footerSpan = new Span { begin = info[footerStartIndex].begin, end = info[index - 1].end };
        }

        private void ParseVC2DefinedPropArrayMethod(xx2container ctr)
        {
            var pems = ctr.AddChild("PEMs", "PEM list", currentLineLocationSpan, currentLineSpan);
            index++;

            HandlePEM(pems, pemMethod, "Method");
            HandlePEM(pems, pemProperty, "Property");
            HandlePEM(pems, pemArray, "Array");

            // footer
            int startIndex = index;
            index++;

            while (line.Trim() == string.Empty) index++;

            pems.locationSpan.endRow = index;
            pems.locationSpan.endCol = info[index - 1].length;

            pems.footerSpan = new Span { begin = info[startIndex].begin, end = info[index - 1].end };

        }

        private void HandlePEM(xx2container pems, string startsWith, string typeName)
        {
            if (line.StartsWith(startsWith))
            {
                var def = pems.AddChild("PEM declaration", $"{typeName}s", currentLineLocationSpan, new Span { begin = info[index].begin, end = info[index].end });

                while (line.StartsWith(startsWith))
                {
                    string name = line.Trim().Split()[1];

                    def.AddLeaf($"{typeName} declaration", name, currentLineLocationSpan, currentLineSpan);
                    index++;
                }

                def.locationSpan.endRow = index;
                def.locationSpan.endCol = info[index - 1].length;
            }
        }

        private void ParseVC2ObjectData(xx2container vc2)
        {
            var ctr = vc2.AddChild("Object data", "ZOrder object data", currentLineLocationSpan, currentLineSpan);
            index++;

            // Add a leaf for each object data
            while (line.StartsWith(objectData))
            {
                int start = line.IndexOf('"') + 1;
                int end = line.IndexOf('"', start);
                string name = line.Substring(start, end - start);

                ctr.AddLeaf("ZOrder", name, currentLineLocationSpan, currentLineSpan);
                index++;
            }

            int startIndex = index;

            while (line.Trim() == string.Empty) index++;

            ctr.locationSpan.endRow = index - 1;
            ctr.locationSpan.endCol = info[index - 1].length;

            ctr.footerSpan = new Span { begin = info[startIndex].begin, end = info[index - 1].end };
        }

        private void ParseVC2Header(xx2file vc2)
        {
            while (line.StartsWith("*") && !line.StartsWith(externalClass)) index++;

            vc2.AddLeaf("file header", "file header",
                new LocationSpan { startRow = 1, startCol = 0, endRow = index, endCol = info[index - 1].length },
                new Span { begin = 0, end = info[index - 1].end }
                );
        }

        private void ParseVC2Footer(xx2file vc2)
        {
            if (index <= length - 1)
            {
                vc2.footerSpan = new Span { begin = info[Math.Min(lastLine, index)].begin, end = info[lastLine].end };
            }
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

        private void LoadFile(string sourceFile)
        {
            lines = File.ReadAllLines(sourceFile);
            info = new lineinfo[lines.Length];

            int index = 0, offset = 0;

            //var wr = File.CreateText(@"c:\temp\lines.txt");

            foreach(string line in lines)
            {
                info[index].begin = offset;
                info[index].end = offset + line.Length - 1 + ((index == lastLine) ? 0 : 2); // +2-1 for CRLF and inclusive length
                info[index].length = line.Length + ((index == lastLine) ? 0 : 2); // CRLF

                //wr.WriteLine($"Line {index:3} {info[index]}");

                offset += info[index].length;
                index++;
            }
        }
    }
}
