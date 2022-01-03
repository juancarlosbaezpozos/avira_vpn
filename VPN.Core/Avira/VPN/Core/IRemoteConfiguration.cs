using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avira.VPN.Core
{
    public interface IRemoteConfiguration
    {
        List<RemoteFeatureData> RemoteFeatures { get; }

        List<string> Buckets { get; }

        event EventHandler ConfigurationChanged;

        Task Refresh();
    }
}