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

        public void Serialize(string outputFile)
        {
            StringWriter sw = new StringWriter();
            JsonTextWriter json = new JsonTextWriter(sw);

            // Begin writing the file
            json.WriteStartObject();

            json.WriteEntry("type", type);
            json.WriteEntry("name", name);


            json.WriteEndObject();
            

            File.WriteAllText(outputFile, sw.ToString());
        }

        private void SerializeChildren(xx2containerbase chidlren, JObject json)
        {

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

    }
}
