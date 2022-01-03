using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class ActionNotification
    {
        public enum ActionType
        {
            openUrl,
            openPurchaseView
        }

        [JsonProperty(PropertyName = "text")] public string Text { get; set; }

        [JsonProperty(PropertyName = "title")] public string Title { get; set; }

        [JsonProperty(PropertyName = "action")]
        public ActionType Action { get; set; }

        [JsonProperty(PropertyName = "parameter")]
        public string Parameter { get; set; }
    }
}