using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Avira.Common.Core.Networking;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Avira.Win.Messaging;
using Serilog;

namespace Avira.VpnService
{
    public class RegionsLatency
    {
        private readonly INetworkChangeMonitor networkChangeMonitor;

        private readonly IInternetConnectionMonitor internetConnectionMonitor;

        private static ConcurrentDictionary<string, int> possibleLatencies = new ConcurrentDictionary<string, int>();

        [Routing("latency", true)] public event EventHandler<LatencyData> LatencyProbingCompleted;

        public RegionsLatency()
            : this(new NetworkChangeMonitor(), new InternetConnectionMonitor())
        {
        }

        internal RegionsLatency(INetworkChangeMonitor networkChangeMonitor,
            IInternetConnectionMonitor internetConnectionMonitor)
        {
            this.networkChangeMonitor = networkChangeMonitor;
            this.internetConnectionMonitor = internetConnectionMonitor;
            if (this.networkChangeMonitor != null)
            {
                this.networkChangeMonitor.NetworkConnected += delegate { ProbeLatency(); };
                this.networkChangeMonitor.NetworkDisconnected += delegate { ResetLatency(); };
                this.networkChangeMonitor.Start();
            }
        }

        [Routing("regions/latency")]
        public void ProbeLatency()
        {
            Regions regions = DiContainer.Resolve<Regions>();
            if (regions != null)
            {
                IVpnProvider vpnProvider = DiContainer.Resolve<IVpnProvider>();
                if (vpnProvider == null || vpnProvider.ConnectionState != ConnectionState.Connected)
                {
                    ProbeLatency(regions.RegionList);
                }
            }
        }

        public void ResetLatency()
        {
            Regions regions = DiContainer.Resolve<Regions>();
            if (regions == null)
            {
                return;
            }

            foreach (RemoteConnectionSettings serversConnectionSetting in regions.RegionList.ServersConnectionSettings)
            {
                this.LatencyProbingCompleted?.Invoke(this, new LatencyData
                {
                    Id = serversConnectionSetting.Id,
                    Latency = 0L
                });
            }
        }

        public void ProbeLatency(RegionList regions, int timeout = 3000)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return;
            }

            internetConnectionMonitor.InitializeAsync();
            Task.Run(delegate
            {
                Task.Delay(1000);
                if (internetConnectionMonitor.LastKnownStatus != InternetConnectionStatus.Disconnected)
                {
                    List<Task> tasks = regions.ServersConnectionSettings
                        .Select((RemoteConnectionSettings region) => SendPing(region, timeout)).Cast<Task>().ToList();
                    try
                    {
                        Task.WhenAll(tasks).Wait(timeout * 2);
                    }
                    catch (AggregateException exception)
                    {
                        Log.Warning(exception, "Failed to proble latecy for region list.");
                        NotifyFailedTasks(regions, tasks);
                    }
                }
            });
        }

        private void NotifyFailedTasks(RegionList regions, List<Task> tasks)
        {
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].Exception != null)
                {
                    Exception ex = tasks[i].Exception;
                    RemoteConnectionSettings remoteConnectionSettings = regions.ServersConnectionSettings[i];
                    while (ex.InnerException != null)
                    {
                        ex = ex.InnerException;
                    }

                    Log.Warning(ex, "Pinging " + remoteConnectionSettings.Uri + " failed.");
                    LatencyData e = new LatencyData
                    {
                        Id = remoteConnectionSettings.Id,
                        Error = ex.Message
                    };
                    this.LatencyProbingCompleted?.Invoke(this, e);
                }
            }
        }

        protected Task<bool> SendPing(RemoteConnectionSettings region, int timeout = 5000)
        {
            return new Ping().SendPingAsync(region.Uri, timeout).ContinueWith(delegate(Task<PingReply> reply)
            {
                LatencyData args = new LatencyData
                {
                    Id = region.Id,
                    IPStatus = reply.Result.Status,
                    Latency = reply.Result.RoundtripTime
                };
                possibleLatencies.AddOrUpdate(args.Id, (int)args.Latency,
                    (string _key, int _latency) => (int)args.Latency);
                this.LatencyProbingCompleted?.Invoke(this, args);
                return true;
            });
        }

        internal static int GetPing(string uri, int timeout = 5000)
        {
            using Ping ping = new Ping();
            return ping.SendPingAsync(uri, timeout).ContinueWith(delegate(Task<PingReply> replyTask)
                {
                    if (replyTask.IsFaulted)
                    {
                        return 0;
                    }

                    if (replyTask.Result.Status != 0)
                    {
                        return 0;
                    }

                    return (int)((replyTask.Result.RoundtripTime <= 0) ? 1 : replyTask.Result.RoundtripTime);
                }).GetAwaiter()
                .GetResult();
        }

        internal static int PossibleLatency(string regionId, int latency)
        {
            return possibleLatencies.GetOrAdd(regionId, latency);
        }
    }
}