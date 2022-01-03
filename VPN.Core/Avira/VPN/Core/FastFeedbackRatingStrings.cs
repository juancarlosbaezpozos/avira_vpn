using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class FastFeedbackRatingStrings
    {
        [JsonProperty(PropertyName = "one")] public string One { get; set; }

        [JsonProperty(PropertyName = "two")] public string Two { get; set; }

        [JsonProperty(PropertyName = "three")] public string Three { get; set; }

        [JsonProperty(PropertyName = "four")] public string Four { get; set; }

        [JsonProperty(PropertyName = "five")] public string Five { get; set; }
    }
}