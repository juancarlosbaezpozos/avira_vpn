using System;

namespace Avira.VpnService
{
    public interface IManagementEndpoint : IDisposable
    {
        event EventHandler<ManagementMessage> MessageReceived;

        event EventHandler<EventArgs> StreamClosed;

        void Request(string command);
    }
}