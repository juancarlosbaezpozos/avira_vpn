using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avira.Messaging;
using Avira.VPN.Shared.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.Core
{
    public sealed class VpnController : IVpnController, IDisposable
    {
        private readonly IVpnConnector vpn;

        private readonly IAuthenticator authenticator;

        private readonly Traffic traffic;

        private readonly IUserManagement loginHandler;

        private readonly ISettings settings = DiContainer.Resolve<ISettings>();

        private readonly IDevice device = DiContainer.Resolve<IDevice>();

        private readonly IUdpPortScanner udpPortScanner = DiContainer.Resolve<IUdpPortScanner>();

        private readonly Features features;

        private readonly RemoteConfiguration remoteConfig;

        private readonly ExperimentTracker experimentTracker;

        private readonly VpnBackendApiClient<RegionList> regionsClient;

        private readonly VpnBackendApiClient<TrafficData> trafficClient;

        private readonly IPChecker ipChecker;

        private bool disconnectRequested;

        private bool disconnectCountdownStarted;

        private CancellationTokenSource disconnectTimerCancellationToken;

        private bool limitReachedReported;

        private ConnectionMonitor connectionMonitor;

        private uint pollForTokenStart;

        [Routing("features")] public JObject Features => DiContainer.Resolve<IFeatures>().Serialize();

        [Routing("traffic")] public Traffic Traffic => traffic;

        [Routing("regions")] public Regions Regions { get; set; }

        [Routing("userProfile")] public UserProfile UserProfileInfo => DiContainer.GetValue<UserProfile>("UserProfile");

        [Routing("deviceData")]
        public JObject DeviceData => new JObject
        {
            {
                "name",
                (JToken)DiContainer.Resolve<IDevice>().MachineName
            },
            {
                "type",
                (JToken)"pc"
            },
            {
                "os",
                (JToken)DiContainer.Resolve<IProductSettings>().OsType
            },
            {
                "os_type",
                (JToken)"desktop"
            },
            {
                "hardware_id",
                (JToken)DiContainer.Resolve<IApplicationIds>().DeviceId
            }
        };

        [Routing("appData")]
        public JObject AppData => new JObject
        {
            {
                "app_id",
                (JToken)"avpn0"
            },
            {
                "download_source",
                (JToken)"wd"
            }
        };

        public VpnStatus Status => vpn.Status;

        [Routing("appSettings")]
        public JObject GuiAppSettings
        {
            get
            {
                return JsonConvert.DeserializeObject<JObject>(
                    JsonConvert.SerializeObject(DiContainer.Resolve<IAppSettings>().Get()));
            }
            set
            {
                AppSettingsData appSettingsData = DiContainer.Resolve<IAppSettings>().Get();
                AppSettingsData appSettingsData2 = DiContainer.Resolve<IAppSettings>().Update(value);
                if ((false | CheckIfSettingChanged(appSettingsData2.MalwareProtection,
                         appSettingsData.MalwareProtection, Tracker.Events.MalwareProtectionChanged) |
                     CheckIfSettingChanged(appSettingsData2.AdBlocking, appSettingsData.AdBlocking,
                         Tracker.Events.AdBlockingChanged)) && vpn.Status == VpnStatus.Connected)
                {
                    Task.Run(async delegate { await DiContainer.Resolve<INodeSettings>().UpdateFeatures(); });
                }

                //OnAppSettingsChanged(value);
                this.AppSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public RegionConnectionSettings LastUsedRegion { get; internal set; }

        internal static int ConnectAnimationDelayTime { get; set; } = 1000;


        [Routing("productInfo")]
        public JObject ProductInfo => new JObject
        {
            {
                "DeviceId",
                (JToken)(DiContainer.Resolve<IApplicationIds>()?.DeviceId)
            },
            {
                "ProductID",
                (JToken)VpnSettings.ProductId.ToString()
            },
            {
                "ProductVersion",
                (JToken)DiContainer.Resolve<IProductSettings>().ProductVersion
            },
            {
                "ProductLanguage",
                (JToken)DiContainer.Resolve<IProductSettings>().ProductLanguage
            },
            {
                "PlatformType",
                (JToken)DiContainer.Resolve<IProductSettings>().OsType
            },
            {
                "PlatformVersion",
                (JToken)DiContainer.Resolve<IProductSettings>().GetOsVersion().ToString()
            },
            {
                "BundleID",
                (JToken)""
            }
        };

        internal int DisconnectTimerDelayTime { get; set; } = 1000;


        [Routing("features/get", true)] public event EventHandler<EventArgs<JObject>> FeaturesChanged;

        [Routing("userProfileChanged")] public event EventHandler<EventArgs<UserProfile>> UserProfileChanged;

        [Routing("status")] public event EventHandler<EventArgs<JObject>> StatusChanged;

        [Routing("ipAddressRefreshed")] public event EventHandler<EventArgs<IPData>> IpAddressRefreshed;

        [Routing("keychainAccessGranted")] public event EventHandler<EventArgs<JObject>> KeychainAccessGrantedResult;

        [Routing("uiVisible")] public event EventHandler<EventArgs<JObject>> UiVisiblilityChanged;

        [Routing("appSettingsSubscription")] public event EventHandler AppSettingsChanged;

        [Routing("trafficLimitReached")] public event EventHandler<EventArgs<TrafficData>> TrafficLimitReached;

        [Routing("disconnectTimer")] public event EventHandler<DisconnectTimerEventArgs> OnDisconnectTimerChanged;

        [Routing("connectionReestablished")] public event EventHandler<EventArgs<JObject>> ConnectionReestablished;

        [Routing("login")] public event EventHandler<EventArgs<JObject>> LoginResponse;

        public VpnController(IVpnConnector vpnConnector)
            : this(DiContainer.Resolve<IAuthenticator>(),
                new VpnBackendApiClient<RegionList>(VpnSettings.VpnApiUrl, "regions", "regions"),
                new VpnBackendApiClient<TrafficData>(VpnSettings.VpnApiUrl, "traffic", "API_Traffic"), vpnConnector)
        {
        }

        public VpnController(IAuthenticator authenticator, IApiClient<RegionList> regionsClient,
            IApiClient<TrafficData> trafficClient, IVpnConnector vpnConnector)
        {
            if ((object)regionsClient.GetType() == typeof(VpnBackendApiClient<RegionList>))
            {
                this.regionsClient = (VpnBackendApiClient<RegionList>)regionsClient;
            }

            if ((object)trafficClient.GetType() == typeof(VpnBackendApiClient<TrafficData>))
            {
                this.trafficClient = (VpnBackendApiClient<TrafficData>)trafficClient;
            }

            Regions = new Regions(regionsClient);
            DiContainer.SetInstance<Regions>(Regions);
            vpn = vpnConnector;
            this.authenticator = authenticator;
            DiContainer.SetInstance<IAuthenticator>(this.authenticator);
            trafficClient.MultipleApiUpdateOnReconnect = false;
            traffic = new Traffic(trafficClient);
            DiContainer.SetInstance<ITraffic>(traffic);
            traffic.TrafficChanged += TrafficChanged;
            loginHandler = DiContainer.Resolve<IUserManagement>();
            vpn.StatusChanged += StatusChangedEventHandler;
            if (device.IsSandboxed())
            {
                connectionMonitor = new ConnectionMonitor(vpn);
                connectionMonitor.ConnectionError += ConnectionErrorHandler;
            }

            remoteConfig = new RemoteConfiguration(
                new VpnBackendApiClient<RemoteConfigurationData>(VpnSettings.VpnApiUrl,
                    "features?device_id=" + DiContainer.Resolve<IApplicationIds>()?.ClientId, "remote_features"),
                GenerateFeaturesPayload, TimeSpan.FromHours(24.0));
            remoteConfig.Refresh().CatchAll();
            remoteConfig.ConfigurationChanged += FeaturesRefreshedHandler;
            DiContainer.SetInstance<IRemoteConfiguration>(remoteConfig);
            features = new Features(remoteConfig);
            DiContainer.SetInstance<IFeatures>(features);
            ipChecker = new IPChecker(new VpnBackendApiClient<IPData>(VpnSettings.VpnApiUrl, "whoami", "whoami"));
            ipChecker.IPRefreshed += delegate
            {
                this.IpAddressRefreshed?.Invoke(null, new EventArgs<IPData>(ipChecker.Data));
            };
            if (DiContainer.Resolve<IAppSettings>().Get().AppImprovement)
            {
                experimentTracker = new ExperimentTracker();
            }

            Initialize();
        }

        private JObject GenerateFeaturesPayload()
        {
            return new JObject
            {
                ["version"] = (JToken)DiContainer.Resolve<IProductSettings>().ProductVersion.ToString(),
                ["language"] = (JToken)DiContainer.Resolve<IProductSettings>().ProductLanguage,
                ["diagnosticDataEnabled"] = (JToken)DiContainer.Resolve<IAppSettings>().Get().AppImprovement.ToString()
            };
        }

        private void UpdateFastFeedbackContent(List<RemoteFeatureData> remoteFeatures)
        {
            RemoteFeatureData remoteFeatureData = remoteFeatures.Find((RemoteFeatureData f) => f.Id == "fast_feedback");
            if (remoteFeatureData != null && remoteFeatureData.IsActive)
            {
                DiContainer.Resolve<FastFeedback>().Update(remoteFeatureData.Params.ToObject<FastFeedbackData>());
            }
        }

        private void FeaturesRefreshedHandler(object sender, EventArgs e)
        {
            this.FeaturesChanged?.Invoke(this, new EventArgs<JObject>(DiContainer.Resolve<IFeatures>().Serialize()));
            UpdateFastFeedbackContent(remoteConfig.RemoteFeatures);
        }

        [Routing("status")]
        public JObject GetStatus()
        {
            return JsonConvert.DeserializeObject<JObject>("{\"status\":\"" + vpn.Status.ToString() +
                                                          "\",\"message\":\"Message\"}");
        }

        [Routing("refreshIPAddress")]
        public void RefreshIpAddress()
        {
            ipChecker.Refresh().CatchAll();
        }

        [Routing("trackEvent")]
        public void TrackEvent(JObject trackingEvent)
        {
            string eventName = trackingEvent["eventId"]!.ToString();
            Dictionary<string, string> properties = trackingEvent["properties"]?.ToObject<Dictionary<string, string>>();
            Tracker.TrackEvent(eventName, properties);
        }

        private void TrackEvent(string eventId)
        {
            JObject trackingEvent = new JObject { ["eventId"] = (JToken)eventId };
            TrackEvent(trackingEvent);
        }

        [Routing("storageGet")]
        public JObject StorageGet(string key)
        {
            return JsonConvert.DeserializeObject<JObject>(settings.Get(key)) ?? new JObject();
        }

        [Routing("storageSet")]
        public void StorageSet(JObject data)
        {
            settings.Set(data["key"]!.ToString(), JsonConvert.SerializeObject(data["value"]));
        }

        [Routing("storageRemove")]
        public void StorageRemove(string key)
        {
            settings.Set(key, string.Empty);
        }

        [Routing("keychainAccessGranted")]
        public void KeychainAccessGranted()
        {
            CheckIfYourGrantedAccessToKeychain().CatchAll();
        }

        [Routing("connect")]
        public void Connect(RegionConnectionSettings region)
        {
            ConnectAsync(region).CatchAll();
        }

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

        private bool CheckIfSettingChanged(bool newSetting, bool oldSetting, string eventName)
        {
            bool result = false;
            if (oldSetting != newSetting)
            {
                TrackSettingChanged(eventName, newSetting);
                result = true;
            }

            return result;
        }

        private void TrackSettingChanged(string eventName, bool enabled)
        {
            Tracker.TrackEvent(eventName, new Dictionary<string, string>
            {
                {
                    "Enabled",
                    enabled.ToString()
                }
            });
        }

        public async Task ReconnectToLastUsedRegion()
        {
            await ConnectAsync(LastUsedRegion);
        }

        public async Task ConnectAsync(RegionConnectionSettings region)
        {
            _ = 3;
            try
            {
                disconnectRequested = false;
                UpdateConnectionStatus(VpnStatus.Connecting);
                IInternetAvailabilityMonitor internetAvailabilityMonitor =
                    DiContainer.Resolve<IInternetAvailabilityMonitor>();
                if (internetAvailabilityMonitor != null && !internetAvailabilityMonitor.IsInternetAvailable)
                {
                    await Task.Delay(ConnectAnimationDelayTime);
                    UpdateConnectionStatus(VpnStatus.Disconnected);
                    NotifyConnectionError(VpnError.NoNetworkAvailable);
                    return;
                }

                if (await IsTrafficLimitReachedAsync())
                {
                    UpdateConnectionStatus(VpnStatus.Disconnected);
                    return;
                }

                LastUsedRegion = region;
                if (device.IsSandboxed())
                {
                    await InitConnectionMonitor();
                }

                disconnectCountdownStarted = false;
                Credentials credentials = CreateUserCredentials();
                Tracker.TrackConnect(region);
                await vpn.StartConnectAsync(region, credentials);
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus(VpnStatus.Disconnected);
                if (!disconnectRequested)
                {
                    NotifyConnectionError(VpnError.ConnectionFailed);
                    Tracker.TrackConnectError(ex, region);
                    Log.Error(ex, "Failed to connect VPN.");
                }
            }
        }

        private async Task InitConnectionMonitor()
        {
            this.connectionMonitor.ConnectRequested = true;
            ConnectionMonitor connectionMonitor = this.connectionMonitor;
            connectionMonitor.IPSecPortsAreOpen = await udpPortScanner.AreIPSecPortsOpen();
        }

        private void UpdateConnectionStatus(VpnStatus vpnStatus)
        {
            JObject value =
                JsonConvert.DeserializeObject<JObject>($"{{\"status\":\"{vpnStatus}\",\"message\":\"Message\"}}");
            this.StatusChanged?.Invoke(this, new EventArgs<JObject>(value));
        }

        private async Task<bool> IsTrafficLimitReachedAsync()
        {
            await Traffic.Refresh();
            return traffic.LimitReached();
        }

        private void ConnectionErrorHandler(object sender, EventArgs<VpnError> e)
        {
            Tracker.TrackEvent(Tracker.Events.ConnectionError, new Dictionary<string, string>
            {
                {
                    "Error",
                    e.Value.ToString()
                }
            });
            NotifyConnectionError(e.Value);
        }

        private void NotifyConnectionError(VpnError connectionError)
        {
            JObject value =
                JsonConvert.DeserializeObject<JObject>($"{{\"error\":{(int)connectionError},\"message\":\"Message\"}}");
            this.StatusChanged?.Invoke(this, new EventArgs<JObject>(value));
        }

        private Credentials CreateUserCredentials()
        {
            string text = DiContainer.Resolve<IApplicationIds>()?.ClientId;
            string text2 = authenticator?.AccessToken;
            string password = (string.IsNullOrEmpty(text2)
                ? text
                : (JwtToken.IsAnonymousToken(text2) ? text : authenticator?.AccessTokenHash));
            return new Credentials
            {
                UserName = text,
                Password = password
            };
        }

        [Routing("registerUser")]
        public void RegisterUser()
        {
            TrackEvent(Tracker.Events.RegistrationClicked);
            loginHandler.StartLogin().CatchAll();
        }

        [Routing("openDashboard")]
        public void OpenDashboard()
        {
            TrackEvent(Tracker.Events.OpenDashboard);
            loginHandler.OpenDashboard().CatchAll();
        }

        [Routing("upgrade")]
        public void StartUpgrade()
        {
        }

        private void StatusChangedEventHandler(object sender, EventArgs e)
        {
            if (device.IsSandboxed())
            {
                switch (vpn.Status)
                {
                    case VpnStatus.Connected:
                        traffic.StartPeriodicRefresh();
                        break;
                    case VpnStatus.Disconnected:
                        traffic.StopPeriodicRefresh();
                        break;
                }
            }

            if (vpn.Status == VpnStatus.Connected)
            {
                settings.Set(SettingsKeys.LastConnectName, JsonConvert.SerializeObject(DateTime.Now));
                UpdateNodeSettings();
            }

            if (VpnStatus.Disconnected == vpn.Status)
            {
                Traffic.Refresh().CatchAll();
            }

            this.StatusChanged?.Invoke(sender, new EventArgs<JObject>(GetStatus()));
        }

        [Routing("themeColor")]
        public void ThemeColor(string theme)
        {
            settings.Set("theme_color", theme);
        }

        [Routing("displaySettings/get")]
        public JObject GetDisplaySettings()
        {
            return JObject.Parse(settings.Get("display_settings", "{\"OsSettings\" : true}"));
        }

        [Routing("displaySettings/set")]
        public void DisplaySettings(JObject settings)
        {
            this.settings.Set("display_settings", JsonConvert.SerializeObject(settings));
        }

        [Routing("themeSelection/get")]
        public JObject ThemeSelection()
        {
            return JObject.Parse(settings.Get("theme_selection", "{\"displayed\" : false}"));
        }

        [Routing("themeSelection/set")]
        public void SetThemeSelection(JObject settings)
        {
            this.settings.Set("theme_selection", JsonConvert.SerializeObject(settings));
        }

        [Routing("login")]
        public void Login(JObject credentials)
        {
            authenticator.Login(credentials).ContinueWith(delegate(Task<JObject> param)
            {
                this.LoginResponse?.Invoke(this, new EventArgs<JObject>(param.Result));
            });
        }

        private void UpdateNodeSettings()
        {
            NodeSessionInfo nodeSessionInfo = new NodeSessionInfo
            {
                SelectedRegionId = ((LastUsedRegion == null) ? "unknown" : LastUsedRegion.Id),
                Fronted = WasFrontingUsed()
            };
            Task.Run(async delegate { await DiContainer.Resolve<INodeSettings>().UpdateFeatures(nodeSessionInfo); });
        }

        private bool WasFrontingUsed()
        {
            bool flag = false;
            if (regionsClient != null)
            {
                flag |= regionsClient.UsedFronting;
            }

            if (trafficClient != null)
            {
                flag |= trafficClient.UsedFronting;
            }

            return flag;
        }

        public async Task CheckIfYourGrantedAccessToKeychain()
        {
            bool flag = await DiContainer.Resolve<IVpnConfigurator>().GetProfileAuthorizationStatusAsync() ==
                        ProfileAuthorizationStatus.Granted;
            this.KeychainAccessGrantedResult?.Invoke(this, new EventArgs<JObject>(new JObject
            {
                {
                    "granted",
                    (JToken)flag
                }
            }));
        }

        [Routing("disconnect")]
        public void Disconnect()
        {
            DisconnectAsync().CatchAll();
        }

        public async Task DisconnectAsync()
        {
            disconnectRequested = true;
            if (device.IsSandboxed())
            {
                connectionMonitor.ConnectRequested = false;
            }

            await vpn.StartDisconnectAsync();
        }

        public Task Initialize()
        {
            IUserManagement userManagement = DiContainer.Resolve<IUserManagement>();
            userManagement.TokenChanged += delegate(object s, EventArgs<string> e)
            {
                authenticator.Refresh(e.Value).CatchAll();
            };
            authenticator.AccessTokenChanged += delegate
            {
                traffic.Refresh().CatchAll();
                this.UserProfileChanged?.Invoke(this, new EventArgs<UserProfile>(UserProfileInfo ?? new UserProfile()));
            };
            return Task.WhenAll(Regions?.Refresh(), authenticator.Refresh(userManagement.Token), traffic.Refresh());
        }

        private void TrafficChanged(object sender, EventArgs<TrafficData> e)
        {
            if (traffic.LimitReached())
            {
                if (!limitReachedReported)
                {
                    Tracker.TrackLimitReached(traffic.TrafficData.UsedTraffic, traffic.TrafficData.TrafficLimit);
                    limitReachedReported = true;
                }

                this.TrafficLimitReached?.Invoke(this, e);
                if (!disconnectCountdownStarted)
                {
                }
            }
            else
            {
                limitReachedReported = false;
            }
        }

        public void Dispose()
        {
            if (traffic != null)
            {
                traffic.Dispose();
            }

            if (disconnectTimerCancellationToken != null)
            {
                disconnectTimerCancellationToken.Dispose();
            }
        }
    }
}