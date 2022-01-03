using System;

namespace Avira.VPN.Core.Win
{
    public class TrafficChangedEventArgs : EventArgs
    {
        public ulong UsedInBytes { get; set; }
    }
}