using System.Runtime.Serialization;

namespace Avira.Acp.Common
{
    [DataContract]
    public class JsonObjectWrapper
    {
        [DataMember] public string Data { get; set; }

        public JsonObjectWrapper()
        {
        }

        public JsonObjectWrapper(string data)
        {
            Data = data;
        }

        public static JsonObjectWrapper DeserializeFromJson(string json)
        {
            return new JsonObjectWrapper(json);
        }

        public static string SerializeToJson(JsonObjectWrapper jsonObjectWrapper)
        {
            return jsonObjectWrapper.ToString();
        }

        public override string ToString()
        {
            return Data;
        }
    }
}