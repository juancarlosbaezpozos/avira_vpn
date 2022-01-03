using System;
//using Avira.Utilities.Pcl;
using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class LatencyData : EventArgs
    {
        [JsonProperty(PropertyName = "id")] public string Id { get; set; }

        [JsonProperty(PropertyName = "latency")]
        public long Latency { get; set; }

        [JsonProperty(PropertyName = "error")] public string Error { get; set; }

        //[JsonProperty(PropertyName = "ipstatus")]
        //public IPStatusWrapper IPStatus { get; set; }
    }
}