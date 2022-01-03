using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public class FeatureData
    {
        private const uint BetaFlag = 1u;

        private readonly RemoteFeatureData remoteFeature;

        private readonly bool activeDefaultValue;

        [JsonProperty(PropertyName = "enabled")]
        public bool Active
        {
            get
            {
                if (remoteFeature != null)
                {
                    return remoteFeature.IsActive;
                }

                return activeDefaultValue;
            }
        }

        [JsonProperty(PropertyName = "beta")] public bool Beta => ((ulong)ConvertFlag(remoteFeature?.Flag) & 1uL) == 1;

        [JsonProperty(PropertyName = "params")]
        public JObject Params => remoteFeature?.Params;

        public FeatureData(RemoteFeatureData remoteFeature, bool activeDefaultValue = false)
        {
            this.remoteFeature = remoteFeature;
            this.activeDefaultValue = activeDefaultValue;
        }

        private int ConvertFlag(string flag)
        {
            if (!int.TryParse(flag, out var result))
            {
                return 0;
            }

            return result;
        }
    }
}