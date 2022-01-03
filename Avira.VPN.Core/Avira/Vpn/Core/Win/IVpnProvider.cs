using System;
using Avira.Win.Messaging;

namespace Avira.VPN.Core.Win
{
    public interface IVpnProvider
    {
        ConnectionState ConnectionState { get; }

        RemoteConnectionSettings ConnectedRegion { get; }

        event EventHandler<EventArgs<Status>> StatusChanged;

        void Connect(RemoteConnectionSettings selectedRegion);

        void ConnectToLastSelectedRegion(string triggerSource, bool isTriggeredByAutoconnect = false);

        void StartClientApp(string triggerSource, bool startMinimized = false);

        void Disconnect();
    }
}