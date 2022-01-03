using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public class RemoteFeatureData
    {
        [JsonProperty(PropertyName = "id")] public string Id { get; set; }

        [JsonProperty(PropertyName = "active")]
        public bool IsActive { get; set; }

        [JsonProperty(PropertyName = "default_value")]
        public string DefaultValue { get; set; }

        [JsonProperty(PropertyName = "flag")] public string Flag { get; set; }

        [JsonProperty(PropertyName = "params")]
        public JObject Params { get; set; }
    }
}