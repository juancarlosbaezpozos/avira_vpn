using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public class FastFeedbackContentStrings
    {
        [JsonProperty(PropertyName = "title")] public string Title { get; set; }

        [JsonProperty(PropertyName = "subject")]
        public string Subject { get; set; }

        [JsonProperty(PropertyName = "button_submit")]
        public string ButtonSubmit { get; set; }

        [JsonProperty(PropertyName = "button_cancel")]
        public string ButtonCancel { get; set; }

        [JsonProperty(PropertyName = "ratings")]
        public JObject Ratings { get; set; }
    }
}