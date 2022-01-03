using System;
using Avira.VPN.Core.Win;

namespace Avira.VpnService
{
    public interface ISession
    {
        ulong TrafficUsedTotal { get; }

        event EventHandler<TrafficChangedEventArgs> TrafficChanged;
    }
}