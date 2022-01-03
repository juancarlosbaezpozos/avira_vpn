using Avira.VPN.Core.Win;
using Avira.VpnService.Properties;
using Serilog;

namespace Avira.VpnService
{
    public class EducationMessageRotator
    {
        private static readonly string VpnPrivacyLandingPageUrl =
            "https://www.avira.com/" + ProductSettings.ProductLanguage + "/vpn-privacy";

        private static readonly string VpnTravelerLandingPageUrl =
            "https://www.avira.com/" + ProductSettings.ProductLanguage + "/vpn-traveler";

        private static readonly string VpnWifiLandingPageUrl =
            "https://www.avira.com/" + ProductSettings.ProductLanguage + "/vpn-wifi";

        private readonly EducationMessage[] messages = new EducationMessage[3]
        {
            new EducationMessage
            {
                Title = ResourcesVpnService.EducationMessageOnlinePrivacyTitle,
                Title2 = ResourcesVpnService.EducationMessageOnlinePrivacyTitle2,
                Message = ResourcesVpnService.EducationMessageOnlinePrivacyMessage,
                Question = ResourcesVpnService.EducationMessageOnlinePrivacyQuestion,
                Hint = "",
                Image = "onlineprivacy.png",
                Url = VpnPrivacyLandingPageUrl,
                BaloonText = ResourcesVpnService.EducationMessageOnlinePrivacyBaloon
            },
            new EducationMessage
            {
                Title = ResourcesVpnService.EducationMessageTravelerTitle,
                Title2 = ResourcesVpnService.EducationMessageTravelerTitle2,
                Message = ResourcesVpnService.EducationMessageTravelerMessage,
                Question = ResourcesVpnService.EducationMessageTravelerQuestion,
                Hint = "",
                Image = "traveler.png",
                Url = VpnTravelerLandingPageUrl,
                BaloonText = ResourcesVpnService.EducationMessageTravelerBaloon
            },
            new EducationMessage
            {
                Title = ResourcesVpnService.EducationMessageWifiTitle,
                Title2 = ResourcesVpnService.EducationMessageWifiTitle2,
                Message = ResourcesVpnService.EducationMessageWifiMessage,
                Question = ResourcesVpnService.EducationMessageWifiQuestion,
                Hint = "",
                Image = "lock.png",
                Url = VpnWifiLandingPageUrl,
                BaloonText = ResourcesVpnService.EducationMessageWifiBaloon
            }
        };

        private int currentId;

        private IVpnNotifier notifier;

        private IServicePersistentData persistentData;

        public EducationMessageRotator(IVpnNotifier notifier)
        {
            this.notifier = notifier;
            persistentData = new ServicePersistentData();
            currentId = persistentData.CurrentEducationMessage;
        }

        public void Show()
        {
            Log.Debug($"Showing education notification with id {currentId}");
            notifier.NotifyLearnMore(messages[currentId]);
            currentId++;
            if (currentId >= messages.Length)
            {
                currentId = 0;
            }

            persistentData.CurrentEducationMessage = currentId;
        }

        public string GetBaloonText()
        {
            return messages[currentId].BaloonText;
        }
    }
}