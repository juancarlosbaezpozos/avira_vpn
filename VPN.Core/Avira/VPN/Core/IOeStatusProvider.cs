using System;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public interface IOeStatusProvider
    {
        JObject HeartbeatCustomData { get; }

        JObject EventsCustomData { get; }

        bool WasConnectFeatureUsed();

        bool WasAppOpened();

        void UpdateLastGuiOpened(DateTime now);
    }
}