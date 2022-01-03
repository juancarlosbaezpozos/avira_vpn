using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Newtonsoft.Json;
using Serilog;

namespace Avira.VpnService
{
    internal sealed class Regions : IDisposable
    {
        private const long DefaultErrorFallbackIntervalInSec = 20L;

        private readonly string productLanguage;

        private readonly IHttpClient vpnBackendHttpClient;

        private readonly INetworkChangeMonitor networkChangeMonitor;

        private readonly ServicePersistentData persistentData;

        private readonly string deviceId;

        private Timer regionsRequestTimer;

        private RegionList regionList;

        private RemoteConnectionSettings nearestRegionWhenDisconnected;

        private int guard;

        private bool fallBackRegionListInUse;

        internal long RegionsUpdateIntervalInSeconds { get; private set; }

        private long ErrorFallbackTimerIntervalInSec { get; }

        public RegionList RegionList
        {
            get
            {
                if (regionList == null)
                {
                    RequestRegionsAndStartTtlTimer();
                }

                return regionList;
            }
        }

        public event EventHandler RegionsListUpdated;

        public Regions(IHttpClient vpnBackendHttpClient, string deviceId)
            : this(vpnBackendHttpClient, new NetworkChangeMonitor(), ProductSettings.SecureStorage, 20L, deviceId)
        {
        }

        internal Regions(IHttpClient vpnBackendHttpClient, IStorage storage)
            : this(vpnBackendHttpClient, new NetworkChangeMonitor(), storage, 20L, GeneratedDeviceInfo.GetClientId())
        {
        }

        internal Regions(IHttpClient vpnBackendHttpClient, INetworkChangeMonitor networkChangeMonitor, IStorage storage,
            long errorFallbackIntervalInSec, string deviceId)
        {
            persistentData = new ServicePersistentData(storage);
            productLanguage = ProductSettings.ProductLanguage;
            ErrorFallbackTimerIntervalInSec = errorFallbackIntervalInSec;
            this.vpnBackendHttpClient = vpnBackendHttpClient;
            this.deviceId = deviceId;
            ThreadPool.QueueUserWorkItem(delegate { RequestRegionsAndStartTtlTimer(); });
            this.networkChangeMonitor = networkChangeMonitor;
            this.networkChangeMonitor.NetworkConnected += OnNetworkConnected;
            this.networkChangeMonitor.Start();
        }

        public void ForceUpdateRegions()
        {
            RequestRegionsAndStartTtlTimer();
        }

        public void ClearCache()
        {
            persistentData.Regions = string.Empty;
        }

        private static RegionList CreateEmptyRegionList()
        {
            return new RegionList
            {
                ServersConnectionSettings = new List<RemoteConnectionSettings>(),
                ErrorMessage = "No network connection available."
            };
        }

        private RegionList CreateFallbackRegionList()
        {
            fallBackRegionListInUse = true;
            Log.Warning("Fallback region list used.");
            return new RegionList
            {
                DefaultRegion = "nl",
                ServersConnectionSettings = new List<RemoteConnectionSettings>
                {
                    remoteSettings("de1.phantom.avira-vpn.com", "de", "Germany", "paid"),
                    remoteSettings("nl1.phantom.avira-vpn.com", "nl", "Netherlands", "paid"),
                    remoteSettings("us-sfo.phantom.avira-vpn.com", "us_sfo", "US - San Francisco", "paid"),
                    remoteSettings("us-was.phantom.avira-vpn.com", "us_was", "US - Washington, D.C.", "paid")
                }
            };

            static RemoteConnectionSettings remoteSettings(string host, string id, string name, string licenseType)
            {
                return new RemoteConnectionSettings
                {
                    Uri = host,
                    Id = id,
                    LicenseType = licenseType,
                    Name = name,
                    Port = 443,
                    Protocol = "tcp"
                };
            }
        }

        private async void OnNetworkConnected(object sender, EventArgs eventArgs)
        {
            Log.Information("Network connection is available. Checking if there are available regions.");
            if ((regionList == null || !regionList.ServersConnectionSettings.Any() || fallBackRegionListInUse) &&
                await HttpAsyncHelper.WaitForInternetConnection(2000))
            {
                RequestRegionsAndStartTtlTimer();
            }
        }

        private void RequestRegionsAndStartTtlTimer()
        {
            Log.Information("Refreshing regions.");
            try
            {
                RequestRegions();
                if (regionList != null && regionList.Ttl > 0.0)
                {
                    Log.Information($"Starting regions updater. TTL: {regionList.Ttl}");
                    StartTimer((long)regionList.Ttl);
                }
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to request the regions and to start the TTL timer.");
            }

            if (regionList == null)
            {
                try
                {
                    regionList = ((persistentData.Regions != string.Empty)
                        ? JsonConvert.DeserializeObject<RegionList>(persistentData.Regions)
                        : CreateFallbackRegionList());
                }
                catch (Exception exception2)
                {
                    Log.Error(exception2, "Invalid Json format in regions list.");
                    regionList = CreateEmptyRegionList();
                }

                StartErrorFallbackTimerIfNetworkAvailable();
            }
        }

        private void StartErrorFallbackTimerIfNetworkAvailable()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Log.Information("Regions list error fallback was successful.");
                StartTimer(ErrorFallbackTimerIntervalInSec);
            }
        }

        private void StartTimer(long intervalInSeconds)
        {
            if (intervalInSeconds != 0L)
            {
                RegionsUpdateIntervalInSeconds = intervalInSeconds;
                regionsRequestTimer?.Dispose();
                regionsRequestTimer = new Timer(RegionsUpdater(), null, RegionsUpdateIntervalInSeconds * 1000,
                    RegionsUpdateIntervalInSeconds * 1000);
            }
        }

        private TimerCallback RegionsUpdater()
        {
            return delegate
            {
                if (Interlocked.CompareExchange(ref guard, 1, 0) != 1)
                {
                    try
                    {
                        RequestRegions();
                        if (regionList.Ttl > 0.0)
                        {
                            UpdateTimerInterval((long)regionList.Ttl);
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Warning(exception, "Failed to request the regions after TTL expired.");
                        regionList = null;
                    }

                    Interlocked.Exchange(ref guard, 0);
                }
            };
        }

        private void UpdateTimerInterval(long intervalInSeconds)
        {
            RegionsUpdateIntervalInSeconds = intervalInSeconds;
            regionsRequestTimer.Change(RegionsUpdateIntervalInSeconds * 1000, RegionsUpdateIntervalInSeconds * 1000);
        }

        private void RequestRegions()
        {
            string text = vpnBackendHttpClient.Get("regions?device_id=" + deviceId + "&lang=" + productLanguage);
            if (!string.IsNullOrEmpty(text))
            {
                regionList = JsonConvert.DeserializeObject<RegionList>(text);
                UpdateNearestRegion(regionList);
                fallBackRegionListInUse = false;
                persistentData.Regions = text;
                Log.Information("Regions were successfuly refreshed.");
                this.RegionsListUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateNearestRegion(RegionList regionList)
        {
            RemoteConnectionSettings remoteConnectionSettings = regionList.ServersConnectionSettings
                .Where((RemoteConnectionSettings s) => s.Id == "nearest").FirstOrDefault();
            if (remoteConnectionSettings != null)
            {
                IVpnProvider vpnProvider = DiContainer.Resolve<IVpnProvider>();
                if ((vpnProvider != null && vpnProvider.ConnectionState == ConnectionState.Disconnected) ||
                    vpnProvider == null)
                {
                    nearestRegionWhenDisconnected = remoteConnectionSettings;
                }
                else
                {
                    remoteConnectionSettings.Uri = nearestRegionWhenDisconnected?.Uri ?? remoteConnectionSettings.Uri;
                }
            }
        }

        public void Dispose()
        {
            if (regionsRequestTimer != null)
            {
                regionsRequestTimer.Dispose();
            }
        }
    }
}