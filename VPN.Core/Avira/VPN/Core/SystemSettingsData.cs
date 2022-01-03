using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class SystemSettingsData
    {
        [JsonProperty(PropertyName = "theme")] public string Theme { get; set; }
    }
}