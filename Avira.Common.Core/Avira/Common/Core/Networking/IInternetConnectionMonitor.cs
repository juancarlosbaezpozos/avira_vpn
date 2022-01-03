using System;

namespace Avira.Common.Core.Networking
{
    public interface IInternetConnectionMonitor
    {
        InternetConnectionStatus LastKnownStatus { get; }

        InternetConnectionStatus CurrentStatus { get; }

        event EventHandler StatusChanged;

        void InitializeAsync();
    }
}