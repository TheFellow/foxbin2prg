using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xx2lib
{
    public static class JsonExtensions
    {
        public static void WriteEntry(this JsonTextWriter json, string property, string value)
        {
            json.WritePropertyName(property);
            json.WriteValue(value);
        }

        public static void WriteEntry(this JsonTextWriter json, string property, int value)
        {
            json.WritePropertyName(property);
            json.WriteValue(value);
        }

        public static void WriteEntry(this JsonTextWriter json, string property, bool value)
        {
            json.WritePropertyName(property);
            json.WriteValue(value);
        }

        public static void WriteRange(this JsonTextWriter json, string name, int low, int high)
        {
            json.WritePropertyName(name);
            json.WriteStartArray();

            json.WriteValue(low);
            json.WriteValue(high);

            json.WriteEndArray();
        }

        public static void WriteLocationSpan(this JsonTextWriter json, LocationSpan locationSpan)
        {
            json.WritePropertyName(nameof(locationSpan));
            json.WriteStartObject();

            json.WriteRange("start", locationSpan.startRow, locationSpan.startCol);
            json.WriteRange("end", locationSpan.endRow, locationSpan.endCol);

            json.WriteEndObject();
        }
    }
}
