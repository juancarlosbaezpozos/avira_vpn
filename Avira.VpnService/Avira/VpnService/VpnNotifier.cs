using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avira.Common.Core;
using Avira.Common.Core.Networking;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Avira.VPN.Notifier;
using Avira.VPN.NotifierClient;
using Avira.VpnService.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VpnService
{
    public sealed class VpnNotifier : IDisposable, IVpnNotifier
    {
        private class VpnNotificationActionHandler
        {
            private readonly IVpnService vpnService;

            private readonly IVpnProvider vpnProvider;

            private readonly ISettings settings;

            private readonly IWifiNetworkManager unsecureWifiMonitor;

            private static string LearnMoreLandingPageUrl => string.Format(
                DiContainer.Resolve<JsonStorage>().Get("LearnMoreLandingPageUrl"), ProductSettings.ProductLanguage);

            private static string GeneralFeedbackUrl => DiContainer.Resolve<JsonStorage>().Get("GeneralFeedbackUrl") +
                                                        RetrieveSurveyUrlParameters();

            private static string ProductFeedbackUrl =>
                ProductSettings.LastProductFeedbackUrl + RetrieveSurveyUrlParameters();

            public VpnNotificationActionHandler(IVpnService vpnCommandProvider, IWifiNetworkManager unsecureWifiMonitor,
                IVpnProvider vpnProvider, ISettings settings)
            {
                vpnService = vpnCommandProvider;
                this.vpnProvider = vpnProvider;
                this.settings = settings;
                this.unsecureWifiMonitor = unsecureWifiMonitor;
            }

            public void ActionHandler(string notificationId, string actionId, string actionParam)
            {
                Serilog.Log.Information("Executing notification action. NotificationId: " + notificationId +
                                        ", ActionId: " + actionId + ", ActionParam: " + actionParam);
                JObject jObject = DecodeActionParams(actionParam);
                CheckDontShowAgainParam(notificationId, jObject);
                TrackNotificationButtonClicked(notificationId, actionId,
                    ConvertNotificationActionIdsToMixpanelIds(jObject));
                switch (actionId)
                {
                    case "NotNow":
                        break;
                    case "Cancel":
                        break;
                    case "OpenGui":
                        DesktopShell.ShellExecute(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ProductSettings.WebAppHostExe));
                        break;
                    case "ConnectVpn":
                        vpnProvider.ConnectToLastSelectedRegion(GetNotificationTriggerSource(notificationId));
                        vpnProvider.StartClientApp(GetNotificationTriggerSource(notificationId));
                        break;
                    case "ConnectVpnNoGui":
                        vpnProvider.ConnectToLastSelectedRegion(GetNotificationTriggerSource(notificationId));
                        break;
                    case "ActivateVpn":
                        unsecureWifiMonitor.UntrustConnectedWifiNetwork();
                        vpnProvider.ConnectToLastSelectedRegion(GetNotificationTriggerSource(notificationId));
                        vpnProvider.StartClientApp(GetNotificationTriggerSource(notificationId));
                        break;
                    case "ActivateVpnNoGui":
                        unsecureWifiMonitor.UntrustConnectedWifiNetwork();
                        vpnProvider.ConnectToLastSelectedRegion(GetNotificationTriggerSource(notificationId));
                        break;
                    case "TrustWifi":
                        unsecureWifiMonitor.TrustConnectedWifiNetwork();
                        break;
                    case "LearnMore":
                        DesktopShell.ShellExecute(LearnMoreLandingPageUrl, null, null);
                        break;
                    case "Upgrade":
                        vpnService.HandleUpgrade();
                        break;
                    case "ReconnectVpn":
                        vpnProvider.ConnectToLastSelectedRegion(GetNotificationTriggerSource(notificationId));
                        vpnProvider.StartClientApp(GetNotificationTriggerSource(notificationId));
                        break;
                    case "UnblockTraffic":
                        NetworkBlocker.Disable();
                        break;
                    case "SendGeneralFeedback":
                        DesktopShell.ShellExecute(GeneralFeedbackUrl, null, null);
                        break;
                    case "SendProductFeedback":
                        DesktopShell.ShellExecute(ProductFeedbackUrl, null, null);
                        break;
                    case "ActivateTrial":
                        DiContainer.Resolve<IUserManagementController>()?.ActivateTrial();
                        break;
                    case "OpenUrl":
                        DesktopShell.ShellExecute(actionParam);
                        break;
                }
            }

            private void CheckDontShowAgainParam(string notificationId, JObject decodedParams)
            {
                if (decodedParams != null && decodedParams.TryGetValue("dont_show_again", out var value) &&
                    value.ToObject<bool>())
                {
                    DisableNotification(notificationId);
                }
            }

            private Dictionary<string, string> ConvertNotificationActionIdsToMixpanelIds(JObject notificationActionIds)
            {
                Dictionary<string, string> dictionary = notificationActionIds?.ToObject<Dictionary<string, string>>();
                if (dictionary == null)
                {
                    return null;
                }

                Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> item in dictionary)
                {
                    string key = item.Key;
                    if (key == "dont_show_again")
                    {
                        dictionary2[Tracker.EventProperties.DontShowAgain] = item.Value;
                    }
                }

                if (dictionary2.Count == 0)
                {
                    return null;
                }

                return dictionary2;
            }

            private void DisableNotification(string id)
            {
                JObject jObject =
                    JsonConvert.DeserializeObject<JObject>(settings.Get("notifier_disabled_notifications", "{}"));
                jObject[id] = (JToken)true;
                settings.Set("notifier_disabled_notifications", JsonConvert.SerializeObject(jObject));
            }

            private string GetNotificationTriggerSource(string notificationId)
            {
                return "Notification " + notificationId;
            }

            private JObject DecodeActionParams(string actionParams)
            {
                if (!actionParams.IsValidJson())
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<JObject>(actionParams);
            }
        }

        private const long GeneralFeedbackTrafficLimit = 31457280L;

        private const long ProductFeedbackTrafficLimit = 314572800L;

        private readonly INotifierClient notifierClient;

        private readonly ISettings settings;

        private readonly IProductSettings productSettings;

        private VpnNotificationActionHandler actionHandler;

        private IReadOnlyList<string> AcceptedSpotlightNotifications =
            new List<string> { "UnsecureWifi", "TrafficLimitReached" };

        internal static int FeedbackNotificationDelayMs { get; set; } = 30000;


        private Dictionary<string, string> Commands => new Dictionary<string, string>
        {
            {
                "ConnectVpn",
                ResourcesVpnService.SecureMyConnection
            },
            {
                "ActivateVpn",
                ResourcesVpnService.Activate
            },
            {
                "TrustWifi",
                ResourcesVpnService.TrustWifiNetwork
            },
            {
                "OpenGui",
                ResourcesVpnService.OpenVpnAction
            },
            {
                "LearnMore",
                ResourcesVpnService.LearnMore
            },
            {
                "Upgrade",
                ResourcesVpnService.Buy
            },
            {
                "ReconnectVpn",
                ResourcesVpnService.Reconnect
            },
            {
                "UnblockTraffic",
                ResourcesVpnService.UnblockTraffic
            },
            {
                "NotNow",
                ResourcesVpnService.NotNow
            },
            {
                "SendGeneralFeedback",
                ResourcesVpnService.GeneralFeedbackAction
            },
            {
                "SendProductFeedback",
                ResourcesVpnService.ProductFeedbackAction
            },
            { "Cancel", null },
            { "ActivateTrial", null }
        };

        public VpnNotifier(IVpnService vpnCommandProvider, IWifiNetworkManager unsecureWifiMonitor,
            IVpnProvider vpnProvider, INotifierClient notifierClient)
            : this(vpnCommandProvider, unsecureWifiMonitor, vpnProvider, notifierClient,
                DiContainer.Resolve<ISettings>(), DiContainer.Resolve<IProductSettings>())
        {
        }

        public VpnNotifier(IVpnService vpnCommandProvider, IWifiNetworkManager unsecureWifiMonitor,
            IVpnProvider vpnProvider, INotifierClient notifierClient, ISettings settings,
            IProductSettings productSettings)
        {
            this.notifierClient = notifierClient;
            this.settings = settings;
            this.productSettings = productSettings;
            actionHandler =
                new VpnNotificationActionHandler(vpnCommandProvider, unsecureWifiMonitor, vpnProvider, settings);
            this.notifierClient.CustomActionHandler = actionHandler.ActionHandler;
            ProductSettings.ProductVersionChanged += ProductVersionChanged;
        }

        public Notification.Command GetCommand(string id, string text = null)
        {
            string text2 = (string.IsNullOrEmpty(text) ? Commands[id] : text);
            return new Notification.Command(id, text2);
        }

        private static void TrackNotificationButtonClicked(string notificationId, string actionId,
            Dictionary<string, string> actionParams = null)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>
            {
                {
                    Tracker.EventProperties.NotificationIdProperty,
                    notificationId
                },
                {
                    Tracker.EventProperties.ActionId,
                    actionId
                }
            };
            actionParams?.ToList().ForEach(delegate(KeyValuePair<string, string> p) { properties[p.Key] = p.Value; });
            Tracker.TrackEvent(Tracker.Events.NotificationButtonClicked, properties);
        }

        private static void TrackNotificationEvent(string eventName, string propertyName, string notificationId)
        {
            if (!string.IsNullOrEmpty(notificationId) && !string.IsNullOrEmpty(eventName))
            {
                Tracker.TrackEvent(eventName, new Dictionary<string, string> { { propertyName, notificationId } });
            }
        }

        public void NotifyUpdate()
        {
            Task.Run(async delegate
            {
                Notification notification = await CreateCustomNotificationIfAvailable("Update");
                if (notification == null)
                {
                    notification = new Notification
                    {
                        Id = "Update",
                        Title = ResourcesVpnService.Congratulations,
                        Message = ResourcesVpnService.UpdateMessage,
                        Hint = ResourcesVpnService.UpdateHint,
                        TemplateName = "TemplateVpnMessagWithCaption",
                        Action1 = GetCommand("OpenGui")
                    };
                }

                notification.Priority = Notification.PriorityLevel.Low;
                Notify(notification);
            });
        }

        public void NotifyUpgrade()
        {
            Task.Run(async delegate
            {
                Notification notification = await CreateCustomNotificationIfAvailable("Upgrade");
                if (notification == null)
                {
                    notification = new Notification
                    {
                        Id = "Upgrade",
                        Title = ResourcesVpnService.Congratulations,
                        Message = ResourcesVpnService.UpgradeMessage,
                        Hint = ResourcesVpnService.UpgradeHint,
                        TemplateName = "TemplateVpnMessagWithCaption",
                        Action1 = GetCommand("OpenGui")
                    };
                }

                notification.Priority = Notification.PriorityLevel.Low;
                Notify(notification);
            });
        }

        public void NotifyFeedback(ulong usedInBytes)
        {
            if (ShouldShowGeneralFeedbackNotification(usedInBytes))
            {
                Task.Run(async delegate
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(FeedbackNotificationDelayMs));
                    if (DateTime.UtcNow - ProductSettings.LastFeedbackNotificationDate >
                        ProductSettings.FeedbackNotificationMinPeriod)
                    {
                        NotifyGeneralFeedback();
                    }
                });
            }
            else
            {
                if (!ShouldShowProductFeedbackNotification(usedInBytes))
                {
                    return;
                }

                Task.Run(async delegate
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(FeedbackNotificationDelayMs));
                    if (DateTime.UtcNow - ProductSettings.LastFeedbackNotificationDate >
                        ProductSettings.FeedbackNotificationMinPeriod)
                    {
                        NotifyProductFeedback();
                    }
                });
            }
        }

        private bool SendFeedbackNotification(string feedbackId, string feedbackMessage, string feedbackHint,
            Notification.Command feedbackAction)
        {
            return Task.Run(async delegate
            {
                Notification notification = await CreateCustomNotificationIfAvailable(feedbackId);
                if (notification == null)
                {
                    notification = new Notification
                    {
                        Id = feedbackId,
                        Icon = Notification.IconType.Feedback,
                        Message = feedbackMessage + "<br/>" + feedbackHint,
                        Hint = feedbackMessage,
                        TemplateName = "TemplateVPnMessage",
                        Action1 = feedbackAction,
                        Action2 = GetCommand("NotNow")
                    };
                }

                notification.Priority = Notification.PriorityLevel.Low;
                if (!Notify(notification))
                {
                    return false;
                }

                ProductSettings.LastFeedbackNotificationDate = DateTime.Now;
                return true;
            }).Result;
        }

        public void NotifyGeneralFeedback()
        {
            if (SendFeedbackNotification("GiveGeneralFeedback", ResourcesVpnService.GeneralFeedbackMessage,
                    ResourcesVpnService.GeneralFeedbackHint, GetCommand("SendGeneralFeedback")))
            {
                ProductSettings.GeneralFeedbackNotificationCount++;
            }
        }

        public void NotifyProductFeedback()
        {
            if (SendFeedbackNotification("GiveProductFeedback", ResourcesVpnService.ProductFeedbackMessage,
                    ResourcesVpnService.ProductFeedbackHint, GetCommand("SendProductFeedback")))
            {
                ProductSettings.ProductFeedbackNotificationCount++;
            }
        }

        public void NotifyConnectedToUnsecureWifi()
        {
            Task.Run(async delegate
            {
                Notification notification = await CreateCustomNotificationIfAvailable("UnsecureWifi");
                if (notification == null)
                {
                    notification = new Notification
                    {
                        Id = "UnsecureWifi",
                        Position = Notification.PositionType.CenterScreen,
                        Message = ResourcesVpnService.ConnectedToUnknownWifiNetworkStatus,
                        Hint = ResourcesVpnService.ConnectedToUnsecureWifiNetworkAdvice,
                        TemplateName = "TemplateVpn2",
                        Action1 = GetCommand("ActivateVpn"),
                        Action2 = GetCommand("TrustWifi"),
                        Close2 = ResourcesVpnService.RemindMeLater
                    };
                }

                string text2 = (notification.Title2 =
                    DiContainer.Resolve<IWifiNetworkMonitor>()?.GetConnectedWifi().Ssid ?? " ");
                notification.Position = Notification.PositionType.CenterScreen;
                Notify(notification);
            });
        }

        public void NotifyInactivity()
        {
            Task.Run(async delegate
            {
                Notification notification = await CreateCustomNotificationIfAvailable("Inactivity");
                if (notification == null)
                {
                    notification = new Notification
                    {
                        Id = "Inactivity",
                        Message = ResourcesVpnService.UserInactivityMessage,
                        TemplateName = "TemplateVPnMessage",
                        Action1 = GetCommand("ConnectVpn"),
                        Action2 = GetCommand("LearnMore")
                    };
                }

                notification.Priority = Notification.PriorityLevel.Low;
                Notify(notification);
            });
        }

        public void NotifyLearnMore(EducationMessage message)
        {
            Notification notification = new Notification
            {
                Id = "Inactivity",
                TemplateName = "TemplateEducationMessage",
                Title = message.Title,
                Title2 = message.Title2,
                Message = message.Message,
                Question = message.Question,
                Hint = message.Hint,
                Image = message.Image,
                Position = Notification.PositionType.CenterScreen,
                Timeout = 0,
                Action1 = CreateInactivityCommand(null, message.Url)
            };
            notification.Priority = Notification.PriorityLevel.Low;
            Notify(notification);
        }

        public void NotifyFtu(bool isRegistered)
        {
            List<Notification.FtuPage> ftu = new List<Notification.FtuPage>
            {
                new Notification.FtuPage
                {
                    Header = ResourcesVpnService.FtuPagePrivacyHeader,
                    Text = ResourcesVpnService.FtuPagePrivacyText,
                    Image = "images/FTU-gfx-screen-1.png",
                    Checkbox = ResourcesVpnService.FtuPagePrivacyCheckbox,
                    Button = ResourcesVpnService.FtuPageNext
                },
                new Notification.FtuPage
                {
                    Header = ResourcesVpnService.FtuPageTravelHeader,
                    Text = ResourcesVpnService.FtuPageTravelText,
                    Image = "images/FTU-gfx-screen-2.png",
                    Checkbox = ResourcesVpnService.FtuPageTravelCheckbox,
                    Button = ResourcesVpnService.FtuPageNext
                },
                new Notification.FtuPage
                {
                    Header = ResourcesVpnService.FtuPageWifiHeader,
                    Text = ResourcesVpnService.FtuPageWifiText,
                    Image = "images/FTU-gfx-screen-3.png",
                    Checkbox = ResourcesVpnService.FtuPageWifiCheckbox,
                    Button = ResourcesVpnService.FtuPageWifiButton
                }
            };
            Notification notification = new Notification
            {
                Id = "Ftu",
                Priority = Notification.PriorityLevel.Low,
                TemplateName = "ftu",
                Timeout = 0,
                Position = Notification.PositionType.CenterScreen,
                Ftu = ftu,
                TrialDisabled = (isRegistered || !DiContainer.Resolve<IFeatures>().IsActive("trial")),
                Title = ResourcesVpnService.FtuTrialTitle,
                Message = ResourcesVpnService.FtuTrialMessage,
                Action1 = GetCommand("ActivateTrial", ResourcesVpnService.FtuTrialAction),
                Action2 = GetCommand("NotNow"),
                Image = "FTU-vpn-logo.png",
                IsMovable = true
            };
            Notify(notification);
        }

        private Notification.Command CreateInactivityCommand(string text, string url)
        {
            string text2 = (DiContainer.Resolve<IRemoteConfiguration>()?.RemoteFeatures)
                ?.Find((RemoteFeatureData f) => f.Id == "inactivity_notification")?.Params?["action_id"]?.ToString();
            string id = (string.IsNullOrEmpty(text2) ? "ConnectVpn" : text2);
            string text3 = (string.IsNullOrEmpty(text2) ? text : null);
            return GetCommand(id, text3);
        }

        public void NotifyKillSwitchActivated()
        {
            Task.Run(async delegate
            {
                Notification notification = await CreateCustomNotificationIfAvailable("KillSwitch");
                if (notification == null)
                {
                    notification = new Notification
                    {
                        Id = "KillSwitch",
                        Message = ResourcesVpnService.KillSwitchMessage,
                        TemplateName = "TemplateWarningNoHint",
                        Action1 = GetCommand("ReconnectVpn"),
                        Action2 = GetCommand("UnblockTraffic"),
                        Timeout = 0
                    };
                }

                notification.Priority = Notification.PriorityLevel.High;
                Notify(notification);
            });
        }

        public void NotifyWelcome()
        {
            Notification notification = new Notification
            {
                Id = "Welcome",
                Priority = Notification.PriorityLevel.Low,
                Message = ResourcesVpnService.WelcomeMessage,
                TemplateName = "TemplateVPnMessage",
                Action1 = GetCommand("ConnectVpn"),
                Action2 = GetCommand("LearnMore")
            };
            Notify(notification);
        }

        public void NotifyConnectedToUnkownWifi()
        {
            Task.Run(async delegate
            {
                Notification notification = await CreateCustomNotificationIfAvailable("UnkownWifi");
                if (notification == null)
                {
                    notification = new Notification
                    {
                        Id = "UnkownWifi",
                        Position = Notification.PositionType.CenterScreen,
                        Message = ResourcesVpnService.ConnectedToUnknownWifiNetworkStatus,
                        Hint = ResourcesVpnService.ConnectedToUnsecureWifiNetworkAdvice,
                        TemplateName = "TemplateVpn2",
                        Action1 = GetCommand("ActivateVpn"),
                        Action2 = GetCommand("TrustWifi"),
                        Close2 = ResourcesVpnService.RemindMeLater
                    };
                }

                string title = DiContainer.Resolve<IWifiNetworkMonitor>()?.GetConnectedWifi().Ssid ?? " ";
                notification.Position = Notification.PositionType.CenterScreen;
                notification.Title2 = title;
                Notify(notification);
            });
        }

        private async Task<Notification> CreateCustomNotificationIfAvailable(string notificationId)
        {
            string text = await (DiContainer.Resolve<CustomNotifications>()?.GetCustomNotificationPath(notificationId,
                DiContainer.Resolve<IRemoteConfiguration>()?.RemoteFeatures));
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            return new Notification
            {
                Id = notificationId,
                Template = Notification.TemplateType.CustomTemplate,
                TemplateName = text
            };
        }

        public void NotifyTrafficLimitReached(string message)
        {
            Task.Run(async delegate
            {
                Notification notification = await CreateCustomNotificationIfAvailable("TrafficLimitReached");
                if (notification == null)
                {
                    notification = new Notification
                    {
                        Id = "TrafficLimitReached",
                        Message = message,
                        TemplateName = "TemplateVPnMessage",
                        Action1 = GetCommand("Upgrade"),
                        Action2 = GetCommand("LearnMore"),
                        OnlyIfNoForegroundUiWindow = true
                    };
                }

                notification.Priority = Notification.PriorityLevel.High;
                Notify(notification);
            });
        }

        public void NotifyTrafficThreshHoldsReached(Tuple<string, string> message)
        {
            return;

            Task.Run(async delegate
            {
                Notification notification = await CreateCustomNotificationIfAvailable(message.Item2);
                if (notification == null)
                {
                    notification = new Notification
                    {
                        Id = message.Item2,
                        Message = message.Item1,
                        TemplateName = "TemplateVPnMessage",
                        Action1 = GetCommand("Upgrade"),
                        Action2 = GetCommand("NotNow"),
                        OnlyIfNoForegroundUiWindow = true
                    };
                }

                Notify(notification);
            });
        }

        public bool IsNotificationDisabled(string id)
        {
            if (JsonConvert.DeserializeObject<JObject>(settings.Get("notifier_disabled_notifications", "{}"))!
                .TryGetValue(id, out var value))
            {
                return (value.ToObject<bool>() ? ((byte)1) : ((byte)0)) != 0;
            }

            return false;
        }

        public bool IsNotificationDisabledByDefault(string id)
        {
            if (JsonConvert.DeserializeObject<JObject>(settings.Get("disabled_notifications", "{}"))!.TryGetValue(id,
                    out var value))
            {
                return (value.ToObject<bool>() ? ((byte)1) : ((byte)0)) != 0;
            }

            return false;
        }

        private bool ShouldNotify(Notification notification)
        {
            if (IsNotificationDisabledByDefault(notification.Id))
            {
                Serilog.Log.Debug("Notification " + notification.Id + " is disabled by default.");
                return false;
            }

            if (IsNotificationDisabled(notification.Id))
            {
                Serilog.Log.Debug("Notification " + notification.Id + " was disabled by the user.");
                return false;
            }

            return true;
        }

        internal bool Notify(Notification notification)
        {
            if (!ShouldNotify(notification))
            {
                return false;
            }

            if (productSettings.IsSpotlightActive() || productSettings.IsSpotlightVpnIntegrated)
            {
                Serilog.Log.Information("Spotlight notification triggered: " + notification.Id);
                TrackNotificationEvent(Tracker.Events.SpotlightNotificationTriggered,
                    Tracker.EventProperties.NotificationIdProperty, notification.Id);
            }
            else
            {
                Serilog.Log.Information("Showing notification: " + notification.Id);
                TrackNotificationEvent(Tracker.Events.NotificationShown, Tracker.EventProperties.NotificationIdProperty,
                    notification.Id);
            }

            notifierClient.Notify(notification);
            return true;
        }

        private void Trigger(string notificationId, Action action)
        {
            Serilog.Log.Information("Executing notification action: " + notificationId);
            TrackNotificationEvent(Tracker.Events.NotificationButtonClicked, Tracker.EventProperties.ActionId,
                notificationId);
            action?.Invoke();
        }

        public void Dispose()
        {
            notifierClient.Dispose();
        }

        private bool ShouldShowGeneralFeedbackNotification(ulong usedInBytes)
        {
            if (ProductSettings.GeneralFeedbackNotificationCount == 0 && usedInBytes > 31457280)
            {
                return DateTime.UtcNow - ProductSettings.InstallDate > ProductSettings.GeneralFeedbackDelay;
            }

            return false;
        }

        private void UpdateProductFeedbackSettings(bool status, string url)
        {
            ProductSettings.LastProductFeedbackStatus = status;
            ProductSettings.LastProductFeedbackUrl = url;
        }

        private void ResetProductFeedbackConditions()
        {
            ProductSettings.ProductFeedbackNotificationCount = 0;
        }

        private void CheckProductFeedbackStatusChanged()
        {
            IFeatures features = DiContainer.Resolve<IFeatures>();
            bool flag = features.IsActive("productFeedback");
            string text = features.CustomDefaultValueField("productFeedback");
            if (flag != ProductSettings.LastProductFeedbackStatus || (!string.IsNullOrEmpty(text) &&
                                                                      string.Compare(text,
                                                                          ProductSettings.LastProductFeedbackUrl,
                                                                          ignoreCase: true) != 0))
            {
                UpdateProductFeedbackSettings(flag, text);
                if (flag)
                {
                    ResetProductFeedbackConditions();
                }
            }
        }

        private bool ShouldShowProductFeedbackNotification(ulong usedInBytes)
        {
            CheckProductFeedbackStatusChanged();
            if (!DiContainer.Resolve<IFeatures>().IsActive("productFeedback"))
            {
                return false;
            }

            if (ProductSettings.ProductFeedbackNotificationCount == 0)
            {
                return usedInBytes > 314572800;
            }

            return false;
        }

        private void ProductVersionChanged(object sender, ProductSettings.ProductVersionChangedEventArgs e)
        {
            if (e.PreviousVersion < new Version(1, 9, 0, 0))
            {
                Task.Run(delegate
                {
                    Task.Delay(1000);
                    NotifyUpdate();
                });
            }
        }

        private static string RetrieveSurveyUrlParameters()
        {
            return "?DeviceID=" + GeneratedDeviceInfo.GetDeviceId() + $"&ProductID={ProductSettings.ProductId}" +
                   "&ProductVersion=" + ProductSettings.ProductVersion + "&ProductLanguage=" +
                   ProductSettings.ProductLanguage + "&PlatformType=Windows" +
                   $"&PlatformVersion={WindowsInfo.VersionNumber()}" + "&BundleID=" +
                   Settings.Default.InstallationBundleId + "&LicenseType=" +
                   DiContainer.GetValue<string>("LicenseType");
        }
    }
}