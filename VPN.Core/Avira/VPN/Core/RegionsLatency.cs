using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Avira.Messaging;
//using Avira.Utilities.Pcl;
using Avira.VPN.Shared.Core;
using Serilog;

namespace Avira.VPN.Core
{
    public class RegionsLatency
    {
        private const int StartupDelay = 3000;

        private const int PingDelay = 5000;

        private readonly IInternetAvailabilityMonitor internetConnectionMonitor;

        [Routing("latency")] public event EventHandler<LatencyData> LatencyProbingCompleted;

        public RegionsLatency()
            : this(DiContainer.Resolve<IInternetAvailabilityMonitor>())
        {
        }

        internal RegionsLatency(IInternetAvailabilityMonitor internetConnectionMonitor)
        {
            this.internetConnectionMonitor = internetConnectionMonitor;
            NetworkChange.NetworkAddressChanged += delegate { ResetLatency(); };
            this.internetConnectionMonitor.InternetConnected += delegate { ProbeLatency(); };
        }

        [Routing("regions/latency")]
        public void ProbeLatency()
        {
            if (DiContainer.Resolve<IVpnController>().Status == VpnStatus.Disconnected)
            {
                ProbeLatencyAsync().Catch(delegate(Exception e)
                {
                    Log.Error(e, "Failed to probe latency for regions list.");
                });
            }
        }

        public void ResetLatency()
        {
            Regions regions = DiContainer.Resolve<Regions>();
            if (regions == null)
            {
                return;
            }

            foreach (RegionConnectionSettings region in regions.RegionList.Regions)
            {
                this.LatencyProbingCompleted?.Invoke(this, new LatencyData
                {
                    Id = region.Id,
                    Latency = 0L
                });
            }
        }

        private async Task ProbeLatencyAsync()
        {
            await Task.Delay(3000);
            if (!internetConnectionMonitor.IsInternetAvailable)
            {
                return;
            }

            Regions regions = DiContainer.Resolve<Regions>();
            foreach (RegionConnectionSettings region in regions.RegionList.Regions)
            {
                await SendPing(region);
            }
        }

        private async Task SendPing(RegionConnectionSettings region, int timeout = 5000)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }

            try
            {
                //IPing pinger = DiContainer.Resolve<IPing>();
                //try
                //{
                //PingReplyWrapper val = await pinger.SendPingAsync(region.Host, timeout);
                await Task.Delay(3000); //TODO: quitar esto que es mio y no es necesario
                this.LatencyProbingCompleted?.Invoke(this, new LatencyData
                {
                    Id = region.Id,
                    //IPStatus = val.get_Status(),
                    Latency = 100 /*val.get_RoundtripTime()*/ //TODO: falta referencia a dll
                });
                //}
                //finally
                //{
                //	((IDisposable)pinger)?.Dispose();
                //}
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to probe latency for region " + region.Id);
                this.LatencyProbingCompleted?.Invoke(this, new LatencyData
                {
                    Id = region.Id,
                    Error = ex.Message
                });
            }
        }
    }
}