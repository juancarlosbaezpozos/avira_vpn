using System;
using System.Threading.Tasks;
using Avira.Messaging;
using Avira.VPN.Shared.Core;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public interface IVpnController
    {
        VpnStatus Status { get; }

        RegionConnectionSettings LastUsedRegion { get; }

        event EventHandler<EventArgs<JObject>> FeaturesChanged;

        event EventHandler<EventArgs<JObject>> StatusChanged;

        event EventHandler<EventArgs<JObject>> KeychainAccessGrantedResult;

        event EventHandler<EventArgs<JObject>> UiVisiblilityChanged;

        event EventHandler<EventArgs<TrafficData>> TrafficLimitReached;

        event EventHandler<DisconnectTimerEventArgs> OnDisconnectTimerChanged;

        event EventHandler<EventArgs<JObject>> ConnectionReestablished;

        JObject GetStatus();

        void Connect(RegionConnectionSettings region);

        void Disconnect();

        Task ReconnectToLastUsedRegion();

        Task ConnectAsync(RegionConnectionSettings region);

        Task DisconnectAsync();
    }
}