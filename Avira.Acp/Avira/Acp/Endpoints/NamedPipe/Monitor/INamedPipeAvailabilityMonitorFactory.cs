using System;

namespace Avira.Acp.Endpoints.NamedPipe.Monitor
{
    public interface INamedPipeAvailabilityMonitorFactory
    {
        INamedPipeAvailabilityMonitor Create(string pipeName);

        INamedPipeAvailabilityMonitor Create(string pipeName, TimeSpan pollTimeout);
    }
}