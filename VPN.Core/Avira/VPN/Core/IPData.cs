using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class IPData
    {
        [JsonProperty(PropertyName = "ip")] public string IP { get; set; }

        [JsonProperty(PropertyName = "geoloc_country")]
        public string Country { get; set; }
    }
}