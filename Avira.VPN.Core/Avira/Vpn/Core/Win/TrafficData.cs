using Newtonsoft.Json;

namespace Avira.VPN.Core.Win
{
    public class TrafficData
    {
        [JsonProperty(PropertyName = "used")] public ulong UsedInBytes { get; set; }

        [JsonProperty(PropertyName = "limit")] public ulong LimitInBytes { get; set; }

        [JsonProperty(PropertyName = "grace_period")]
        public int GracePeriodInSeconds { get; set; }
    }
}