using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Avira.Common.Core;
using Avira.Common.Core.Networking;
using Avira.VPN.Core;
using Avira.Vpn.Core.Win;
using Avira.VPN.Core.Win;
using Avira.VPN.NotifierClient;
using Avira.VPN.Shared.Core;
using Avira.VPN.Shared.UWP;
using Avira.VpnService.Properties;
using Avira.Win.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VpnService
{
    public class VpnService : ServiceBase, IVpnService, IVpnProvider
    {
        public class DisconnectTimerEventArgs : EventArgs
        {
            [JsonProperty(PropertyName = "RemainingSeconds")]
            public int RemainingSeconds { get; set; }
        }

        private static readonly int NetworkBlockerDelay = 500;

        private UserActivityMonitor userActivityMonitor;

        private readonly ReflectionService reflectionService;

        private VpnNotifier vpnNotifier;

        private bool isLogoffDone = true;

        private OpenVpn openVpn;

        private Regions regions;

        private RemoteConfiguration remoteConfiguration;

        private Traffic traffic;

        private Features features;

        private AppSettings appSettings;

        private NodeSettings nodeSettings;

        private PipeCommunicatorServer communciatorServer;

        private Router router;

        private IOeStatusReporter statusReporter;

        private ILauncherGuiController launcherGuiController;

        private EducationMessageRotator educationMessageRotator;

        private Diagnostics diagnostic;

        private ConnectionState openVpnConnectionState = ConnectionState.Unknown;

        private WifiNetworkManager unsecureWifiNetworkMonitor;

        private IAuthenticator auth2;

        private VpnHttpClient vpnApiHttpClient;

        private RegionsLatency latencyProber = new RegionsLatency();

        private WifiManager wifiManager;

        private bool disconnectWasRequested;

        private bool traffic50PercentReachedNotification;

        private bool traffic80PercentReachedNotification;

        private bool traffic90PercentReachedNotification;

        private CustomNotifications customNotifications;

        private FastFeedback fastFeedback;

        private DataUsagePopup dataUsagePopup;

        private static Random rnd = new Random();

        private VpnConnectorBridge vpnConnectorBridge;

        private IPChecker ipChecker;

        private ExperimentTracker experimentTracker;

        private Stopwatch sessionDuration = new Stopwatch();

        private Task serviceStartTask;

        private IContainer components;

        [Routing("appSettings")]
        public AppSettingsData GuiSettings
        {
            get { return DiContainer.Resolve<IAppSettings>().Get(); }
            set
            {
                bool malwareProtectionUserSetting = ProductSettings.MalwareProtectionUserSetting;
                bool adBlockingUserSetting = ProductSettings.AdBlockingUserSetting;
                DiContainer.Resolve<IAppSettings>().Set(value);
                if ((false | CheckIfSettingChanged(ProductSettings.MalwareProtectionUserSetting,
                         malwareProtectionUserSetting, Tracker.Events.MalwareProtectionChanged,
                         value.MalwareProtection) |
                     CheckIfSettingChanged(ProductSettings.AdBlockingUserSetting, adBlockingUserSetting,
                         Tracker.Events.AdBlockingChanged, value.AdBlocking)) &&
                    ConnectionState.Connected == ConnectionState)
                {
                    Task.Run(async delegate { await nodeSettings.UpdateFeatures(); });
                }
            }
        }

        [Routing("features")] public JObject Features => DiContainer.Resolve<IFeatures>().Serialize();

        [Routing("isSandBoxed")] public string IsSandboxed => "False";

        [Routing("productInfo")]
        public JObject ProductInfo => new JObject
        {
            {
                "DeviceId",
                (JToken)GeneratedDeviceInfo.GetDeviceId()
            },
            {
                "ProductID",
                (JToken)ProductSettings.ProductId
            },
            {
                "ProductVersion",
                (JToken)ProductSettings.ProductVersion
            },
            {
                "ProductLanguage",
                (JToken)ProductSettings.ProductLanguage
            },
            {
                "PlatformType",
                (JToken)"Windows"
            },
            {
                "PlatformVersion",
                (JToken)WindowsInfo.VersionNumber().ToString()
            },
            {
                "BundleID",
                (JToken)Settings.Default.InstallationBundleId
            }
        };

        [Routing("insider")] public bool Insider => ProductSettings.IsInsider;

        public RemoteConnectionSettings ConnectedRegion => GetSelectedRegion(regions.RegionList);

        public ConnectionState ConnectionState => openVpn.ConnectionState;

        [Routing("userProfile")] public UserProfile UserProfileInfo => DiContainer.GetValue<UserProfile>("UserProfile");

        [Routing("uiSettings")]
        public JObject UiSettings
        {
            get { return JsonConvert.DeserializeObject<JObject>(ProductSettings.UiSettings); }
            set
            {
                ProductSettings.UiSettings = JsonConvert.SerializeObject(value);
                this.UiSettingsChanged?.Invoke(this, new EventArgs<JObject>(value));
            }
        }

        [Routing("serviceReady")] public event EventHandler<EventArgs> OnServiceReady;

        [Routing("traffic/get", true)]
        public event EventHandler<EventArgs<Avira.VPN.Core.Win.TrafficData>> TrafficChanged;

        [Routing("disconnectTimer")] public event EventHandler<DisconnectTimerEventArgs> OnDisconnectTimerChanged;

        [Routing("trafficLimitReached")] public event EventHandler<EventArgs> OnTrafficLimitReached;

        [Routing("killSwitchActivated")] public event EventHandler<EventArgs> KillSwitchActivated;

        [Routing("regions/get", true)] public event EventHandler<EventArgs<RegionList>> RegionsChanged;

        [Routing("status", true)] public event EventHandler<EventArgs<Status>> StatusChanged;

        [Routing("showNotification")] public event EventHandler<EventArgs<JObject>> ShowNotification;

        [Routing("wifis/get", true)] public event EventHandler<EventArgs<JToken>> WifiListChanged;

        [Routing("features/get", true)] public event EventHandler<EventArgs<JObject>> FeaturesChanged;

        [Routing("diagnostics/get", true)] public event EventHandler<EventArgs<DiagnosticData>> DiagnosticConfirmation;

        [Routing("ipAddressRefreshed")] public event EventHandler<EventArgs<IPData>> IpAddressRefreshed;

        [Routing("userRegistered")] public event EventHandler<EventArgs<string>> UserRegisteredChanged;

        [Routing("userProfileChanged")] public event EventHandler<EventArgs<UserProfile>> UserProfileChanged;

        [Routing("displayFastFeedback")] public event EventHandler DisplayFastFeedbackDialog;

        [Routing("displayDataUsagePopup")] public event EventHandler DisplayDataUsagePopup;

        [Routing("uiVisible")] public event EventHandler<EventArgs<JObject>> UiVisiblilityChanged;

        [Routing("systemSettingsChanged")]
        public event EventHandler<EventArgs<SystemSettingsData>> SystemSettingsChanged;

        [Routing("login")] public event EventHandler<EventArgs<JObject>> LoginResponse;

        [Routing("uiSettingsChanged")] public event EventHandler<EventArgs<JObject>> UiSettingsChanged;

        public VpnService()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            InitializeComponent();
            base.CanHandlePowerEvent = true;
            base.CanHandleSessionChangeEvent = true;
            reflectionService = new ReflectionService(this);
            StatusChanged += HandleStatusChanged;
        }

        private void HandleStatusChanged(object sender, EventArgs<Status> e)
        {
            if (e.Value.NewState == ConnectionState.Disconnected)
            {
                wifiManager?.ClearAutoconnectIds();
            }
        }

        [Routing("wifiAutoconnect")]
        public void WifiAutoconnect()
        {
            if (openVpnConnectionState == ConnectionState.Disconnected)
            {
                Serilog.Log.Information("Trigger WiFiAutoconnect.");
                unsecureWifiNetworkMonitor?.TriggerAutoconnect();
            }
        }

        private bool CheckIfSettingChanged(bool newSetting, bool oldSetting, string eventName, bool appSettingValue)
        {
            bool result = false;
            if (oldSetting != newSetting)
            {
                TrackSettingChanged(eventName, appSettingValue);
                result = true;
            }

            return result;
        }

        private static void TrackSettingChanged(string eventName, bool enabled)
        {
            Tracker.TrackEvent(eventName, new Dictionary<string, string>
            {
                {
                    "Enabled",
                    enabled.ToString()
                }
            });
        }

        [Routing("refreshIPAddress")]
        public void RefreshIpAddress()
        {
            ipChecker.Refresh().CatchAll();
        }

        [Routing("toggleInsider")]
        public bool ToggleInsider()
        {
            ProductSettings.IsInsider = !ProductSettings.IsInsider;
            remoteConfiguration.Refresh().CatchAll();
            return ProductSettings.IsInsider;
        }

        [Routing("exit")]
        public void HandleExitRequest()
        {
            DisconnectOpenVpn();
        }

        [Routing("themeColor")]
        public void ThemeColor(string theme)
        {
            ProductSettings.ThemeColor = theme;
        }

        [Routing("displaySettings/get")]
        public JObject GetDisplaySettings()
        {
            return JObject.Parse(ProductSettings.DisplaySettings);
        }

        [Routing("displaySettings/set")]
        public void DisplaySettings(JObject settings)
        {
            ProductSettings.DisplaySettings = JsonConvert.SerializeObject(settings);
        }

        [Routing("themeSelection/get")]
        public JObject ThemeSelection()
        {
            return JObject.Parse(ProductSettings.ThemeSelection);
        }

        [Routing("themeSelection/set")]
        public void SetThemeSelection(JObject value)
        {
            ProductSettings.ThemeSelection = JsonConvert.SerializeObject(value);
        }

        [Routing("resetUserConfig")]
        public void ResetUserConfig()
        {
            Settings.Default.Reset();
        }

        [Routing("regions/get")]
        public RegionList GetRegionList()
        {
            return regions.RegionList;
        }

        [Routing("disconnect")]
        public void Disconnect(string disconnectSource)
        {
            DisconnectVpn(disconnectSource);
        }

        [Routing("disconnect")]
        public void Disconnect()
        {
            DisconnectVpn("");
        }

        private void DisconnectVpn(string source)
        {
            DisconnectOpenVpn(source);
            dataUsagePopup.ShowDialogIfNeeded();
        }

        [Routing("status")]
        public Status HandleStatus()
        {
            return openVpn.Status;
        }

        [Routing("disconnect")]
        public void HandleCloseClicked()
        {
            if (openVpnConnectionState == ConnectionState.Disconnected)
            {
                ProductSettings.StartGuiAfterUpdate = true;
            }
        }

        [Routing("traffic/get")]
        public Avira.VPN.Core.Win.TrafficData GetCurrentTrafficInfo()
        {
            return new Avira.VPN.Core.Win.TrafficData
            {
                UsedInBytes = traffic.TrafficData.UsedTraffic,
                LimitInBytes = 0L,
                GracePeriodInSeconds = 60
            };
        }

        [Routing("wifis/getAll")]
        public JToken GetStoredWifiList()
        {
            if (unsecureWifiNetworkMonitor == null)
            {
                return new JArray();
            }

            string connectedId = unsecureWifiNetworkMonitor.ConnectedWifi;
            return JToken.FromObject(unsecureWifiNetworkMonitor.Wifis.Select((KnownWifis.WiFiData item) => new
            {
                Id = item.Id,
                Ssid = item.Ssid,
                TrustMode = item.TrustMode.ToString(),
                SecurityMode = item.SecurityMode.ToString(),
                Connected = (!string.IsNullOrEmpty(connectedId) && item.Id == connectedId)
            }).ToList());
        }

        [Routing("wifis/get")]
        public JToken GetCurrentWifiList()
        {
            if (unsecureWifiNetworkMonitor == null)
            {
                return new JArray();
            }

            string connectedId = unsecureWifiNetworkMonitor.ConnectedWifi;
            new JObject();
            var list = (from item in unsecureWifiNetworkMonitor.Wifis
                where item.TrustMode != TrustMode.Unknown
                select new
                {
                    Id = item.Id,
                    Ssid = item.Ssid,
                    Autoconnect = (item.TrustMode == TrustMode.Untrusted),
                    Connected = (!string.IsNullOrEmpty(connectedId) && item.Id == connectedId)
                }).ToList();
            if (!string.IsNullOrEmpty(connectedId))
            {
                var list2 = (from item in unsecureWifiNetworkMonitor.Wifis
                    where item.TrustMode == TrustMode.Unknown && item.Id == connectedId
                    select new
                    {
                        Id = item.Id,
                        Ssid = item.Ssid,
                        Autoconnect = false,
                        Connected = true
                    }).ToList();
                if (list2.Count > 0)
                {
                    list.Add(list2[0]);
                }
            }

            int num = list.FindIndex(item => item.Connected);
            if (num != -1)
            {
                var item2 = list[num];
                list.RemoveAt(num);
                list.Insert(0, item2);
            }

            return JToken.FromObject(list);
        }

        [Routing("trackEvent")]
        public void TrackEvent(JObject trackingEvent)
        {
            string eventName = trackingEvent["eventId"]!.ToString();
            Dictionary<string, string> properties = trackingEvent["properties"]?.ToObject<Dictionary<string, string>>();
            Tracker.TrackEvent(eventName, properties);
        }

        [Routing("wifis/trust")]
        public void TrustWifi(string id)
        {
            unsecureWifiNetworkMonitor.TrustById(id);
        }

        [Routing("wifis/untrust")]
        public void UntrustWifi(string id)
        {
            unsecureWifiNetworkMonitor.UntrustById(id);
        }

        [Routing("wifis/delete")]
        public void DeleteWifi(string id)
        {
            unsecureWifiNetworkMonitor.DeleteById(id);
        }

        [Routing("connect")]
        public void Connect(RemoteConnectionSettings region)
        {
            Connect(region, isTriggeredByAutoconnect: false);
        }

        [Routing("diagnostics/send")]
        public void SendDiagnostics(JObject userSelection)
        {
            diagnostic.CollectData(userSelection).ContinueWith(delegate(Task<bool> collectProcess)
            {
                DiagnosticData diagnosticData = (collectProcess.Result ? diagnostic.SendData() : null);
                this.DiagnosticConfirmation?.Invoke(this, new EventArgs<DiagnosticData>(diagnosticData));
                if (diagnosticData != null)
                {
                    diagnostic.LogReferenceNr(diagnosticData.DiagnosticId);
                    GenericAccessor.Set(ProductSettings.SharedStorage, "last_diagnostics",
                        JsonConvert.SerializeObject(diagnosticData));
                }
            });
        }

        [Routing("diagnostics/lastReference")]
        public JToken GetLastDiagnostics()
        {
            return JsonConvert.DeserializeObject<JObject>(GenericAccessor.Get(ProductSettings.SharedStorage,
                "last_diagnostics", "{}"));
        }

        private void Connect(RemoteConnectionSettings region, bool isTriggeredByAutoconnect)
        {
            ProductSettings.LastConnect = DateTime.UtcNow;
            JObject uiSettings = UiSettings;
            uiSettings["selectedRegion"] = (JToken)region.Id;
            UiSettings = uiSettings;
            Task.Run(delegate { ConnectThread(region, isTriggeredByAutoconnect); });
        }

        private void ConnectThread(RemoteConnectionSettings region, bool isTriggeredByAutoconnect)
        {
            if (Traffic.IsLimitReached())
            {
                DisconnectOpenVpn();
                this.OnTrafficLimitReached?.Invoke(this, new EventArgs());
            }
            else
            {
                RemoteConnectionSettings connectionRegion = DecideConnectionProtocol(region);
                connectionRegion = DecideConnectionUri(connectionRegion);
                openVpn.Connect(connectionRegion, isTriggeredByAutoconnect);
            }
        }

        private RemoteConnectionSettings DecideConnectionUri(RemoteConnectionSettings connectionRegion)
        {
            connectionRegion.Uri = regions.RegionList.GetDefault().Uri;
            foreach (RemoteConnectionSettings serversConnectionSetting in regions.RegionList.ServersConnectionSettings)
            {
                if (connectionRegion.Id == serversConnectionSetting.Id)
                {
                    connectionRegion.Uri = serversConnectionSetting.Uri;
                    return connectionRegion;
                }
            }

            return connectionRegion;
        }

        [Routing("registerUser")]
        public void RegisterUser()
        {
            GetLauncherDashboardAccessor().Register();
            Tracker.TrackEvent(Tracker.Events.RegistrationClicked);
        }

        [Routing("learnMore")]
        public void LearnMore()
        {
            educationMessageRotator?.Show();
        }

        [Routing("upgrade")]
        public void Upgrade()
        {
            HandleUpgrade();
        }

        [Routing("openDashboard")]
        public void OpenDashboard()
        {
            GetLauncherDashboardAccessor().OpenDashboard();
            Tracker.TrackEvent(Tracker.Events.OpenDashboard);
        }

        [Routing("userRegistered")]
        public string IsUserRegistered()
        {
            if (string.IsNullOrEmpty(auth2.AccessToken))
            {
                return "false";
            }

            return (!JwtToken.IsAnonymousToken(auth2?.AccessToken)).ToString().ToLower();
        }

        [Routing("sendFastFeedback")]
        public void SendFastFeedback(int rating)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>
            {
                { "Feedback Id", fastFeedback.FeedbackId },
                {
                    "Rating",
                    rating.ToString()
                },
                { "Host", ConnectedRegion.Uri },
                { "Region Id", ConnectedRegion.Id },
                {
                    "Connection Ip",
                    ipChecker.LastConnectedData?.IP
                },
                {
                    "Session Duration (seconds)",
                    sessionDuration.Elapsed.TotalSeconds.ToString("0")
                },
                {
                    "Session Traffic (MB)",
                    (openVpn.SessionTotalTraffic / 1024uL / 1024uL).ToString()
                }
            };
            int num = RegionsLatency.PossibleLatency(ConnectedRegion.Id, 0);
            if (num > 0)
            {
                dictionary.Add("Latency", num.ToString());
            }

            Tracker.TrackEvent(Tracker.Events.UserFeedback, dictionary);
        }

        [Routing("notNowFastFeedback")]
        public void NotNowFastFeedback()
        {
            Tracker.TrackEvent(Tracker.Events.UserFeedbackDismissed,
                new Dictionary<string, string> { { "Feedback Id", fastFeedback.FeedbackId } });
        }

        [Routing("fastFeedbackStrings")]
        public JObject FastFeedbackStrings()
        {
            return fastFeedback.FastFeedbackStrings();
        }

        [Routing("getProDataUsage")]
        public void GetProDataUsage()
        {
            dataUsagePopup.GetProDataUsage();
        }

        [Routing("notNowDataUsage")]
        public void NotNowDataUsage()
        {
            dataUsagePopup.NotNowDataUsage();
        }

        [Routing("uiVisibilityChanged")]
        public void NotifyUiVisibility(bool isVisible)
        {
            this.UiVisiblilityChanged?.Invoke(this, new EventArgs<JObject>(new JObject
            {
                {
                    "isVisible",
                    (JToken)isVisible
                }
            }));
        }

        [Routing("osThemeChanged")]
        public void NotifyOsThemeChanged(string theme)
        {
            this.SystemSettingsChanged?.Invoke(this, new EventArgs<SystemSettingsData>(new SystemSettingsData
            {
                Theme = theme
            }));
        }

        [Routing("login")]
        public void Login(JObject credentials)
        {
            DiContainer.Resolve<IAuthenticator>()?.Login(credentials).ContinueWith(delegate(Task<JObject> param)
            {
                this.LoginResponse?.Invoke(this, new EventArgs<JObject>(param.Result));
            });
        }

        [Routing("trackGuiOpenedTrigger")]
        public void TrackGuiOpenedTrigger(string triggerSource)
        {
            Tracker.TrackEvent(Tracker.Events.GuiOpenedTrigger,
                new Dictionary<string, string> { { "Trigger Source", triggerSource } });
        }

        [Routing("spotlightVpnIntegrated")]
        public void SpotlightVpnIntegrated(bool isIntegrated)
        {
            ProductSettings.IsSpotlightVpnIntegrated = isIntegrated;
        }

        private static void SignalWebAppHostIfRunning()
        {
            SharedStartEvent sharedStartEvent = new SharedStartEvent();
            if (sharedStartEvent.Exists())
            {
                Serilog.Log.Information("WebApphost is running. Signaling event for reconnection.");
                sharedStartEvent.Signal();
            }
        }

        private static void SignalSpotlightIfRunning()
        {
            SharedStartEvent sharedStartEvent = new SharedStartEvent("Global\\EDF9B880-F67B-45FD-83F3-7EAD199668B5");
            if (sharedStartEvent.Exists())
            {
                Serilog.Log.Information("Spotlight service is running. Signaling event for reconnection.");
                sharedStartEvent.Signal();
            }
        }

        protected override void OnStart(string[] args)
        {
            Serilog.Log.Information("{0} started. Version: {1}", base.ServiceName,
                FileSystem.GetVersion(GetType().Assembly.Location));
            Catch.All(AdjustSettingsPathAccessControl);
            SettingsMigrator.MigrateSettings();
            if (ProductSettings.InstallDate == DateTime.MinValue)
            {
                ProductSettings.InstallDate = File.GetCreationTimeUtc(ProductSettings.SettingsFilePath);
            }

            Serilog.Log.Information("{0} Install Date : {1}", base.ServiceName, ProductSettings.InstallDate);
            serviceStartTask = Task.Run(delegate { Start(); });
            Serilog.Log.Debug("VpnService - exiting OnStart()");
            NativeMethods.ServiceStatus serviceStatus = default(NativeMethods.ServiceStatus);
            serviceStatus.dwCurrentState = NativeMethods.ServiceState.SERVICE_RUNNING;
            NativeMethods.ServiceStatus serviceStatus2 = serviceStatus;
            NativeMethods.SetServiceStatus(base.ServiceHandle, ref serviceStatus2);
        }

        private void AdjustSettingsPathAccessControl()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(ProductSettings.SettingsFilePath);
            DirectorySecurity accessControl = directoryInfo.GetAccessControl();
            AuthorizationRuleCollection accessRules = accessControl.GetAccessRules(includeExplicit: true,
                includeInherited: false, typeof(SecurityIdentifier));
            FileSystemAccessRule fileSystemAccessRule = new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), FileSystemRights.Write,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly, AccessControlType.Allow);
            foreach (FileSystemAccessRule item in accessRules)
            {
                if (item.IdentityReference.Value == fileSystemAccessRule.IdentityReference.Value)
                {
                    return;
                }
            }

            accessControl.AddAccessRule(fileSystemAccessRule);
            directoryInfo.SetAccessControl(accessControl);
        }

        protected override void OnStop()
        {
            Serilog.Log.Debug("Stopping VPN service ...");
            if (serviceStartTask != null && !serviceStartTask.IsCompleted)
            {
                Serilog.Log.Debug("Waiting for the service start task...");
                Task.WaitAll(new Task[1] { serviceStartTask }, TimeSpan.FromMinutes(1.0));
                Serilog.Log.Debug("Service start task completed.");
            }

            launcherGuiController?.Stop();
            DisconnectOpenVpn();
            communciatorServer?.Dispose();
            DiContainer.Clear();
            regions?.Dispose();
            userActivityMonitor?.Dispose();
            Serilog.Log.Debug("VpnService - exiting OnStop()");
        }

        private void CreateVpnConnector()
        {
            vpnConnectorBridge = new VpnConnectorBridge();
            DiContainer.SetInstance<IVpnConnector>(vpnConnectorBridge);
        }

        private void CreateFastFeedback()
        {
            fastFeedback = new FastFeedback(ProductSettings.ProductLanguage, delegate
            {
                bool num = DiContainer.Resolve<IAppSettings>().Get().FastFeedback;
                bool productImprovementUserSetting = ProductSettings.ProductImprovementUserSetting;
                if (!num || !productImprovementUserSetting)
                {
                    return false;
                }

                return ProductSettings.FastFeedbackStillShowUserSetting &&
                       DateTime.UtcNow - ProductSettings.LastFeedbackNotificationDate >
                       ProductSettings.FeedbackNotificationMinPeriod;
            });
            fastFeedback.DisplayFastFeedbackDialog += delegate
            {
                this.DisplayFastFeedbackDialog?.Invoke(this, EventArgs.Empty);
                ProductSettings.LastFeedbackNotificationDate = DateTime.Now;
            };
            DiContainer.SetInstance<FastFeedback>(fastFeedback);
        }

        private void CreateDataUsagePopup()
        {
            dataUsagePopup = new DataUsagePopup(delegate
            {
                ulong num = 0uL;
                if (features.IsActive("data_usage_popup"))
                {
                    if (features.GetFeatureData("data_usage_popup").Params.TryGetValue("minUsedTraffic", out var value))
                    {
                        num = value.ToObject<ulong>();
                    }

                    if (traffic.TrafficData.UsedTraffic >= num)
                    {
                        return DateTime.UtcNow - ProductSettings.LastFeedbackNotificationDate >
                               ProductSettings.FeedbackNotificationMinPeriod;
                    }

                    return false;
                }

                return false;
            });
            dataUsagePopup.DisplayDataUsagePopupDialog += delegate
            {
                this.DisplayDataUsagePopup?.Invoke(this, EventArgs.Empty);
            };
        }

        private void CreateRemoteConfiguration()
        {
            remoteConfiguration = new RemoteConfiguration(
                new ApiClient<RemoteConfigurationData>(Settings.Default.VpnBackendUrl,
                    "features?device_id=" + GeneratedDeviceInfo.GetClientId(), "remote_features"),
                GenerateFeaturesPayload, TimeSpan.FromHours(24.0));
            remoteConfiguration.ConfigurationChanged += FeaturesRefreshedHandler;
            DiContainer.SetInstance<IRemoteConfiguration>(remoteConfiguration);
        }

        private void CreateOpenVpn()
        {
            openVpn = new OpenVpn
            {
                Credentials = new Credentials
                {
                    UserId = () => GeneratedDeviceInfo.GetClientId(),
                    Password = delegate
                    {
                        string accessToken = auth2.AccessToken;
                        return (!string.IsNullOrEmpty(accessToken))
                            ? ((!JwtToken.IsAnonymousToken(accessToken))
                                ? auth2.AccessTokenHash
                                : GeneratedDeviceInfo.GetClientId())
                            : GeneratedDeviceInfo.GetClientId();
                    }
                }
            };
            DiContainer.SetInstance<IOpenVpn>(openVpn);
        }

        private void CreateAndRegisterDiObjects()
        {
            InitWifiMonitor();
            ProductSettings.BundleId = Settings.Default.InstallationBundleId;
            DiContainer.SetInstance<ISettings>(new WinSettings(Settings.Default));
            DiContainer.SetInstance<ISecureSettings>(new WinSecureSettings());
            DiContainer.SetInstance<IProductSettings>(new ProductSettingsBridge());
            DiContainer.SetInstance<IApplicationIds>(new ApplicationIds());
            DiContainer.SetInstance<IAppStateNotifier>(new AppStateNotifier());
            DiContainer.SetGetter("OeApiUrl", () => Settings.Default.OeApi);
            DiContainer.SetGetter("ResourceManager", () => ResourcesVpnService.ResourceManager);
            DiContainer.SetGetter("Culture", () => ResourcesVpnService.Culture);
            DiContainer.SetInstance<ApplicationSettingsBase>(Settings.Default);
            DiContainer.ExportedTypes exportedTypes = DiContainer.GetExportedTypes(
                Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    Settings.Default.UserManagementAsembly)));
            DiContainer.Register<IAuthenticator>(exportedTypes);
            if (exportedTypes.ContainsKey(typeof(ILauncherGuiController)))
            {
                DiContainer.Register<ILauncherGuiController>(exportedTypes);
                launcherGuiController = DiContainer.Resolve<ILauncherGuiController>();
                launcherGuiController?.Initialize();
            }

            DiContainer.Register<IDashboardAccessor>(exportedTypes, CreationMode.Multiple);
            DiContainer.Register<IUserManagementController>(exportedTypes);
            DiContainer.SetInstance<Ratings>(new Ratings());
            auth2 = DiContainer.Resolve<IAuthenticator>();
            vpnApiHttpClient = new VpnHttpClient(auth2);
            if (exportedTypes.ContainsKey(typeof(IOeStatusReporter)))
            {
                DiContainer.Register<IOeStatusReporter>(exportedTypes);
            }
            else
            {
                DiContainer.SetInstance<IOeStatusReporter>(new OeStatusReporter(new OeApi(VpnSettings.OeApiUrl)));
            }

            vpnNotifier = new VpnNotifier(this, unsecureWifiNetworkMonitor, this, new NotifierClient());
            if (Settings.Default.EducationMessageActive)
            {
                educationMessageRotator = new EducationMessageRotator(vpnNotifier);
            }

            traffic = new Traffic(vpnApiHttpClient, GeneratedDeviceInfo.GetClientId());
            DiContainer.SetInstance<Traffic>(traffic);
            DiContainer.SetInstance<ITraffic>(traffic);
            InitWifiManager();
            regions = new Regions(vpnApiHttpClient, GeneratedDeviceInfo.GetClientId());
            CreateRemoteConfiguration();
            features = new Features(remoteConfiguration);
            DiContainer.SetInstance<IFeatures>(features);
            appSettings = new AppSettings();
            DiContainer.SetInstance<IAppSettings>(appSettings);
            DiContainer.SetInstance<Regions>(regions);
            customNotifications = new CustomNotifications(ProductSettings.SecureStorage);
            DiContainer.SetInstance<CustomNotifications>(customNotifications);
            experimentTracker = new ExperimentTracker();
            DiContainer.SetInstance<IOeStatusProvider>(
                new StatusProvider(TimeSpan.FromSeconds(Settings.Default.OeStatusUpdateInterval)));
            CreateVpnConnector();
            CreateFastFeedback();
            CreateDataUsagePopup();
            CreateUserActivityMonitor();
            nodeSettings = new NodeSettings();
            statusReporter = DiContainer.Resolve<IOeStatusReporter>();
            statusReporter.Start(TimeSpan.FromMilliseconds(15000.0),
                TimeSpan.FromSeconds(Settings.Default.OeStatusUpdateInterval));
            CreateOpenVpn();
            DiContainer.SetInstance<IVpnProvider>(this);
            launcherGuiController?.SetVpnProvider(this);
            diagnostic = new Diagnostics(vpnApiHttpClient, GeneratedDeviceInfo.GetClientId());
            DiContainer.SetInstance<IDiagnostics>(diagnostic);
            DiContainer.SetInstance<IInternetAvailabilityMonitor>(new InternetAvailabilityMonitor());
            ipChecker = new IPChecker(new VpnBackendApiClient<IPData>(Settings.Default.VpnBackendUrl, "whoami",
                "whoami"));
            ipChecker.IPRefreshed += delegate { HandleIpRefresh(); };
        }

        private void HandleIpRefresh()
        {
            this.IpAddressRefreshed?.Invoke(null, new EventArgs<IPData>(ipChecker.Data));
            if (ConnectionState == ConnectionState.Connected)
            {
                ipChecker.LastConnectedData = new IPData
                {
                    Country = ipChecker.Data.Country,
                    IP = ipChecker.Data.IP
                };
            }
        }

        private void CreateUserActivityMonitor()
        {
            TimeSpan inactivityThreshold = Settings.Default.InactivityThreshold;
            TimeSpan popupTimeout = Settings.Default.PopupTimeout;
            if (features.IsActive("inactivity_notification") && features.GetFeatureData("inactivity_notification")
                    .Params.TryGetValue("inactivity_threshold", out var value))
            {
                inactivityThreshold = TimeSpan.FromMinutes((double)value.ToObject<int>());
            }

            userActivityMonitor = new UserActivityMonitor(inactivityThreshold, popupTimeout);
        }

        private void FeaturesRefreshedHandler(object sender, EventArgs e)
        {
            DiContainer.Resolve<CustomNotifications>().Update(remoteConfiguration.RemoteFeatures).Catch(
                delegate(Exception ex) { Serilog.Log.Error(ex, "Failed to update custom notifications."); });
            this.FeaturesChanged?.Invoke(this, new EventArgs<JObject>(DiContainer.Resolve<IFeatures>().Serialize()));
            UpdateFastFeedbackContent(remoteConfiguration.RemoteFeatures);
            TrackRemoteFeaturesChanged();
        }

        private void TrackRemoteFeaturesChanged()
        {
            string text = JsonConvert.SerializeObject(remoteConfiguration.RemoteFeatures);
            if (!(text != ProductSettings.LastSavedRemoteFeatures))
            {
                return;
            }

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            foreach (RemoteFeatureData remoteFeature in remoteConfiguration.RemoteFeatures)
            {
                dictionary[remoteFeature.Id] = remoteFeature.IsActive.ToString();
                dictionary[remoteFeature.Id + "Default"] = remoteFeature.DefaultValue;
            }

            Tracker.TrackEvent(Tracker.Events.RemoteFeaturesChanged, dictionary);
            ProductSettings.LastSavedRemoteFeatures = text;
        }

        private void UpdateFastFeedbackContent(List<RemoteFeatureData> remoteFeatures)
        {
            RemoteFeatureData remoteFeatureData = remoteFeatures.Find((RemoteFeatureData f) => f.Id == "fast_feedback");
            if (remoteFeatureData != null && remoteFeatureData.IsActive)
            {
                DiContainer.Resolve<FastFeedback>().Update(remoteFeatureData.Params.ToObject<FastFeedbackData>());
            }
        }

        private JObject GenerateFeaturesPayload()
        {
            KnownWifis knownWiFis = new ServicePersistentData().KnownWiFis;
            Ratings ratings = DiContainer.Resolve<Ratings>();
            return new JObject
            {
                ["version"] = (JToken)ProductSettings.PreviousVersion.ToString(),
                ["installation_date"] = (JToken)ProductSettings.InstallDate.ToString("s"),
                ["bundle_id"] = (JToken)Settings.Default.InstallationBundleId,
                ["language"] = (JToken)ProductSettings.ProductLanguage,
                ["download_source"] = (JToken)ProductSettings.DownloadSource,
                ["beta"] = (JToken)Settings.Default.IsBeta,
                ["insider"] = (JToken)ProductSettings.IsInsider,
                ["diagnosticDataEnabled"] = (JToken)ProductSettings.ProductImprovementUserSetting.ToString(),
                ["attributes"] = new JObject
                {
                    ["connected_wifis"] = new JObject
                    {
                        {
                            "1d",
                            (JToken)(knownWiFis?.GetConnectedWifis(1) ?? 0)
                        },
                        {
                            "2d",
                            (JToken)(knownWiFis?.GetConnectedWifis(2) ?? 0)
                        },
                        {
                            "7d",
                            (JToken)(knownWiFis?.GetConnectedWifis(7) ?? 0)
                        },
                        {
                            "30d",
                            (JToken)(knownWiFis?.GetConnectedWifis(30) ?? 0)
                        }
                    },
                    ["sar"] = (JToken)(ratings?.SecurityAfinityRating),
                    ["dar"] = (JToken)(ratings?.DownloadAfinityRating),
                    ["cii"] = (JToken)ProductSettings.InitialClientId,
                    ["cit"] = (JToken)ProductSettings.ClientIdChangeTotal
                }
            };
        }

        private void VpnService_UserProfileChanged(object sender, EventArgs<JObject> e)
        {
            Serilog.Log.Debug("User name changed");
        }

        public void Start()
        {
            CreateAndRegisterDiObjects();
            CleanupEnvironment();
            auth2.AccessTokenChanged += delegate
            {
                this.UserRegisteredChanged?.Invoke(this, new EventArgs<string>(IsUserRegistered()));
                this.UserProfileChanged?.Invoke(this,
                    new EventArgs<UserProfile>(DiContainer.GetValue<UserProfile>("UserProfile") ?? new UserProfile()));
            };
            traffic.TrafficChanged += OnTrafficChanged;
            regions.RegionsListUpdated += delegate(object sender, EventArgs args)
            {
                this.RegionsChanged?.Invoke(sender, new EventArgs<RegionList>(GetRegionList()));
            };
            openVpn.StateChangedNotification += OpenVpnOnStateChangedNotification;
            openVpn.StateChangedNotification += vpnConnectorBridge.OpenVpnOnStateChangedNotification;
            openVpn.TrafficChanged += traffic.OnTrafficChanged;
            StartCommunicatorServer();
            Task task = Task.Run(delegate
            {
                traffic.Refresh();
                remoteConfiguration.Refresh().CatchAll();
                unsecureWifiNetworkMonitor?.TriggerAutoconnect();
            });
            SignalWebAppHostIfRunning();
            SignalSpotlightIfRunning();
            this.OnServiceReady?.Invoke(this, new EventArgs());
            if (ProductSettings.StartGuiAfterUpdate)
            {
                StartClientApp("Update Process", startMinimized: true);
                ProductSettings.StartGuiAfterUpdate = false;
            }

            InitUserActivityMonitor();
            KillSwitchActivated += delegate { vpnNotifier.NotifyKillSwitchActivated(); };
            ProductSettings.ProductVersionChanged += delegate
            {
                new TapDriver(new ProcessWrapper(), new PathWhiteList(), DiContainer.Resolve<ISettings>())
                    .UpdateTapDriverIfNecessary();
            };
            ProductSettings.CheckForVersionUpdate();
            if (features.IsActive("custom_traffic_limit"))
            {
                TrafficRefreshTimer();
            }

            Task.WaitAll(new Task[1] { task }, TimeSpan.FromSeconds(5.0));
        }

        private void TrafficRefreshTimer()
        {
            int num = rnd.Next(5, 16);
            Task.Delay(DateTime.Today.AddDays(1.0).AddMinutes(num) - DateTime.Now).ContinueWith(delegate
            {
                Task.Run(delegate { traffic.Refresh(); }).CatchAll();
                TrafficRefreshTimer();
            });
        }

        private void TriggerWifiListChanged(object sender, EventArgs args)
        {
            this.WifiListChanged?.Invoke(this, new EventArgs<JToken>(GetCurrentWifiList()));
        }

        private void CleanupEnvironment()
        {
            if (!ProductSettings.KillSwitchUserSetting)
            {
                NetworkBlocker.Disable();
            }

            new AbandonedProcess(FileSystem.MakeFullPath(ProductSettings.OpenVpnPath)).SoftCleanRunningInstances();
        }

        private void StartCommunicatorServer()
        {
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,
                PipeAccessRights.FullControl, AccessControlType.Allow));
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.ReadWrite,
                AccessControlType.Allow));
            communciatorServer = new PipeCommunicatorServer(ProductSettings.VpnPipeName, pipeSecurity);
            if (!Settings.Default.DisablePipeAccessAuthorization)
            {
                communciatorServer.AuthorizationChecker = new PipeAuthorizationChecker();
            }

            communciatorServer.Start();
            router = new Router(new Multiplexer(communciatorServer));
            router.AddAllRoutes(reflectionService);
            router.AddAllRoutes(new ReflectionService(latencyProber));
            IUserManagementController userManagementController = DiContainer.Resolve<IUserManagementController>();
            if (userManagementController != null)
            {
                router.AddAllRoutes(new ReflectionService(userManagementController));
                userManagementController.RunAfterUserProfileChanged = delegate { auth2.Refresh().CatchAll(); };
            }
        }

        private void InitUserActivityMonitor()
        {
            try
            {
                userActivityMonitor.Installed += delegate { NotifyWelcome(); };
                userActivityMonitor.OnStart();
            }
            catch (Exception exception)
            {
                Serilog.Log.Warning(exception, "Failed to initialize user activity monitor.");
            }
        }

        private void NotifyWelcome()
        {
            if (Settings.Default.InstallationBundleId.StartsWith("avpn") ||
                Settings.Default.InstallationBundleId.StartsWith("avpp") ||
                Settings.Default.InstallationBundleId.StartsWith("vpnb"))
            {
                vpnNotifier.NotifyFtu(true);
            }
        }

        private void NotifyTrafficLimitReached()
        {
            if (openVpn.Status.NewState != 0)
            {
                string message = string.Format(ResourcesVpnService.TrafficLimitReached, 60);
                vpnNotifier.NotifyTrafficLimitReached(message);
            }
        }

        public void ConnectToLastSelectedRegion(string triggerSource, bool isTriggeredByAutoconnect = false)
        {
            if (regions?.RegionList == null)
            {
                Serilog.Log.Error("Couldn't connect the VPN. There are no connection regions.");
                return;
            }

            RemoteConnectionSettings remoteConnectionSettings =
                new RemoteConnectionSettings(GetSelectedRegion(regions.RegionList));
            remoteConnectionSettings.TriggerSource = triggerSource;
            Serilog.Log.Information($"Connecting to Last Selected Region: {remoteConnectionSettings}");
            Connect(remoteConnectionSettings, isTriggeredByAutoconnect);
        }

        public void StartClientApp(string triggerSource, bool startMinimized = false)
        {
            if (!startMinimized)
            {
                Tracker.TrackEvent(Tracker.Events.GuiOpenedTrigger,
                    new Dictionary<string, string> { { "Trigger Source", triggerSource } });
            }

            DesktopShell.ShellExecute(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProductSettings.WebAppHostExe),
                startMinimized ? "/hide" : string.Empty, AppDomain.CurrentDomain.BaseDirectory);
        }

        internal static RemoteConnectionSettings GetSelectedRegion(RegionList regionList)
        {
            string selectedRegion = regionList.DefaultRegion;
            try
            {
                Dictionary<string, object> dictionary =
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(ProductSettings.UiSettings);
                if (dictionary.ContainsKey("selectedRegion") &&
                    !string.IsNullOrEmpty(dictionary["selectedRegion"]?.ToString()))
                {
                    selectedRegion = dictionary["selectedRegion"]?.ToString();
                }
            }
            catch (Exception exception)
            {
                Serilog.Log.Warning(exception, "GetSelectedRegion failed");
            }

            return regionList.ServersConnectionSettings.FirstOrDefault((RemoteConnectionSettings r) =>
                r.Id.Equals(selectedRegion)) ?? regionList.GetDefault();
        }

        private void OnTrafficChanged(object sender, EventArgs args)
        {
            Avira.VPN.Core.Win.TrafficData currentTrafficInfo = GetCurrentTrafficInfo();
            this.TrafficChanged?.Invoke(this, new EventArgs<Avira.VPN.Core.Win.TrafficData>(currentTrafficInfo));
            NotifyUserIfTrafficThreshHoldsReached(currentTrafficInfo);
            if (Traffic.IsLimitReached())
            {
                HandleTrafficLimitReached();
            }
        }

        private void NotifyUserIfTrafficThreshHoldsReached(Avira.VPN.Core.Win.TrafficData trafficData)
        {
            if (openVpn.Status.NewState != 0)
            {
                Tuple<string, string> tuple = TrafficThreshHoldReached(trafficData);
                if (!string.IsNullOrEmpty(tuple.Item1))
                {
                    vpnNotifier.NotifyTrafficThreshHoldsReached(tuple);
                }
            }
        }

        private Tuple<string, string> TrafficThreshHoldReached(Avira.VPN.Core.Win.TrafficData trafficData)
        {
            ulong num = trafficData.LimitInBytes / 2uL;
            if (trafficData.UsedInBytes <= num || trafficData.LimitInBytes == 0L ||
                trafficData.UsedInBytes >= trafficData.LimitInBytes)
            {
                return new Tuple<string, string>(string.Empty, string.Empty);
            }

            double num2 = (double)trafficData.UsedInBytes * 100.0 / (double)trafficData.LimitInBytes;
            if (num2 >= 90.0)
            {
                if (traffic90PercentReachedNotification)
                {
                    return new Tuple<string, string>(string.Empty, string.Empty);
                }

                traffic90PercentReachedNotification = true;
                return new Tuple<string, string>(string.Format(ResourcesVpnService.TrafficXPercentReached, 90),
                    "Traffic90PercentReached");
            }

            if (num2 >= 80.0)
            {
                if (traffic80PercentReachedNotification)
                {
                    return new Tuple<string, string>(string.Empty, string.Empty);
                }

                traffic80PercentReachedNotification = true;
                return new Tuple<string, string>(string.Format(ResourcesVpnService.TrafficXPercentReached, 80),
                    "Traffic80PercentReached");
            }

            if (num2 >= 50.0)
            {
                if (traffic50PercentReachedNotification)
                {
                    return new Tuple<string, string>(string.Empty, string.Empty);
                }

                traffic50PercentReachedNotification = true;
                return new Tuple<string, string>(string.Format(ResourcesVpnService.TrafficXPercentReached, 50),
                    "Traffic50PercentReached");
            }

            return new Tuple<string, string>(string.Empty, string.Empty);
        }

        private void HandleTrafficLimitReached()
        {
            this.OnTrafficLimitReached?.Invoke(this, new EventArgs());
            NotifyTrafficLimitReached();
            Tracker.TrackEvent(Tracker.Events.TrafficLimitReached, new Dictionary<string, string>
            {
                {
                    "Used Traffic (bytes)",
                    traffic.TrafficData.UsedTraffic.ToString()
                }
            });
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            Serilog.Log.Debug(
                $"=> Session changed for {changeDescription.SessionId} ({changeDescription.Reason.ToString()})");
            switch (changeDescription.Reason)
            {
                case SessionChangeReason.ConsoleConnect:
                    if (!isLogoffDone)
                    {
                        TriggerUIWithDelay(3000);
                    }

                    break;
                case SessionChangeReason.SessionLogon:
                    isLogoffDone = false;
                    TriggerUIWithDelay(3000);
                    break;
                case SessionChangeReason.SessionLogoff:
                    isLogoffDone = true;
                    break;
            }
        }

        private void TriggerUIWithDelay(int milliseconds)
        {
            Task.Run(async delegate
            {
                await Task.Delay(milliseconds);
                if (openVpn.ConnectionState != 0)
                {
                    StartClientApp("Session Change");
                }
            });
        }

        private void InitWifiMonitor()
        {
            try
            {
                WifiNetworkMonitor wifiNetworkMonitor = new WifiNetworkMonitor();
                DiContainer.SetInstance<IWifiNetworkMonitor>(wifiNetworkMonitor);
                unsecureWifiNetworkMonitor = new WifiNetworkManager(wifiNetworkMonitor);
            }
            catch (Exception ex)
            {
                Win32Exception ex2 = ex as Win32Exception;
                if (ex2 != null && ex2.NativeErrorCode == 1062)
                {
                    Serilog.Log.Information("WLAN AutoConfig service is not started. WiFiMonitor will be disabled");
                }
                else
                {
                    Serilog.Log.Warning(ex, "Failed to initialize Wifi Monitor.");
                }
            }
        }

        private void InitWifiManager()
        {
            if (unsecureWifiNetworkMonitor == null)
            {
                Serilog.Log.Warning("Wifi Manager not initialized.");
                return;
            }

            wifiManager = new WifiManager(unsecureWifiNetworkMonitor, vpnNotifier);
            WifiManager obj = wifiManager;
            obj.WifiAutoconnected = (EventHandler<EventArgs>)Delegate.Combine(obj.WifiAutoconnected,
                (EventHandler<EventArgs>)delegate
                {
                    ConnectToLastSelectedRegion("Wifi Autoconnect", isTriggeredByAutoconnect: true);
                    StartClientApp("Wifi Autoconnect", !Traffic.IsLimitReached());
                });
            WifiManager obj2 = wifiManager;
            obj2.AutoconnectedWifiDisconnected = (EventHandler<EventArgs>)Delegate.Combine(
                obj2.AutoconnectedWifiDisconnected, (EventHandler<EventArgs>)delegate { Disconnect(); });
            unsecureWifiNetworkMonitor.WifiListChanged += TriggerWifiListChanged;
            unsecureWifiNetworkMonitor.WifiNetworkDisconnected += TriggerWifiListChanged;
            unsecureWifiNetworkMonitor.WifiNetworkConnected += TriggerWifiListChanged;
        }

        private void ShowWifiAutoConnectNotification()
        {
            if (!string.IsNullOrEmpty(wifiManager?.AutoconnectSsid))
            {
                JObject value = new JObject
                {
                    ["message"] =
                        (JToken)string.Format(ResourcesVpnService.WifiAutoConnect, wifiManager.AutoconnectSsid),
                    ["type"] = (JToken)"info",
                    ["timeout"] = (JToken)5000,
                    ["notificationId"] = (JToken)"Autoconnected"
                };
                this.ShowNotification?.Invoke(this, new EventArgs<JObject>(value));
            }
        }

        private void OpenVpnOnStateChangedNotification(object sender, Status status)
        {
            Serilog.Log.Debug($"[!] VpnService: {status.NewState.ToString()}, {status.Error}, {status.Message}\n");
            if (status.NewState == ConnectionState.Connected)
            {
                NodeSessionInfo nodeSessionInfo = new NodeSessionInfo
                {
                    SelectedRegionId = ConnectedRegion.Id,
                    Fronted = vpnApiHttpClient.UsedFronting
                };
                Task.Run(async delegate { await nodeSettings.UpdateFeatures(nodeSessionInfo); });
                ShowWifiAutoConnectNotification();
                bool enableNetworkBlockerOnConnect = Settings.Default.EnableNetworkBlockerOnConnect;
                if (ProductSettings.KillSwitchUserSetting || enableNetworkBlockerOnConnect)
                {
                    Thread.Sleep(NetworkBlockerDelay);
                    NetworkBlocker.Enable();
                }

                sessionDuration.Restart();
            }

            if (status.NewState == ConnectionState.Disconnected)
            {
                if (disconnectWasRequested || !ProductSettings.KillSwitchUserSetting)
                {
                    NetworkBlocker.Disable();
                }

                disconnectWasRequested = false;
                if (NetworkBlocker.Enabled)
                {
                    this.KillSwitchActivated?.Invoke(this, EventArgs.Empty);
                }

                sessionDuration.Stop();
            }

            this.StatusChanged?.Invoke(sender, new EventArgs<Status>(status));
            openVpnConnectionState = status.NewState;
            if (status.NewState == ConnectionState.Disconnected && (status.Error == ErrorType.NetworkError ||
                                                                    status.Error == ErrorType.DnsError ||
                                                                    status.Error == ErrorType.ServerError))
            {
                Task.Run(delegate { regions.ForceUpdateRegions(); });
            }
        }

        public void HandleUpgrade()
        {
            GetLauncherDashboardAccessor().Upgrade();
        }

        private bool IsUdpAvailable()
        {
            try
            {
                using UdpChecker udpChecker = new UdpChecker(Settings.Default.UdpEchoServerUrl);
                return udpChecker.IsUdpAvailable();
            }
            catch (Exception)
            {
                return false;
            }
        }

        private RemoteConnectionSettings DecideConnectionProtocol(RemoteConnectionSettings region)
        {
            if (DiContainer.Resolve<IAppSettings>().Get().UdpSupport && IsUdpAvailable())
            {
                region.Protocol = "udp";
                region.Port = 1194;
                region.TlsHadshakeWindow = 10;
                region.FallbackProtocol = "tcp";
                region.FallbackPort = 443;
            }
            else
            {
                region.Protocol = "tcp";
                region.Port = 443;
                region.TlsHadshakeWindow = 60;
                region.FallbackProtocol = null;
                region.FallbackPort = 0;
            }

            return region;
        }

        private IDashboardAccessor GetLauncherDashboardAccessor()
        {
            return DiContainer.Resolve<IDashboardAccessor>();
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            Serilog.Log.Debug($"PowerEvent {powerStatus}");
            if (powerStatus == PowerBroadcastStatus.ResumeSuspend)
            {
                userActivityMonitor.OnStart();
                Task.Run(delegate
                {
                    Thread.Sleep(4000);
                    traffic.Refresh();
                });
            }

            return base.OnPowerEvent(powerStatus);
        }

        private void DisconnectOpenVpn(string disconnectSource = "")
        {
            if (fastFeedback != null)
            {
                fastFeedback.DisconnectSource = disconnectSource;
            }

            disconnectWasRequested = true;
            openVpn?.Disconnect();
            if (traffic != null)
            {
                ProductSettings.UsedTraffic = (long)traffic.TrafficData.UsedTraffic;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                if (statusReporter != null)
                {
                    statusReporter.Dispose();
                }

                if (openVpn != null)
                {
                    openVpn.Dispose();
                }

                if (communciatorServer != null)
                {
                    communciatorServer.Dispose();
                }

                if (regions != null)
                {
                    regions.Dispose();
                }

                if (unsecureWifiNetworkMonitor != null)
                {
                    unsecureWifiNetworkMonitor.Dispose();
                }

                if (userActivityMonitor != null)
                {
                    userActivityMonitor.Dispose();
                }

                if (vpnNotifier != null)
                {
                    vpnNotifier.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new Container();
            base.ServiceName = ProductSettings.ProductName;
        }
    }
}