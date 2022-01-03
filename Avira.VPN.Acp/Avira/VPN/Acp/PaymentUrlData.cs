using Newtonsoft.Json;

namespace Avira.VPN.Acp
{
    public class PaymentUrlData
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "url")] public string Url { get; set; }

        [JsonProperty(PropertyName = "operation")]
        public string Operation { get; set; }

        [JsonProperty(PropertyName = "scope")] public string Scope { get; set; }

        [JsonProperty(PropertyName = "target")]
        public string Target { get; set; }
    }
}