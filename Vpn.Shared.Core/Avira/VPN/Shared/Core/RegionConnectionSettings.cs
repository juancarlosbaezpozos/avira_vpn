using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Avira.VPN.Shared.Core
{
    public class RegionConnectionSettings
    {
        [JsonProperty(PropertyName = "host")] public string Host { get; set; }

        [JsonProperty(PropertyName = "id")] public string Id { get; set; }

        [JsonProperty(PropertyName = "name")] public string Name { get; set; }

        [JsonProperty(PropertyName = "port")] public int Port { get; set; }

        [JsonProperty(PropertyName = "protocol")]
        public string Protocol { get; set; }

        [JsonProperty(PropertyName = "latency")]
        public string LatencyDisplay { get; set; }

        [JsonProperty(PropertyName = "license_type")]
        public string LicenseType { get; set; }

        public TimeSpan Latency
        {
            get
            {
                var values = (LatencyDisplay ?? string.Empty).ToCharArray().TakeWhile(char.IsDigit);
                int.TryParse(string.Join(string.Empty, values), out var result);
                return TimeSpan.FromMilliseconds(result);
            }
        }
    }
}