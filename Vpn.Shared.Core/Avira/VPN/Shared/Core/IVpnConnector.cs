using System;
using System.Threading.Tasks;

namespace Avira.VPN.Shared.Core
{
    public interface IVpnConnector
    {
        VpnStatus Status { get; }

        event EventHandler<EventArgs> StatusChanged;

        Task StartConnectAsync(RegionConnectionSettings connectionSettings, Credentials credentials);

        Task StartDisconnectAsync();
    }
}