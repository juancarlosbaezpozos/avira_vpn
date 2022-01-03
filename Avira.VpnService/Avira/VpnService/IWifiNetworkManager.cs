using System;
using Avira.Win.Messaging;

namespace Avira.VpnService
{
    public interface IWifiNetworkManager
    {
        event EventHandler<EventArgs<KnownWifis.WiFiData>> WifiNetworkConnected;

        event EventHandler<EventArgs<KnownWifis.WiFiData>> WifiNetworkDisconnected;

        void TrustConnectedWifiNetwork();

        void UntrustConnectedWifiNetwork();

        KnownWifis.WiFiData FindWifi(string ssid);
    }
}