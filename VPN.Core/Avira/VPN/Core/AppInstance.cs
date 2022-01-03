using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public class AppInstance : IOeResource
    {
        public JObject JsonObject { get; }

        public JToken Status => JsonObject.SelectToken("attributes.status");

        public string State
        {
            get { return (string?)JsonObject.SelectToken("attributes.state"); }
            set { JsonObject["attributes"]!["state"] = (JToken)value; }
        }

        public JToken About => JsonObject.SelectToken("attributes.status.about");

        public long Id => (long)JsonObject["id"];

        public AppInstance(JObject jsonObject)
        {
            JsonObject = jsonObject;
        }

        public AppInstance(long id)
        {
            JObject value = new JObject();
            JObject jObject = new JObject();
            JObject jObject2 = new JObject();
            JsonObject = new JObject();
            jObject2["status"] = jObject;
            jObject["about"] = value;
            JsonObject["attributes"] = jObject2;
            JsonObject["id"] = (JToken)id;
            JsonObject["attributes"]!["state"] = (JToken)"active";
        }
    }
}