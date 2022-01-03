using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class AppSettingsData
    {
        [JsonProperty(PropertyName = "appImprovement")]
        public bool AppImprovement { get; set; }

        [JsonProperty(PropertyName = "killSwitch")]
        public bool KillSwitch { get; set; }

        [JsonProperty(PropertyName = "udpSupport")]
        public bool UdpSupport { get; set; }

        [JsonProperty(PropertyName = "malwareProtection")]
        public bool MalwareProtection { get; set; }

        [JsonProperty(PropertyName = "adBlocking")]
        public bool AdBlocking { get; set; }

        [JsonProperty(PropertyName = "showFastFeedback")]
        public bool FastFeedback { get; set; }
    }
}