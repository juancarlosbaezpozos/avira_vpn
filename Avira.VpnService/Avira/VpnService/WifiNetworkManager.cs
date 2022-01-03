using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Avira.Common.Core;
using Avira.Common.Core.Networking;
using Avira.VPN.Core.Win;
using Avira.Win.Messaging;
using Serilog;

namespace Avira.VpnService
{
    public sealed class WifiNetworkManager : IDisposable, IWifiNetworkManager
    {
        internal static class NativeMethods
        {
            [DllImport("iphlpapi.dll", SetLastError = true)]
            public static extern int GetBestInterface(uint DestAddr, out uint BestIfIndex);
        }

        private readonly IWifiNetworkMonitor wifiNetworkMonitor;

        private readonly IInternetConnectionMonitor internetConnectionMonitor;

        private readonly object internetCheckerLock = new object();

        private readonly IServicePersistentData persistentData;

        private readonly IDnsWrapper dnsWrapper;

        private readonly object mutex = new object();

        private Timer internetCheckTimer;

        private long internetCheckerStartTimeInSec;

        private WifiConnectionEventArgs connectedWifiConnectionArgs;

        private string connectedWifiId = string.Empty;

        internal static long InternetCheckIntervalInMiliseconds { get; set; } = 5000L;


        internal static long InternetCheckMaxCheckTimeInMiliseconds { get; set; } = 300000L;


        public KnownWifis Wifis
        {
            get
            {
                lock (mutex)
                {
                    return ReadKnownWifis();
                }
            }
        }

        public string ConnectedWifi
        {
            get
            {
                WifiConnectionEventArgs connectedWifi = wifiNetworkMonitor.GetConnectedWifi();
                if (connectedWifi == null || connectedWifi == null || connectedWifi.Status != 0)
                {
                    return string.Empty;
                }

                return GetId(connectedWifi.Ssid);
            }
        }

        public event EventHandler<EventArgs<KnownWifis.WiFiData>> WifiNetworkConnected;

        public event EventHandler<EventArgs<KnownWifis.WiFiData>> WifiNetworkDisconnected;

        public event EventHandler WifiListChanged;

        public WifiNetworkManager(IWifiNetworkMonitor wifiMonitor)
            : this(wifiMonitor, new InternetConnectionMonitor(), new ServicePersistentData(), new DnsWrapper())
        {
        }

        internal WifiNetworkManager(IWifiNetworkMonitor wifiNetworkMonitor,
            IInternetConnectionMonitor internetConnectionMonitor, IServicePersistentData persistentData,
            IDnsWrapper dnsWrapper)
        {
            this.wifiNetworkMonitor = wifiNetworkMonitor;
            this.internetConnectionMonitor = internetConnectionMonitor;
            this.persistentData = persistentData;
            this.dnsWrapper = dnsWrapper;
            WifiConnectionEventArgs connectedWifi = this.wifiNetworkMonitor.GetConnectedWifi();
            if (connectedWifi != null)
            {
                UpdateWifiConnectionTime(connectedWifiId = GetId(connectedWifi.Ssid));
            }

            this.wifiNetworkMonitor.StatusChanged += WifiNetworkMonitorOnStatusChanged;
        }

        public void TrustById(string id)
        {
            lock (mutex)
            {
                KnownWifis knownWifis = ReadKnownWifis();
                knownWifis.Trust(id);
                WriteKnownWiFis(knownWifis);
            }
        }

        public void DeleteById(string id)
        {
            lock (mutex)
            {
                KnownWifis knownWifis = ReadKnownWifis();
                int index = knownWifis.FindIndex((KnownWifis.WiFiData item) => item.Id == id);
                knownWifis.RemoveAt(index);
                WriteKnownWiFis(knownWifis);
            }
        }

        public void TrustConnectedWifiNetwork()
        {
            if (!string.IsNullOrEmpty(connectedWifiConnectionArgs?.Ssid))
            {
                string id = GetId(connectedWifiConnectionArgs?.Ssid);
                Serilog.Log.Information("Trusting Wifi Network: " + connectedWifiConnectionArgs?.Ssid + " (" + id +
                                        ")");
                TrustById(id);
            }
        }

        public void UntrustById(string id)
        {
            lock (mutex)
            {
                KnownWifis knownWifis = ReadKnownWifis();
                knownWifis.Untrust(id);
                WriteKnownWiFis(knownWifis);
            }
        }

        public void UntrustConnectedWifiNetwork()
        {
            if (!string.IsNullOrEmpty(connectedWifiConnectionArgs?.Ssid))
            {
                string id = GetId(connectedWifiConnectionArgs?.Ssid);
                Serilog.Log.Information("Untrusting Wifi Network: " + connectedWifiConnectionArgs?.Ssid + " (" + id +
                                        ")");
                UntrustById(id);
            }
        }

        public bool IsKnownWifi(string id)
        {
            return FindWifi(id) != null;
        }

        public KnownWifis.WiFiData FindWifi(string id)
        {
            lock (mutex)
            {
                return ReadKnownWifis()?.Find((KnownWifis.WiFiData d) => d.Id == id);
            }
        }

        private string GetId(string ssid)
        {
            lock (mutex)
            {
                string id = wifiNetworkMonitor.GetProfileUniqueId(ssid);
                KnownWifis knownWifis = ReadKnownWifis();
                KnownWifis.WiFiData wiFiData = (string.IsNullOrEmpty(id)
                    ? knownWifis.Find((KnownWifis.WiFiData d) => d.Ssid == ssid)
                    : knownWifis.Find((KnownWifis.WiFiData d) => d.Id == id));
                if (wiFiData == null)
                {
                    wiFiData = knownWifis.Find((KnownWifis.WiFiData d) => d.Ssid == ssid);
                    if (wiFiData == null)
                    {
                        WifiConnectionSecurityMode securityMode = ((connectedWifiConnectionArgs != null)
                            ? connectedWifiConnectionArgs.SecurityMode
                            : WifiConnectionSecurityMode.Unknown);
                        wiFiData = new KnownWifis.WiFiData
                        {
                            Id = id,
                            Ssid = ssid,
                            SecurityMode = securityMode
                        };
                        knownWifis.Add(wiFiData);
                    }
                    else
                    {
                        wiFiData.Id = id;
                    }

                    WriteKnownWiFis(knownWifis);
                }

                return id;
            }
        }

        private KnownWifis ReadKnownWifis()
        {
            return persistentData.KnownWiFis ?? new KnownWifis();
        }

        private void WriteKnownWiFis(KnownWifis knownWiFis)
        {
            persistentData.KnownWiFis = knownWiFis;
            this.WifiListChanged?.Invoke(this, EventArgs.Empty);
        }

        private bool IsWifiNeworkTrusted(string id)
        {
            KnownWifis.WiFiData wiFiData = FindWifi(id);
            if (wiFiData != null)
            {
                return wiFiData.TrustMode == TrustMode.Trusted;
            }

            return false;
        }

        private NetworkInterface GetNetworkInterfaceByIndex(uint index)
        {
            NetworkInterface networkInterface = (from thisInterface in NetworkInterface.GetAllNetworkInterfaces()
                where thisInterface.Supports(NetworkInterfaceComponent.IPv4)
                let ipv4Properties = thisInterface.GetIPProperties().GetIPv4Properties()
                where ipv4Properties != null && ipv4Properties.Index == index
                select thisInterface).SingleOrDefault();
            if (networkInterface != null)
            {
                return networkInterface;
            }

            return (from thisInterface in NetworkInterface.GetAllNetworkInterfaces()
                where thisInterface.Supports(NetworkInterfaceComponent.IPv6)
                let ipv6Properties = thisInterface.GetIPProperties().GetIPv6Properties()
                where ipv6Properties != null && ipv6Properties.Index == index
                select thisInterface).SingleOrDefault();
        }

        private bool IsMainInterfaceWifi()
        {
            try
            {
                IPAddress iPAddress = dnsWrapper.GetHostEntry("dns.msftncsi.com").AddressList
                    .Where((IPAddress thisAddress) => thisAddress.AddressFamily == AddressFamily.InterNetwork)
                    .FirstOrDefault();
                if (iPAddress == null)
                {
                    Serilog.Log.Debug("No IPV4 found for dns.msftncsi.com.");
                    return true;
                }

                uint BestIfIndex;
                int bestInterface =
                    NativeMethods.GetBestInterface(BitConverter.ToUInt32(iPAddress.GetAddressBytes(), 0),
                        out BestIfIndex);
                if (bestInterface != 0)
                {
                    Serilog.Log.Debug($"Could not find best interface for IPV4 {iPAddress}. Error {bestInterface}.");
                    return true;
                }

                return GetNetworkInterfaceByIndex(BestIfIndex).NetworkInterfaceType ==
                       NetworkInterfaceType.Wireless80211;
            }
            catch (SocketException)
            {
                Serilog.Log.Debug("DNS server did not respond or the host was not found.");
                return true;
            }
            catch (Exception exception)
            {
                Serilog.Log.Warning(exception, "Failed to check if main interface is Wifi.");
                return true;
            }
        }

        private void WifiNetworkMonitorOnStatusChanged(object sender, WifiConnectionEventArgs wifiConnectionEventArgs)
        {
            if (wifiConnectionEventArgs.Status != 0)
            {
                Serilog.Log.Debug("Disconnected from WiFi network. Stop polling for internet connectivity.");
                StopInternetChecker();
                EventArgs<KnownWifis.WiFiData> e = new EventArgs<KnownWifis.WiFiData>(FindWifi(connectedWifiId));
                this.WifiNetworkDisconnected?.Invoke(this, e);
                return;
            }

            if (!IsMainInterfaceWifi())
            {
                Serilog.Log.Debug("Connected Wifi network " + wifiConnectionEventArgs.Ssid +
                                  " is not the main network interface. Not popping up the Notifier.");
                return;
            }

            connectedWifiConnectionArgs = wifiConnectionEventArgs;
            connectedWifiId = GetId(wifiConnectionEventArgs.Ssid);
            UpdateWifiConnectionTime(connectedWifiId);
            if (IsWifiNeworkTrusted(connectedWifiId))
            {
                Serilog.Log.Information("Connected to a known Wifi Network. " + connectedWifiConnectionArgs?.Ssid +
                                        " (" + connectedWifiId + ")");
            }
            else
            {
                Serilog.Log.Debug(
                    "Connected to an unknown Wifi without internet connetion.Start polling for internet connectivity.");
                StartInternetChecker();
            }
        }

        private void NotifyConnectionEstablish()
        {
            EventArgs<KnownWifis.WiFiData> e = new EventArgs<KnownWifis.WiFiData>(FindWifi(connectedWifiId));
            this.WifiNetworkConnected?.Invoke(this, e);
        }

        private void UpdateWifiConnectionTime(string id)
        {
            lock (mutex)
            {
                KnownWifis knownWifis = ReadKnownWifis();
                knownWifis.UpdateConnectionTime(id, DateTime.Now);
                WriteKnownWiFis(knownWifis);
            }
        }

        private void StartInternetChecker()
        {
            if (internetConnectionMonitor.CurrentStatus == InternetConnectionStatus.Connected)
            {
                InternetStatusChangedEventHandler(null, EventArgs.Empty);
                return;
            }

            internetConnectionMonitor.StatusChanged -= InternetStatusChangedEventHandler;
            internetConnectionMonitor.StatusChanged += InternetStatusChangedEventHandler;
            internetCheckerStartTimeInSec = DateTime.Now.ToUnixTimeStamp();
            lock (internetCheckerLock)
            {
                internetCheckTimer?.Dispose();
                internetCheckTimer = new Timer(InternetChecker(), null, 0L, InternetCheckIntervalInMiliseconds);
            }
        }

        private TimerCallback InternetChecker()
        {
            return delegate
            {
                if (internetCheckerStartTimeInSec * 1000 + InternetCheckMaxCheckTimeInMiliseconds <
                    DateTime.Now.ToUnixTimeStamp() * 1000)
                {
                    Serilog.Log.Debug("Timeout checking the internet connectivity. Stop polling for internet access.");
                    StopInternetChecker();
                }
                else
                {
                    Serilog.Log.Debug("Checking internet connectivity...");
                    internetConnectionMonitor.InitializeAsync();
                }
            };
        }

        private void StopInternetChecker()
        {
            internetConnectionMonitor.StatusChanged -= InternetStatusChangedEventHandler;
            lock (internetCheckerLock)
            {
                if (internetCheckTimer != null)
                {
                    internetCheckTimer.Dispose();
                }
            }
        }

        public void TriggerAutoconnect()
        {
            if (internetConnectionMonitor.CurrentStatus == InternetConnectionStatus.Connected)
            {
                EventArgs<KnownWifis.WiFiData>
                    eventArgs = new EventArgs<KnownWifis.WiFiData>(FindWifi(connectedWifiId));
                KnownWifis.WiFiData value = eventArgs.Value;
                if (value != null && value.TrustMode == TrustMode.Untrusted)
                {
                    this.WifiNetworkConnected?.Invoke(this, eventArgs);
                }
            }
        }

        private void InternetStatusChangedEventHandler(object sender, EventArgs eventArgs)
        {
            if (internetConnectionMonitor.CurrentStatus != InternetConnectionStatus.Connected)
            {
                Serilog.Log.Debug("Internet access not available.");
                return;
            }

            Serilog.Log.Debug("Internet access is available. Stop polling for internet access.");
            NotifyConnectionEstablish();
            StopInternetChecker();
        }

        public void Dispose()
        {
            if (internetCheckTimer != null)
            {
                internetCheckTimer.Dispose();
            }
        }
    }
}