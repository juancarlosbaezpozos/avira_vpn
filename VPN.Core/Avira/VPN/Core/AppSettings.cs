using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public class AppSettings : IAppSettings
    {
        private JObject appSettings;

        public AppSettingsData Get()
        {
            appSettings = JObject.Parse(GetAppSettings());
            return new AppSettingsData
            {
                AppImprovement = GetSetting("appImprovement", "true"),
                KillSwitch = GetSetting("killSwitch", "false"),
                UdpSupport = GetSetting("udpSupport", "false"),
                MalwareProtection = GetSetting("malwareProtection", "false"),
                AdBlocking = GetSetting("adBlocking", "false"),
                FastFeedback = GetSetting("showFastFeedback", "true")
            };
        }

        public void Set(AppSettingsData value)
        {
            DiContainer.Resolve<ISettings>().Set("appSettings", JsonConvert.SerializeObject(value));
        }

        public AppSettingsData Update(JObject value)
        {
            AppSettingsData appSettingsData = Get();
            if (value.Property("appImprovement") != null)
            {
                appSettingsData.AppImprovement = (bool)(JToken)value.Property("appImprovement");
            }

            if (value.Property("killSwitch") != null)
            {
                appSettingsData.KillSwitch = (bool)(JToken)value.Property("killSwitch");
            }

            if (value.Property("udpSupport") != null)
            {
                appSettingsData.UdpSupport = (bool)(JToken)value.Property("udpSupport");
            }

            if (value.Property("malwareProtection") != null)
            {
                appSettingsData.MalwareProtection = (bool)(JToken)value.Property("malwareProtection");
            }

            if (value.Property("adBlocking") != null)
            {
                appSettingsData.AdBlocking = (bool)(JToken)value.Property("adBlocking");
            }

            if (value.Property("showFastFeedback") != null)
            {
                appSettingsData.FastFeedback = (bool)(JToken)value.Property("showFastFeedback");
            }

            Set(appSettingsData);
            return appSettingsData;
        }

        public string GetAppSettings()
        {
            string text = DiContainer.Resolve<ISettings>()?.Get("app_improvement", "true");
            return DiContainer.Resolve<ISettings>()?.Get("appSettings",
                "{\"appImprovement\":" + text.ToLower() + ", \"showFastFeedback\": \"true\"}");
        }

        private bool GetSetting(string key, string defaultValue)
        {
            bool result = ((DiContainer.Resolve<ISettings>().Get(key, defaultValue) == "true") ? true : false);
            if (appSettings.Property(key) != null)
            {
                return (bool)(JToken)appSettings.Property(key);
            }

            return result;
        }

        public bool IsFreshInstallation(ISettings settings)
        {
            return string.IsNullOrEmpty(settings.Get("device_id"));
        }

        public void CheckIfEulaShouldBeDisplayed(ISettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Get("eula_accepted")))
            {
                if (!IsFreshInstallation(settings))
                {
                    settings.Set("eula_accepted", "{\"accepted\":true}");
                }
                else
                {
                    settings.Set("eula_accepted", "{\"accepted\":false}");
                }
            }
        }
    }
}