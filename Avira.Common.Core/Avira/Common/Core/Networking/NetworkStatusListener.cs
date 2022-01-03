using System;
using System.Net.NetworkInformation;

namespace Avira.Common.Core.Networking
{
    public class NetworkStatusListener : INetworkStatusListener
    {
        public event EventHandler StatusChanged;

        public NetworkStatusListener()
        {
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            RaiseStatusChangedEvent();
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            RaiseStatusChangedEvent();
        }

        private void RaiseStatusChangedEvent()
        {
            this.StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}