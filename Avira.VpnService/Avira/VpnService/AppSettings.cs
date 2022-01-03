using System;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Newtonsoft.Json.Linq;

namespace Avira.VpnService
{
    public class AppSettings : IAppSettings
    {
        public AppSettingsData Get()
        {
            IFeatures features = DiContainer.Resolve<IFeatures>();
            return new AppSettingsData
            {
                AppImprovement = ProductSettings.ProductImprovementUserSetting,
                KillSwitch = ProductSettings.KillSwitchUserSetting,
                UdpSupport = (features.IsActive("udp_support") && ProductSettings.UdpSupportUserSetting),
                MalwareProtection = (features.IsActive("malwareProtection", defaultValue: true) &&
                                     ProductSettings.MalwareProtectionUserSetting),
                AdBlocking = (features.IsActive("adBlocking") && ProductSettings.AdBlockingUserSetting),
                FastFeedback = (features.IsActive("fast_feedback") && ProductSettings.FastFeedbackStillShowUserSetting)
            };
        }

        public void Set(AppSettingsData value)
        {
            ProductSettings.ProductImprovementUserSetting = value.AppImprovement;
            if (!value.KillSwitch && ProductSettings.KillSwitchUserSetting)
            {
                NetworkBlocker.Disable();
            }

            ProductSettings.KillSwitchUserSetting = value.KillSwitch;
            if (value.UdpSupport != ProductSettings.UdpSupportUserSetting)
            {
                ProductSettings.UdpSupportUserSetting = value.UdpSupport;
            }

            if (value.MalwareProtection != ProductSettings.MalwareProtectionUserSetting)
            {
                ProductSettings.MalwareProtectionUserSetting = value.MalwareProtection;
            }

            if (value.AdBlocking != ProductSettings.AdBlockingUserSetting)
            {
                ProductSettings.AdBlockingUserSetting = value.AdBlocking;
            }

            if (value.FastFeedback != ProductSettings.FastFeedbackStillShowUserSetting)
            {
                ProductSettings.FastFeedbackStillShowUserSetting = value.FastFeedback;
            }
        }

        public AppSettingsData Update(JObject value)
        {
            throw new NotImplementedException();
        }
    }
}