using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class TrafficData
    {
        [JsonProperty(PropertyName = "used")] public ulong UsedTraffic { get; set; }

        [JsonProperty(PropertyName = "limit")] public ulong TrafficLimit { get; set; }
    }
}