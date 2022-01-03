using System;

namespace Avira.Acp.Endpoints.NamedPipe.Monitor
{
    public class NamedPipeAvailabilityMonitorFactory : INamedPipeAvailabilityMonitorFactory
    {
        public INamedPipeAvailabilityMonitor Create(string pipeName)
        {
            return new NamedPipeAvailabilityMonitor(pipeName);
        }

        public INamedPipeAvailabilityMonitor Create(string pipeName, TimeSpan pollTimeout)
        {
            return new NamedPipeAvailabilityMonitor(pipeName, pollTimeout);
        }
    }
}