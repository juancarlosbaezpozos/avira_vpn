using Avira.VPN.Core.Win;

namespace Avira.VpnService
{
    public interface IOpenVpn
    {
        ConnectionState ConnectionState { get; }

        void Connect(RemoteConnectionSettings selectedRegion, bool isWifiAutoconnect = false);

        void Disconnect();
    }
}