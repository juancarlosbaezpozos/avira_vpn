using System;
using System.Collections.Generic;
using Avira.VPN.Shared.Core;

namespace Avira.VPN.Core
{
    public static class Tracker
    {
        public static class Events
        {
            public static string Connect => "Connect";

            public static string Connected => "Connected";

            public static string Reconnect => "Reconnect";

            public static string Disconnect => "Disconnect";

            public static string ConnectionError => "Connection Error";

            public static string TapDriverError => "TAP Driver Error";

            public static string TrafficLimitReached => "Traffic Limit Reached";

            public static string UpgradeClicked => "Upgrade Clicked";

            public static string LicenseChanged => "License Changed";

            public static string OpenDashboard => "OpenDashboard Clicked";

            public static string RegistrationClicked => "Registration Clicked";

            public static string PurchaseCompleted => "Purchase Completed";

            public static string PurchaseRestored => "Purchase Restored";

            public static string PurchaseFailed => "Purchase Failed";

            public static string PurchaseCanceled => "Purchase Canceled";

            public static string GuiOpened => "Gui Opened";

            public static string UpdateFinished => "Update Finished";

            public static string UpdateStarted => "Update Started";

            public static string Install => "Install";

            public static string Uninstall => "Uninstall";

            public static string Ping => "Ping";

            public static string RateMeShown => "RateMe Shown";

            public static string RateMeClicked => "RateMe Clicked";

            public static string UserFeedback => "User Feedback";

            public static string UserFeedbackShown => "User Feedback Shown";

            public static string UserFeedbackDismissed => "User Feedback Dismissed";

            public static string RemoteFeaturesChanged => "Features Changed";

            public static string NotificationButtonClicked => "Notification Button Clicked";

            public static string Autoconnect => "Autoconnect";

            public static string OsNotificationClicked => "OS Notification Clicked";

            public static string OsNotificationShown => "OS Notification Shown";

            public static string NotificationShown => "Notification Shown";

            public static string NotificationIgnored => "Notification Ignored";

            public static string SpotlightNotificationTriggered => "Spotlight Notification Triggered";

            public static string MalwareProtectionChanged => "Malware Protection Changed";

            public static string AdBlockingChanged => "Ad Blocking Changed";

            public static string DataUsageShown => "Data Usage Popup Shown";

            public static string DataUsageClicked => "Data Usage Popup Clicked";

            public static string DataUsageDismissed => "Data Usage Popup Dismissed";

            public static string ExperimentStarted => "Experiment Started";

            public static string ExperimentStopped => "Experiment Stopped";

            public static string AARRR_FeatureUsed => "AARRR Feature Used";

            public static string AARRR_AppOpen => "AARRR App Open";

            public static string GuiOpenedTrigger => "Gui Opened Trigger";
        }

        public static class EventProperties
        {
            public static string NotificationIdProperty => "Notification Id";

            public static string ActionId => "Action Id";

            public static string DontShowAgain => "Don't show again";

            public static string Bucket => "Bucket";
        }

        public static void TrackConnect(RegionConnectionSettings region, bool isTriggeredByAutoconnect = false)
        {
            TrackEvent(Events.Connect, new Dictionary<string, string>
            {
                { "Region", region.Name },
                { "Uri", region.Host },
                {
                    "Port",
                    region.Port.ToString()
                },
                { "Protocol", region.Protocol },
                {
                    "Latency",
                    ((int)region.Latency.TotalMilliseconds).ToString()
                },
                {
                    "Autoconnect",
                    isTriggeredByAutoconnect.ToString()
                }
            });
        }

        public static void TrackPurchaseSucceeded(string productId)
        {
            TrackEvent(Events.PurchaseCompleted, new Dictionary<string, string> { { "Sku", productId } });
        }

        public static void TrackPurchaseCanceled(string productId)
        {
            TrackEvent(Events.PurchaseCanceled, new Dictionary<string, string> { { "Sku", productId } });
        }

        public static void TrackPurchaseFailed(string error)
        {
            TrackEvent(Events.PurchaseFailed, new Dictionary<string, string> { { "Error", error } });
        }

        public static void TrackPurchaseRestored(string productId)
        {
            TrackEvent(Events.PurchaseRestored, new Dictionary<string, string> { { "Sku", productId } });
        }

        public static void TrackConnectError(Exception error, RegionConnectionSettings region)
        {
            TrackEvent(Events.ConnectionError, new Dictionary<string, string>
            {
                { "Message", error.Message },
                {
                    "Port",
                    region.Port.ToString()
                },
                { "Protocol", region.Protocol }
            });
        }

        public static void TrackLimitReached(ulong usedBytes, ulong limitBytes)
        {
            TrackEvent(Events.TrafficLimitReached, new Dictionary<string, string>
            {
                {
                    "Used Traffic",
                    usedBytes.ToString()
                },
                {
                    "Traffic Limit",
                    limitBytes.ToString()
                }
            });
        }

        public static void Ping()
        {
            string value = DiContainer.Resolve<ISettings>()?.Get("first_seen");
            if (string.IsNullOrEmpty(value))
            {
                value = DateTime.UtcNow.ToString("u");
                DiContainer.Resolve<ISettings>()?.Set("first_seen", value);
            }

            IDevice device = DiContainer.Resolve<IDevice>();
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                {
                    "License Type",
                    "Unregistered"
                },
                {
                    "Version",
                    DiContainer.Resolve<IProductSettings>()?.ProductVersion
                },
                { "First Seen", value },
                { "Operating System", device.OperatingSystem },
                { "OS Architecture", device.OperatingSystemArchitecture },
                { "OS Version", device.OperatingSystemVersion },
                { "OS Language", device.OperatingSystemLanguage }
            };
            TrackEvent(Events.Ping, properties);
        }

        public static void TrackEvent(string eventName, Dictionary<string, string> properties = null)
        {
            //DiContainer.Resolve<IMixpanelTracker>()?.Track(eventName, properties).CatchAll();
        }
    }
}