using System.Collections.Generic;
using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class RemoteConfigurationData
    {
        [JsonProperty(PropertyName = "buckets")]
        public List<string> Buckets { get; set; }

        [JsonProperty(PropertyName = "features")]
        public List<RemoteFeatureData> RemoteFeatures { get; set; }
    }
}