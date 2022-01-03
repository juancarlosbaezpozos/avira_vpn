using System;
using Avira.Messaging;
using Avira.VPN.Shared.Core;
using Serilog;

namespace Avira.VPN.Core
{
    public class ConnectionMonitor
    {
        private readonly IVpnConnector vpnConnector;

        private readonly IInternetAvailabilityMonitor internetAvailabilityMonitor;

        private bool connectionSuccessful;

        private int retryConnectionCount;

        public bool ConnectRequested { get; set; }

        public bool IPSecPortsAreOpen { get; set; } = true;


        internal int MaxRetryReconnecting { get; set; } = 1;


        public event EventHandler<EventArgs<VpnError>> ConnectionError;

        public ConnectionMonitor(IVpnConnector vpnConnector)
            : this(vpnConnector, DiContainer.Resolve<IInternetAvailabilityMonitor>())
        {
        }

        public ConnectionMonitor(IVpnConnector vpnConnector, IInternetAvailabilityMonitor internetAvailabilityMonitor)
        {
            this.vpnConnector = vpnConnector;
            this.vpnConnector.StatusChanged += VpnStatusChangedHandler;
            this.internetAvailabilityMonitor = internetAvailabilityMonitor;
        }

        private void VpnStatusChangedHandler(object sender, EventArgs e)
        {
            if (ConnectRequested)
            {
                if (vpnConnector.Status == VpnStatus.Connected)
                {
                    connectionSuccessful = true;
                    retryConnectionCount = 0;
                }
                else if (vpnConnector.Status == VpnStatus.Disconnected)
                {
                    if (connectionSuccessful)
                    {
                        ReconnectVpn();
                        return;
                    }

                    VpnError connectionError = GetConnectionError();
                    this.ConnectionError?.Invoke(this, new EventArgs<VpnError>(connectionError));
                }
            }
            else if (vpnConnector.Status == VpnStatus.Disconnected)
            {
                connectionSuccessful = false;
                retryConnectionCount = 0;
            }
        }

        private VpnError GetConnectionError()
        {
            VpnError result = VpnError.ConnectionFailed;
            if (internetAvailabilityMonitor == null || !internetAvailabilityMonitor.IsInternetAvailable)
            {
                result = VpnError.NoNetworkAvailable;
            }
            else if (!IPSecPortsAreOpen)
            {
                result = VpnError.IpsecBlocked;
            }

            return result;
        }

        private void ReconnectVpn()
        {
            if (retryConnectionCount < MaxRetryReconnecting)
            {
                retryConnectionCount++;
                DiContainer.Resolve<IVpnController>().ReconnectToLastUsedRegion().Catch(delegate(Exception ex)
                {
                    Log.Error(ex, "Failed to connect to last user region.");
                });
            }
            else
            {
                this.ConnectionError?.Invoke(this, new EventArgs<VpnError>(VpnError.ConnectionLost));
            }
        }
    }
}