using System;
using System.Net.NetworkInformation;
using Newtonsoft.Json;

namespace Avira.VpnService
{
    public class LatencyData : EventArgs
    {
        [JsonProperty(PropertyName = "id")] public string Id { get; set; }

        [JsonProperty(PropertyName = "latency")]
        public long Latency { get; set; }

        [JsonProperty(PropertyName = "error")] public string Error { get; set; }

        [JsonProperty(PropertyName = "ipstatus")]
        public IPStatus IPStatus { get; set; }
    }
}