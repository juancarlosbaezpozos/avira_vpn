using System;

namespace Avira.Acp.Endpoints.NamedPipe.Monitor
{
    public interface INamedPipeAvailabilityMonitor
    {
        NamedPipeStatus CurrentStatus { get; }

        event EventHandler<NamedPipeAvailabilityChangedEventArgs> StatusChanged;

        void Start();

        void Stop();

        void Reset();
    }
}