using System;

namespace Avira.Common.Core.Networking
{
    public class WifiConnectionEventArgs : EventArgs
    {
        public WifiConnectionSecurityMode SecurityMode { get; private set; }

        public WifiConnectionStatus Status { get; private set; }

        public string Ssid { get; private set; }

        public WifiConnectionEventArgs(string ssid, WifiConnectionSecurityMode connectionMode,
            WifiConnectionStatus connectionStatus)
        {
            Ssid = ssid;
            SecurityMode = connectionMode;
            Status = connectionStatus;
        }
    }
}