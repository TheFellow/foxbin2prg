using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace xx2lib
{
    public abstract class xx2node
    {
        public string type;
        public string name;
        public LocationSpan locationSpan;
    }

    public abstract class xx2containerbase : xx2node
    {
        public Span footerSpan = new Span { begin = 0, end = -1 };
        public List<xx2node> children = new List<xx2node>();

        public xx2leaf AddLeaf(string type, string name, LocationSpan locationSpan, Span span)
        {
            var leaf = new xx2leaf()
            {
                type = type,
                name = name,
                locationSpan = locationSpan,
                span = span
            };

            children.Add(leaf);
            return leaf;
        }

        public xx2container AddChild(string type, string name, LocationSpan locationSpan, Span? headerSpan = null, Span? footerSpan = null)
        {
            var child = new xx2container()
            {
                type = type,
                name = name,
                locationSpan = locationSpan
            };

            if (headerSpan.HasValue)
                child.headerSpan = headerSpan.Value;

            if (footerSpan.HasValue)
                child.footerSpan = footerSpan.Value;

            children.Add(child);
            return child;
        }

        public void Serialize(string outputFile)
        {
            StringWriter sw = new StringWriter();
            JsonTextWriter json = new JsonTextWriter(sw);

            // Begin writing the file
            json.WriteStartObject();

            json.WriteEntry("type", type);
            json.WriteEntry("name", name);
            json.WriteLocationSpan(locationSpan);
            json.WriteRange("footerSpan", footerSpan.begin, footerSpan.end);
            json.WriteEntry("parsingErrorsDetected", false);

            if(children.Count > 0)
            {
                json.WritePropertyName("children");
                json.WriteStartArray();
                SerializeChildren(children, json);
                json.WriteEndArray();
            }

            json.WriteEndObject();
            

            File.WriteAllText(outputFile, sw.ToString());
        }

        private void SerializeChildren(List<xx2node> children, JsonTextWriter json)
        {
            foreach(var child in children)
            {
                if(child is xx2leaf)
                {
                    // Serialize a leaf node
                    var leaf = child as xx2leaf;
                    json.WriteStartObject();

                    json.WriteEntry("type", leaf.type);
                    json.WriteEntry("name", leaf.name);
                    json.WriteLocationSpan(leaf.locationSpan);
                    json.WriteRange("span", leaf.span.begin, leaf.span.end);

                    json.WriteEndObject();
                }
                else // child is xx2container
                {
                    // Serialize a container
                    var ctr = child as xx2container;

                    json.WriteStartObject();

                    json.WriteEntry("type", ctr.type);
                    json.WriteEntry("name", ctr.name);
                    json.WriteLocationSpan(ctr.locationSpan);
                    json.WriteRange("headerSpan", ctr.headerSpan.begin, ctr.headerSpan.end);
                    json.WriteRange("footerSpan", ctr.footerSpan.begin, ctr.footerSpan.end);

                    json.WritePropertyName("children");
                    json.WriteStartArray();
                    SerializeChildren(ctr.children, json);
                    json.WriteEndArray();

                    json.WriteEndObject();
                }
            }
        }
    }

    public class xx2file : xx2containerbase
    {
        public bool parsingErrorsDetected = false;
    }

    public class xx2container : xx2containerbase
    {
        public Span headerSpan;
    }

    public class xx2leaf : xx2node
    {
        public Span span;
    }
}
