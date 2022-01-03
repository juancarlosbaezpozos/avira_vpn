using System;
using System.Threading.Tasks;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Avira.VPN.Shared.Core;
using Serilog;

namespace Avira.VpnService
{
    public sealed class VpnConnectorBridge : IVpnConnector
    {
        public VpnStatus Status { get; set; } = VpnStatus.Disconnected;


        public event EventHandler<EventArgs> StatusChanged;

        public Task StartConnectAsync(RegionConnectionSettings connectionSettings,
            Avira.VPN.Shared.Core.Credentials credentials)
        {
            throw new NotImplementedException();
        }

        public Task StartDisconnectAsync()
        {
            throw new NotImplementedException();
        }

        public void OpenVpnOnStateChangedNotification(object sender, Status status)
        {
            Log.Debug($"[!] VpnConnectionBridge : {status.NewState.ToString()}, {status.Error}, {status.Message}\n");
            switch (status.NewState)
            {
                case ConnectionState.Connected:
                    Status = VpnStatus.Connected;
                    break;
                case ConnectionState.Connecting:
                    Status = VpnStatus.Connecting;
                    break;
                case ConnectionState.Disconnected:
                    Status = VpnStatus.Disconnected;
                    break;
                case ConnectionState.Disconnecting:
                    Status = VpnStatus.Disconnecting;
                    break;
                default:
                    Status = VpnStatus.Disconnected;
                    break;
            }

            this.StatusChanged?.Invoke(this, new StatusEventArgs(Status));
        }
    }
}