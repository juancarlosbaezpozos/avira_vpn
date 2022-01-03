using System;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Newtonsoft.Json;
using Serilog;

namespace Avira.VpnService
{
    public class Traffic : ITraffic
    {
        private readonly IHttpClient httpClient;

        private readonly string deviceId;

        private Avira.VPN.Core.Win.TrafficData backendTrafficData;

        private ulong sessionTraffic;

        internal Avira.VPN.Core.Win.TrafficData BackendTrafficData
        {
            get
            {
                if (backendTrafficData == null)
                {
                    backendTrafficData = new Avira.VPN.Core.Win.TrafficData
                    {
                        UsedInBytes = (ulong)ProductSettings.UsedTraffic
                    };
                    Refresh();
                }

                return backendTrafficData;
            }
        }

        public Avira.VPN.Core.TrafficData TrafficData => new Avira.VPN.Core.TrafficData
        {
            UsedTraffic = BackendTrafficData.UsedInBytes + sessionTraffic,
            TrafficLimit = BackendTrafficData.LimitInBytes
        };

        public event EventHandler<EventArgs> TrafficChanged;

        public Traffic(IHttpClient httpClient, string deviceId)
        {
            this.httpClient = httpClient;
            this.deviceId = deviceId;
            sessionTraffic = 0uL;
            DiContainer.SetGetter("TrafficUsed", () => TrafficData.UsedTraffic);
        }

        public void OnTrafficChanged(object sender, TrafficChangedEventArgs trafficEventArgs)
        {
            sessionTraffic = trafficEventArgs.UsedInBytes;
            this.TrafficChanged?.Invoke(sender, new EventArgs());
        }

        public void Refresh()
        {
            string uri = "traffic?device_id=" + deviceId;

            try
            {
                Avira.VPN.Core.Win.TrafficData trafficData = backendTrafficData ?? new Avira.VPN.Core.Win.TrafficData
                {
                    UsedInBytes = (ulong)ProductSettings.UsedTraffic,
                    LimitInBytes = 0uL
                };
                string value = httpClient.Get(uri);
                if (string.IsNullOrEmpty(value))
                {
                    backendTrafficData = new Avira.VPN.Core.Win.TrafficData
                    {
                        UsedInBytes = (ulong)ProductSettings.UsedTraffic
                    };
                    return;
                }

                backendTrafficData = JsonConvert.DeserializeObject<Avira.VPN.Core.Win.TrafficData>(value);
                sessionTraffic = 0uL;
                Log.Information($"Backend store traffic: {backendTrafficData.UsedInBytes} Bytes.");
                if (trafficData.UsedInBytes != backendTrafficData.UsedInBytes ||
                    trafficData.LimitInBytes != backendTrafficData.LimitInBytes)
                {
                    this.TrafficChanged?.Invoke(this, new EventArgs());
                }
            }
            catch (Exception exception)
            {
                backendTrafficData = new Avira.VPN.Core.Win.TrafficData
                {
                    UsedInBytes = (ulong)ProductSettings.UsedTraffic
                };
                Log.Warning(exception, "Failed to refresh traffic information.");
            }
        }

        public static bool IsLimitReached()
        {
            Traffic traffic = DiContainer.Resolve<Traffic>();
            ulong limit = 0L;
            if (limit != 0L)
            {
                return traffic.TrafficData.UsedTraffic > limit;
            }

            return false;
        }

        public void Update(ulong additionalBytes)
        {
            throw new NotImplementedException();
        }
    }
}