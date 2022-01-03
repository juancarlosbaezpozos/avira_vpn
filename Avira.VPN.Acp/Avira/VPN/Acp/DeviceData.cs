using Newtonsoft.Json;

namespace Avira.VPN.Acp
{
    public class DeviceData
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "date_updated")]
        public string data_updated { get; set; }

        [JsonProperty(PropertyName = "hardware_id")]
        public string hardware_id { get; set; }

        [JsonProperty(PropertyName = "date_churned")]
        public string date_churned { get; set; }

        [JsonProperty(PropertyName = "os_version")]
        public string os_version { get; set; }

        [JsonProperty(PropertyName = "state")] public string state { get; set; }

        [JsonProperty(PropertyName = "hidden")]
        public string hidden { get; set; }

        [JsonProperty(PropertyName = "type")] public string type { get; set; }

        [JsonProperty(PropertyName = "brand")] public string brand { get; set; }

        [JsonProperty(PropertyName = "last_online")]
        public string last_online { get; set; }

        [JsonProperty(PropertyName = "agent_language")]
        public string agent_language { get; set; }

        [JsonProperty(PropertyName = "date_added")]
        public string date_added { get; set; }

        [JsonProperty(PropertyName = "os_type")]
        public string os_type { get; set; }

        [JsonProperty(PropertyName = "download_source")]
        public string download_source { get; set; }

        [JsonProperty(PropertyName = "locked")]
        public string locked { get; set; }

        [JsonProperty(PropertyName = "name")] public string name { get; set; }

        [JsonProperty(PropertyName = "country")]
        public string country { get; set; }

        [JsonProperty(PropertyName = "alias")] public string alias { get; set; }

        [JsonProperty(PropertyName = "tracking_id")]
        public string tracking_id { get; set; }

        [JsonProperty(PropertyName = "model")] public string model { get; set; }

        [JsonProperty(PropertyName = "os")] public string os { get; set; }

        [JsonProperty(PropertyName = "agent_version")]
        public string agent_version { get; set; }
    }
}