using System;

namespace Avira.Common.Core.Networking
{
    public interface IWifiNetworkMonitor
    {
        event EventHandler<WifiConnectionEventArgs> StatusChanged;

        WifiConnectionEventArgs GetConnectedWifi();

        string GetProfileUniqueId(string profileName);
    }
}