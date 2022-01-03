using System;

namespace Avira.VPN.Core
{
    public interface IOeStatusReporter : IDisposable
    {
        void Start(TimeSpan startDelay, TimeSpan repeatInterval);
    }
}