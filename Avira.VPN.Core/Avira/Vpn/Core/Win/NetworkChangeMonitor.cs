using System;
using System.Net.NetworkInformation;

namespace Avira.VPN.Core.Win
{
    public sealed class NetworkChangeMonitor : INetworkChangeMonitor, IDisposable
    {
        private NetworkAvailabilityChangedEventHandler networkAvailabilityChangedEventHandler;

        public event EventHandler NetworkConnected;

        public event EventHandler NetworkDisconnected;

        public void Start()
        {
            RegisterForNetworkConnectivityChanges();
        }

        private void RegisterForNetworkConnectivityChanges()
        {
            networkAvailabilityChangedEventHandler = NetworkConnectivityWatcher;
            NetworkChange.NetworkAvailabilityChanged += networkAvailabilityChangedEventHandler;
        }

        private void UnregisterForNetworkConnectivityChanges()
        {
            if (networkAvailabilityChangedEventHandler != null)
            {
                NetworkChange.NetworkAvailabilityChanged -= networkAvailabilityChangedEventHandler;
                networkAvailabilityChangedEventHandler = null;
            }
        }

        private void NetworkConnectivityWatcher(object sender, NetworkAvailabilityEventArgs eventArgs)
        {
            if (eventArgs.IsAvailable)
            {
                this.NetworkConnected?.Invoke(this, null);
            }
            else
            {
                this.NetworkDisconnected?.Invoke(this, null);
            }
        }

        public void Dispose()
        {
            UnregisterForNetworkConnectivityChanges();
        }
    }
}