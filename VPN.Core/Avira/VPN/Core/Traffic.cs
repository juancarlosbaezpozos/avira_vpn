using System;
using System.Threading;
using System.Threading.Tasks;
using Avira.Messaging;

namespace Avira.VPN.Core
{
    public sealed class Traffic : ITraffic, IDisposable
    {
        private readonly IApiClient<TrafficData> client;

        private readonly IApplicationIds applicationIds;

        private TrafficData trafficData;

        private ulong currentTrafficSize;

        private object trafficDataLock = new();

        private CancellationTokenSource periodicRefreshCancellationToken;

        private Task periodicRefreshTask;

        [Routing("Data")]
        public TrafficData TrafficData => new TrafficData
        {
            UsedTraffic = Math.Max(currentTrafficSize, trafficData.UsedTraffic),
            TrafficLimit = trafficData.TrafficLimit
        };

        [Routing("DataChanged")] public event EventHandler<EventArgs<TrafficData>> TrafficChanged;

        public Traffic(IApiClient<TrafficData> client)
            : this(client, DiContainer.Resolve<IApplicationIds>())
        {
        }

        public Traffic(IApiClient<TrafficData> client, IApplicationIds applicationIds)
        {
            this.client = client;
            this.applicationIds = applicationIds;
            trafficData = client.Data;
            if (trafficData != null)
            {
                currentTrafficSize = trafficData.UsedTraffic;
            }

            this.client.DataChanged += OnDataChanged;
        }

        public bool LimitReached()
        {
            ulong num = Math.Max(currentTrafficSize, trafficData.UsedTraffic);
            if (trafficData.TrafficLimit != 0L)
            {
                return num > trafficData.TrafficLimit;
            }

            return false;
        }

        public async Task Refresh()
        {
            await client.Refresh("?device_id=" + applicationIds?.ClientId);
        }

        public void Update(ulong additionalBytes)
        {
            TrafficData value;
            lock (trafficDataLock)
            {
                currentTrafficSize += additionalBytes;
                value = new TrafficData
                {
                    UsedTraffic = currentTrafficSize,
                    TrafficLimit = trafficData.TrafficLimit
                };
            }

            this.TrafficChanged?.Invoke(this, new EventArgs<TrafficData>(value));
        }

        public void ResetCurrentTraffic()
        {
            TrafficData value;
            lock (trafficDataLock)
            {
                ulong num2 = (currentTrafficSize = (trafficData.UsedTraffic = 0uL));
                value = new TrafficData
                {
                    UsedTraffic = currentTrafficSize,
                    TrafficLimit = trafficData.TrafficLimit
                };
            }

            this.TrafficChanged?.Invoke(this, new EventArgs<TrafficData>(value));
        }

        public void StartPeriodicRefresh()
        {
            periodicRefreshCancellationToken = new CancellationTokenSource();
            periodicRefreshTask = Task.Run((Func<Task>)TrafficPeriodicRefresh);
        }

        public void StopPeriodicRefresh()
        {
            if (periodicRefreshTask != null && periodicRefreshCancellationToken != null)
            {
                periodicRefreshCancellationToken.Cancel();
                try
                {
                    periodicRefreshTask.Wait();
                }
                catch (AggregateException)
                {
                }

                periodicRefreshCancellationToken.Dispose();
                periodicRefreshCancellationToken = null;
            }
        }

        private void OnDataChanged(object sender, EventArgs args)
        {
            bool flag = false;
            TrafficData value;
            lock (trafficDataLock)
            {
                flag = currentTrafficSize != client.Data.UsedTraffic ||
                       trafficData.TrafficLimit != client.Data.TrafficLimit;
                trafficData = client.Data;
                if (trafficData != null)
                {
                    trafficData.TrafficLimit = 0L;
                }

                currentTrafficSize = Math.Max(currentTrafficSize, trafficData.UsedTraffic);
                value = new TrafficData
                {
                    UsedTraffic = currentTrafficSize,
                    TrafficLimit = trafficData.TrafficLimit
                };
            }

            if (flag)
            {
                this.TrafficChanged?.Invoke(this, new EventArgs<TrafficData>(value));
            }
        }

        private async Task TrafficPeriodicRefresh()
        {
            while (periodicRefreshCancellationToken != null &&
                   !periodicRefreshCancellationToken.IsCancellationRequested)
            {
                await Task.Delay(60000, periodicRefreshCancellationToken.Token);
                await Refresh();
            }
        }

        public void Dispose()
        {
            if (periodicRefreshCancellationToken != null)
            {
                periodicRefreshCancellationToken.Dispose();
            }
        }
    }
}