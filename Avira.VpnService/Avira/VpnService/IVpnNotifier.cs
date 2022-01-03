using System;
using Avira.VPN.Notifier;

namespace Avira.VpnService
{
    public interface IVpnNotifier
    {
        Notification.Command GetCommand(string id, string text = null);

        void NotifyConnectedToUnkownWifi();

        void NotifyConnectedToUnsecureWifi();

        void NotifyFeedback(ulong usedInBytes);

        void NotifyInactivity();

        void NotifyKillSwitchActivated();

        void NotifyTrafficLimitReached(string message);

        void NotifyTrafficThreshHoldsReached(Tuple<string, string> message);

        void NotifyUpdate();

        void NotifyUpgrade();

        void NotifyWelcome();

        void NotifyLearnMore(EducationMessage message);

        void NotifyFtu(bool isRegistered);
    }
}