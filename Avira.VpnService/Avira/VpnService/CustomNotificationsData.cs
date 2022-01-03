using Newtonsoft.Json;

namespace Avira.VpnService
{
    public class CustomNotificationsData
    {
        public class NotificationId
        {
            public const string Inactivity = "inactivity_notification";

            public const string GiveGeneralFeedback = "general_feedback_notification";

            public const string GiveProductFeedback = "product_feedback_notification";

            public const string UnkownWifi = "unknown_wifi_notification";

            public const string UnsecureWifi = "unsecure_wifi_notification";

            public const string TrafficLimitReached = "traffic_limit_notification";

            public const string Traffic50PercentReached = "traffic_50_percent_notification";

            public const string Traffic80PercentReached = "traffic_80_percent_notification";

            public const string Traffic90PercentReached = "traffic_90_percent_notification";

            public const string KillSwitch = "kill_switch_notification";

            public const string Update = "update_notification";

            public const string Upgrade = "upgrade_notification";
        }

        [JsonProperty(PropertyName = "id")] public string Id { get; set; }

        [JsonProperty(PropertyName = "bundle_url")]
        public string BundleUrl { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }
    }
}