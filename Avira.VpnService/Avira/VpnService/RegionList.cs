using System;
using System.Collections.Generic;
using System.Linq;
using Avira.VPN.Core.Win;
using Newtonsoft.Json;
using Serilog;

namespace Avira.VpnService
{
    public class RegionList
    {
        [JsonProperty(PropertyName = "default")]
        public string DefaultRegion { get; internal set; }

        [JsonProperty(PropertyName = "timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty(PropertyName = "ttl")] public double Ttl { get; private set; }

        [JsonProperty(PropertyName = "regions")]
        public List<RemoteConnectionSettings> ServersConnectionSettings { get; set; }

        [JsonProperty(PropertyName = "errorMessage")]
        public string ErrorMessage { get; set; }

        public RemoteConnectionSettings GetDefault()
        {
            try
            {
                return ServersConnectionSettings.Where((RemoteConnectionSettings server) => server.Id == DefaultRegion)
                    .FirstOrDefault() ?? ServersConnectionSettings.First();
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to obtain default region.");
            }

            return null;
        }
    }
}