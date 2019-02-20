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
        public string line => length == 0 ? string.Empty : lines[index];
        public LocationSpan currentLineLocationSpan => new LocationSpan { startRow = index + 1, startCol = 0, endRow = index + 1, endCol = info[index].length };
        public Span currentLineSpan => new Span { begin = info[index].begin, end = info[index].end };
        public Span currentLineEmptySpan => new Span { begin = info[index].begin, end = info[index].begin - 1 };

        public bool group = false;
        public List<xx2container> groupList = new List<xx2container>();

        private int index = 0;

        #region Constants

        public const string defineClass =           @"DEFINE CLASS ";
        public const string endDefineClass =        @"ENDDEFINE";
        public const string include =               @"	#INCLUDE ";
        public const string externalClassHeader =   @"*-- EXTERNAL_CLASS ";
        public const string externalClass =         @"*< EXTERNAL_CLASS: ";
        public const string objectDataHeader =      @"	*-- OBJECTDATA ";
        public const string objectData =            @"	*< OBJECTDATA: ObjPath=""";
        public const string addObject =             @"	ADD OBJECT ";
        public const string endObject =             @"		*< END OBJECT: ";
        public const string definePropArrayMethod   = @"	*<DefinedPropArrayMethod>";
        public const string undefinePropArrayMethod = @"	*</DefinedPropArrayMethod>";
        public const string pemMethod =             @"		*m: ";
        public const string pemProperty =           @"		*p: ";
        public const string pemArray =              @"		*a: ";
        public const string hiddenPropList =        @"	HIDDEN ";
        public const string protectedPropList =     @"	PROTECTED ";
        public const string protectedProc =         @"	PROTECTED PROCEDURE";
        public const string hiddenProc =            @"	HIDDEN PROCEDURE";
        public const string procDef =               @"	PROCEDURE ";
        public const string endProc =               @"	ENDPROC";

        #endregion

        public override bool Execute(string sourceFile, string outputFile)
        {
            if (!File.Exists(sourceFile)) return false;

            LoadFile(sourceFile);

            // Parse the source file
            var vc2 = Parse(sourceFile);

            // Group objects into parents if enabled
            GroupVC2();

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
                    endCol = lastLine < 0 ? 0 : info[lastLine].length
                }
            };

            if(lastLine >= 0)
            {
                // Determine file type and parse accordingly
                ParseVC2Header(vc2);

                if (lineStartsWith(externalClass))
                {
                    ParseVC2ClassLib(vc2);
                }
                else
                {
                    while (index <= lastLine && lineStartsWith(defineClass))
                    {
                        ParseVC2Class(vc2);
                    }
                }

                ParseVC2Footer(vc2);
            }

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

            // Check for an include of a header file
            if (lineStartsWith(include)) ParseVC2Include(ctr);

            // Check for Object Data
            if (lineStartsWith(objectDataHeader)) ParseVC2ObjectData(ctr);

            // Check for Defined Prop Array Method
            if (lineStartsWith(definePropArrayMethod)) ParseVC2DefinedPropArrayMethod(ctr);

            // Check for Hidden/Protected/property sheet value
            if ((lineStartsWith(hiddenPropList) || lineStartsWith(protectedPropList) || line[line.IndexOf(' ') + 1] == '=')
                    && !(lineStartsWith(hiddenProc) || lineStartsWith(protectedProc)))
                ParseVC2PropertyValues(ctr);

            // Check for Add Object(s)
            if (lineStartsWith(addObject)) ParseVC2AddObjects(ctr);

            // Check for procedures
            if (lineStartsWith(procDef) || lineStartsWith(hiddenProc) || lineStartsWith(protectedProc)) ParseVC2Procedures(ctr);

            if (line == endDefineClass)
            {
                int startIndex = index++; // Skip the enddefine
                while (index < length && line.Trim() == string.Empty) index++;

                ctr.locationSpan.endRow = index;
                ctr.locationSpan.endCol = info[index - 1].length;
                ctr.footerSpan = new Span { begin = info[startIndex].begin, end = info[index - 1].end };
            }
        }

        private bool lineStartsWith(string value) => line.Trim().StartsWith(value.Trim());

        private void ParseVC2Include(xx2container ctr)
        {
            var includes = ctr.AddLeaf("include", line.Substring(include.Length),
                currentLineLocationSpan,
                currentLineSpan);

            index++; // Skip this line
            while (line.Trim() == string.Empty) index++;
        }

        private void ParseVC2Procedures(xx2container ctr)
        {
            var meths = ctr.AddChild("methods", "method implementations",
                    currentLineLocationSpan,
                    currentLineEmptySpan);

            groupList.Add(meths);

            while(lineStartsWith(procDef) || lineStartsWith(hiddenProc) || lineStartsWith(protectedProc))
            {
                // Determine the procedure name
                string procName;

                if (lineStartsWith(procDef))
                    procName = line.Trim().Split()[1];
                else
                    procName = line.Trim().Split()[2];

                int startIndex = index;

                while (!lineStartsWith(endProc)) index++;

                index++; // Skip the ENDPROC line
                while (line.Trim() == string.Empty) index++;

                meths.AddLeaf("method implementation", procName,
                        new LocationSpan { startRow = startIndex + 1, startCol = 0, endRow = index, endCol = info[index - 1].length },
                        new Span { begin = info[startIndex].begin, end = info[index - 1].end })
                    .groupType = "object";
            }

            // Finish meths.
            // Note: No footer
            meths.locationSpan.endRow = index;
            meths.locationSpan.endCol = info[index - 1].length;
        }

        #region ADD OBJECT

        private string AddObjects_GetName()
        {
            int begin = line.IndexOf('\'') + 1;
            int end = line.IndexOf('\'', begin);
            string name = line.Substring(begin, end - begin);
            return name;
        }

        private void ParseVC2AddObjects(xx2container ctr)
        {
            var add = ctr.AddChild("objects", ctr.name + " objects",
                currentLineLocationSpan,
                currentLineEmptySpan);

            groupList.Add(add);

            while (lineStartsWith(addObject))
            {
                string name = AddObjects_GetName();

                var ctrl = add.AddChild("object", name, currentLineLocationSpan, currentLineSpan);
                groupList.Add(ctrl);

                index++; // Skip header

                while (!lineStartsWith(endObject))
                {
                    string propName = line.Substring(0, line.IndexOf('=')).Trim();
                    ctrl.AddLeaf("property", propName, currentLineLocationSpan, currentLineSpan);
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

        #endregion

        #region Property Sheet Values

        private void ParseVC2PropertyValues(xx2container ctr)
        {
            var props = ctr.AddChild("properties", "property values", currentLineLocationSpan, currentLineEmptySpan);

            groupList.Add(props);

            if(lineStartsWith(hiddenPropList) || lineStartsWith(protectedPropList))
            {
                var scope = props.AddChild("visibility", "visibility", currentLineLocationSpan, currentLineEmptySpan);

                // Check for HIDDEN property list
                if (lineStartsWith(hiddenPropList))
                {
                    scope.AddLeaf("hidden", "hidden property list", currentLineLocationSpan, currentLineSpan);
                    index++;
                }

                // Check for PROTECTED property list
                if (lineStartsWith(protectedPropList))
                {
                    scope.AddLeaf("protected", "protected property list", currentLineLocationSpan, currentLineSpan);
                    index++;
                }

                scope.locationSpan.endRow = index;
                scope.locationSpan.endCol = info[index - 1].length;
            }

            while(line.Trim() != string.Empty)
            {
                string name = line.Substring(0, line.IndexOf(' ')).Trim();

                if(name.ToUpperInvariant() == "_MemberData".ToUpperInvariant())
                {
                    int startIndex = index;
                    while (!line.ToLowerInvariant().Contains(@"</VFPData>".ToLowerInvariant())) index++;

                    props.AddLeaf(
                            "property",
                            name,
                            new LocationSpan { startRow = startIndex + 1, startCol = 0, endRow = index + 1, endCol = info[index - 1].length },
                            new Span { begin = info[startIndex].begin, end = info[index - 1].end })
                        .groupType = "object";
                }
                else
                {
                    props.AddLeaf(
                            "property",
                            name,
                            currentLineLocationSpan,
                            currentLineSpan
                        ).groupType = "object";
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

        #endregion

        #region PEM Declarations

        private void ParseVC2DefinedPropArrayMethod(xx2container ctr)
        {
            var pems = ctr.AddChild("PEM", "PEM", currentLineLocationSpan, currentLineSpan);
            index++;

            // When there are arrays and _memberdata, the _memberdata is added last
            // as a property, and comes after the array definitions. So, we'll just repeat this until
            // we find a closing item
            
            while (!lineStartsWith(undefinePropArrayMethod))
            {
                HandlePEM(pems, pemMethod, "method", "Methods");
                HandlePEM(pems, pemProperty, "property", "Properties");
                HandlePEM(pems, pemArray, "array", "Arrays");
            }

            // footer
            int startIndex = index;
            index++;

            while (line.Trim() == string.Empty) index++;

            pems.locationSpan.endRow = index;
            pems.locationSpan.endCol = info[index - 1].length;

            pems.footerSpan = new Span { begin = info[startIndex].begin, end = info[index - 1].end };

        }

        private void HandlePEM(xx2container pems, string startsWith, string typeName, string typeNamePlural)
        {
            if (lineStartsWith(startsWith))
            {
                var def = pems.AddChild("PEM declaration", typeNamePlural, currentLineLocationSpan, currentLineEmptySpan);

                while (lineStartsWith(startsWith))
                {
                    string name = line.Trim().Split()[1];

                    def.AddLeaf($"{typeName} declaration", name, currentLineLocationSpan, currentLineSpan);
                    index++;
                }

                def.locationSpan.endRow = index;
                def.locationSpan.endCol = info[index - 1].length;
            }
        }

        #endregion

        #region OBJECT DATA (ZOrder)

        private string ObjectData_GetName()
        {
            int start = line.IndexOf('"') + 1;
            int end = line.IndexOf('"', start);
            string name = line.Substring(start, end - start);
            return name;
        }

        private void ParseVC2ObjectData(xx2container vc2)
        {
            var ctr = vc2.AddChild("zorder", "object zorder data", currentLineLocationSpan, currentLineSpan);
            index++;

            groupList.Add(ctr);

            // Add a leaf for each object data
            while (lineStartsWith(objectData))
            {
                string name = ObjectData_GetName();

                // Not nested
                var leaf = ctr.AddLeaf("zorder", name, currentLineLocationSpan, currentLineSpan);
                leaf.groupType = "object";

                index++;
            }

            int startIndex = index;

            while (line.Trim() == string.Empty) index++;

            ctr.locationSpan.endRow = index;
            ctr.locationSpan.endCol = info[index - 1].length;

            ctr.footerSpan = new Span { begin = info[startIndex].begin, end = info[index - 1].end };
        }

        #endregion

        private void ParseVC2Header(xx2file vc2)
        {
            while (line.StartsWith("*") && !lineStartsWith(externalClass)) index++;

            if (index == 0)
                return;

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
            while (lineStartsWith(externalClass))
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
            lines = File.ReadAllLines(sourceFile, Encoding.GetEncoding(1252));
            //File.WriteAllLines($@"c:\temp\{Path.GetFileName(sourceFile)}.txt", lines);
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

        #region Grouping by parent

        private void GroupVC2()
        {
            if (!group) return;

            foreach(var context in groupList)
            {
                // Get a list of every node name that should be a container
                var containers = TreeInfo.GroupTree(context.children.Select(n => n.name).ToArray());
                foreach (string parentName in containers)
                {
                    int beg = -1, end = -1;

                    for (int i = 0; i < context.children.Count; i++)
                        if (context.children[i].name.StartsWith(parentName + '.'))
                        {
                            if (beg < 0) beg = i;
                            end = i;
                        }

                    if (beg < 0 || end < 0)
                        continue;

                    // Extract and replace the range with a container
                    var leaves = context.children.GetRange(beg, end - beg + 1);         // Save copy of "leaves"
                    if (end > beg) context.children.RemoveRange(beg + 1, end - beg);    // Remove excess "leaves"

                    int last = end - beg;
                    Span newSpan;

                    if (leaves[0] is xx2leaf)
                        newSpan = ((xx2leaf)leaves[0]).span;
                    else
                        newSpan = ((xx2container)leaves[0]).headerSpan;

                    newSpan.end = newSpan.begin - 1;

                    string containerName = GetParentName(leaves[0].name);

                    // Should we change the type during grouping?
                    string groupType;
                    if (leaves[0] is xx2leaf)
                        groupType = ((xx2leaf)leaves[0]).groupType ?? leaves[0].type;
                    else
                        groupType = ((xx2container)leaves[0]).groupType ?? leaves[0].type;

                    // Fix the leaf names
                    foreach (var item in leaves)
                        item.name = GetChildName(item.name);

                    // Replace with new container
                    context.children[beg] = new xx2container
                    {
                        type = groupType,
                        name = containerName,
                        locationSpan = new LocationSpan
                        {
                            startRow = leaves[0].locationSpan.startRow,
                            startCol = leaves[0].locationSpan.startCol,
                            endRow = leaves[last].locationSpan.endRow,
                            endCol = leaves[last].locationSpan.endCol
                        },
                        headerSpan = newSpan,
                        children = leaves
                    };
                }
            }
        }

        private string GetParentName(string path)
        {
            int index = path.LastIndexOf('.');
            if (index < 0)
                return path;

            return path.Substring(0, index);
        }

        private string GetChildName(string path)
        {
            int index = path.LastIndexOf('.');
            if (index < 0)
                return path;
            return path.Substring(index + 1);
        }

        #endregion
    }
}

/*
string[] currName = context.children[childIndex].name.Split('.');
xx2container[] currCtr = new xx2container[currName.Length];

// See how many match
int i;
for (i = 0; i < currName.Length; i++)
{
    if (i >= prevCtr.Length || prevName[i] != currName[i])
        break;

    currCtr[i] = prevCtr[i];
}

// Add a child up to the parent
for (; i < currName.Length - 1; i++)
{
    var o = (i == 0 ? ctr : currCtr[i - 1]).AddChild(
        "ZOrder",
        currName[i],
        currentLineLocationSpan,
        currentLineEmptySpan);

    currCtr[i] = o;
}

// If the next line starts with me, then I should be a container
// otherwise I should be a leaf
if (index + 1 < lastLine && lines[index + 1].StartsWith(objectData + name))
{
    currCtr[i] = (i == 0 ? ctr : currCtr[i - 1]).AddChild("ZOrder", currName[i], currentLineLocationSpan, currentLineSpan);
}
else
{
    var c = (i == 0 ? ctr : currCtr[i - 1]);

    c.AddLeaf("ZOrder", (i == 0 ? "" : c.name + ".") + currName[i], currentLineLocationSpan, currentLineSpan);

    for (int j = 0; j < i; j++)
    {
        currCtr[j].locationSpan.endRow = index + 1;
        currCtr[j].locationSpan.endCol = info[index].length;
    }
}

prevCtr = currCtr;
prevName = currName;
*/
