using System;

namespace Avira.Acp.Endpoints.NamedPipe
{
    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectReason Reason { get; private set; }

        public DisconnectedEventArgs(DisconnectReason reason)
        {
            Reason = reason;
        }
    }
}