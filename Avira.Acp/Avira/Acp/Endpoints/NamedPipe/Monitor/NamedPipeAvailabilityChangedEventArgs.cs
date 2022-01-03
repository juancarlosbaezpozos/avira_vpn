using System;

namespace Avira.Acp.Endpoints.NamedPipe.Monitor
{
    public class NamedPipeAvailabilityChangedEventArgs : EventArgs
    {
        public NamedPipeStatus Status { get; private set; }

        public NamedPipeAvailabilityChangedEventArgs(NamedPipeStatus status)
        {
            Status = status;
        }
    }
}