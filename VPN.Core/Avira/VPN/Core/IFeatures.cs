using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public interface IFeatures
    {
        bool IsActive(string featureId);

        bool IsActive(string featureId, bool defaultValue);

        bool IsSwitchedOn(string featureId);

        string CustomDefaultValueField(string featureId);

        FeatureData GetFeatureData(string id);

        JObject Serialize();
    }
}