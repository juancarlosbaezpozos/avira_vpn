using System;

namespace Avira.VPN.Core.Win
{
    public interface INetworkChangeMonitor : IDisposable
    {
        event EventHandler NetworkConnected;

        event EventHandler NetworkDisconnected;

        void Start();
    }
}