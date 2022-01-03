using System;

namespace Avira.VPN.Core
{
    public interface IInternetAvailabilityMonitor
    {
        bool IsInternetAvailable { get; }

        event EventHandler InternetConnected;
    }
}