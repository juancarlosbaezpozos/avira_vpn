using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public class FastFeedbackData
    {
        [JsonProperty(PropertyName = "feedback_id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "content")]
        public JObject Content { get; set; }
    }
}