using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public class Features : IFeatures
    {
        private readonly IRemoteConfiguration remoteConfiguration;

        public Features(IRemoteConfiguration remoteConfiguration)
        {
            this.remoteConfiguration = remoteConfiguration;
        }

        public bool IsActive(string featureId)
        {
            return IsActive(featureId, GetAppDefaultsValue(featureId, defaultValue: false));
        }

        public bool IsSwitchedOn(string featureId)
        {
            bool appDefaultsValue = GetAppDefaultsValue(featureId + "_switched_on", defaultValue: false);
            string value = CustomDefaultValueField(featureId);
            if (string.IsNullOrEmpty(value))
            {
                return appDefaultsValue;
            }

            if (!bool.TryParse(value, out var result))
            {
                return appDefaultsValue;
            }

            return result;
        }

        public string CustomDefaultValueField(string featureId)
        {
            return GetRemoteFeature(featureId)?.DefaultValue;
        }

        public FeatureData GetFeatureData(string id)
        {
            return new FeatureData(GetRemoteFeature(id));
        }

        public JObject Serialize()
        {
            return new JObject
            {
                {
                    "killSwitch",
                    (JToken)GetAppDefaultsValue("kill_switch", defaultValue: true)
                },
                {
                    "disableTracking",
                    (JToken)GetAppDefaultsValue("disable_tracking", defaultValue: true)
                },
                {
                    "autoStart",
                    (JToken)GetAppDefaultsValue("auto_start", defaultValue: true)
                },
                {
                    "disableLaunchAtStartup",
                    (JToken)GetAppDefaultsValue("disable_launch_at_startup", defaultValue: false)
                },
                {
                    "autoConnectUnsecureWifi",
                    (JToken)GetAppDefaultsValue("auto_connect_unsecure_wifi", defaultValue: false)
                },
                {
                    "wifiManagement",
                    (JToken)GetAppDefaultsValue("wifi_management", defaultValue: true)
                },
                {
                    "udpSupport",
                    (JToken)IsActive("udp_support",
                        GetAppDefaultsValue("IsBeta", defaultValue: false) &&
                        GetAppDefaultsValue("udp_support", defaultValue: true))
                },
                {
                    "enableCancelConnecting",
                    (JToken)GetAppDefaultsValue("cancel_connecting_button", defaultValue: true)
                },
                {
                    "trial",
                    (JToken)IsActive("trial")
                },
                {
                    "malwareProtection",
                    SerializeFeatureData("malwareProtection",
                        GetAppDefaultsValue("malware_protection", defaultValue: true))
                },
                {
                    "adBlocking",
                    SerializeFeatureData("adBlocking")
                },
                {
                    "trial_lp",
                    (JToken)IsActive("trial_lp")
                },
                {
                    "fastFeedback",
                    SerializeFeatureData("fast_feedback")
                },
                {
                    "restrictedProRegions",
                    SerializeFeatureData("restricted_pro_regions",
                        GetAppDefaultsValue("restricted_pro_regions", defaultValue: true))
                },
                {
                    "waitingWindow",
                    SerializeFeatureData("waiting_window")
                },
                {
                    "dataUsagePopup",
                    SerializeFeatureData("data_usage_popup")
                },
                {
                    "diagnosticTool",
                    SerializeFeatureData("diagnostic_tool")
                },
                {
                    "ipAddress",
                    SerializeFeatureData("ipAddress")
                }
            };
        }

        public bool IsActive(string featureId, bool defaultValue = false)
        {
            return new FeatureData(GetRemoteFeature(featureId), defaultValue).Active;
        }

        private bool GetAppDefaultsValue(string key, bool defaultValue)
        {
            return bool.Parse(DiContainer.Resolve<ISettings>().Get(key, defaultValue.ToString()));
        }

        private RemoteFeatureData GetRemoteFeature(string id)
        {
            return remoteConfiguration.RemoteFeatures.Find((RemoteFeatureData d) => d.Id == id);
        }

        private JObject SerializeFeatureData(string id, bool activeDefaultValue = false)
        {
            return JObject.FromObject(new FeatureData(GetRemoteFeature(id), activeDefaultValue));
        }
    }
}