using System;
using System.Collections.Generic;
using System.Linq;
using Avira.VPN.Shared.Core;
using Newtonsoft.Json;
using Serilog;

namespace Avira.VPN.Core
{
    public class RegionList
    {
        [JsonProperty(PropertyName = "default")]
        public string DefaultRegion { get; internal set; }

        [JsonProperty(PropertyName = "lang")] public string Lang { get; internal set; }

        [JsonProperty(PropertyName = "regions")]
        public List<RegionConnectionSettings> Regions { get; set; }

        [JsonProperty(PropertyName = "ttl")] public double Ttl { get; private set; }

        [JsonProperty(PropertyName = "type")] public string Type { get; private set; }

        public RegionList()
        {
            Regions = new List<RegionConnectionSettings>();
        }

        public RegionConnectionSettings GetDefault()
        {
            try
            {
                return Regions.Where((RegionConnectionSettings server) => server.Id == DefaultRegion)
                    .FirstOrDefault() ?? Regions.First();
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to get default region.");
            }

            return null;
        }
    }
}