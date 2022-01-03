using System;
using Avira.Common.Core.Networking;
using Avira.VPN.Core;
using Avira.Win.Messaging;
using Serilog;

namespace Avira.VpnService
{
    public class WifiManager
    {
        private IWifiNetworkManager wifiNetworkManager;

        private IVpnNotifier vpnNotifier;

        internal EventHandler<EventArgs> WifiAutoconnected;

        internal EventHandler<EventArgs> AutoconnectedWifiDisconnected;

        private string AutoconnectId { get; set; }

        public string AutoconnectSsid { get; private set; }

        public WifiManager(IWifiNetworkManager wifiNetworkManager, IVpnNotifier vpnNotifier)
        {
            this.wifiNetworkManager = wifiNetworkManager;
            this.vpnNotifier = vpnNotifier;
            this.wifiNetworkManager.WifiNetworkConnected += ConnectedToWifi;
            this.wifiNetworkManager.WifiNetworkDisconnected += delegate(object s, EventArgs<KnownWifis.WiFiData> a)
            {
                if (a.Value != null)
                {
                    if (AutoconnectId == a.Value.Id)
                    {
                        Log.Debug("WifiManager.OnWifiNetworkDisconnected: requested disconnect for " + a.Value.Ssid +
                                  " " + a.Value.Id);
                        AutoconnectedWifiDisconnected?.Invoke(this, EventArgs.Empty);
                    }

                    ClearAutoconnectIds();
                }
            };
        }

        internal void ClearAutoconnectIds()
        {
            AutoconnectId = string.Empty;
            AutoconnectSsid = string.Empty;
        }

        internal void ConnectedToWifi(object sender, EventArgs<KnownWifis.WiFiData> eventArgs)
        {
            Log.Information("Connected to an unknown WiFi network.");
            AutoconnectId = string.Empty;
            AutoconnectSsid = string.Empty;
            KnownWifis.WiFiData value = eventArgs.Value;
            if (value == null || value.TrustMode != TrustMode.Trusted)
            {
                if (value != null && value.TrustMode == TrustMode.Untrusted)
                {
                    Log.Debug("WifiManager.ConnectedToUnkownWifiNetworkHandler: requested connect for " + value.Ssid +
                              " " + value.Id);
                    WifiAutoconnected?.Invoke(this, EventArgs.Empty);
                    Tracker.TrackEvent(Tracker.Events.Autoconnect);
                    AutoconnectId = value.Id;
                    AutoconnectSsid = value.Ssid;
                }
                else if (eventArgs.Value.SecurityMode == WifiConnectionSecurityMode.Unsecure)
                {
                    vpnNotifier?.NotifyConnectedToUnsecureWifi();
                }
                else
                {
                    vpnNotifier?.NotifyConnectedToUnkownWifi();
                }
            }
        }
    }
}