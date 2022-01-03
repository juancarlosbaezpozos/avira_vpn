using System;
using System.Threading.Tasks;

namespace Avira.VPN.Core
{
    public class IPChecker
    {
        private readonly IApiClient<IPData> apiClient;

        private readonly IInternetAvailabilityMonitor internetAvailabilityMonitor;

        private readonly SemaphoreLock refreshLock = new SemaphoreLock();

        public IPData LastConnectedData;

        public IPData Data => apiClient.Data;

        public event EventHandler IPRefreshed;

        public IPChecker(IApiClient<IPData> apiClient)
            : this(apiClient, DiContainer.Resolve<IInternetAvailabilityMonitor>())
        {
        }

        public IPChecker(IApiClient<IPData> apiClient, IInternetAvailabilityMonitor internetAvailabilityMonitor)
        {
            this.apiClient = apiClient;
            this.internetAvailabilityMonitor = internetAvailabilityMonitor;
            this.internetAvailabilityMonitor.InternetConnected += delegate { Refresh().CatchAll(); };
        }

        public async Task Refresh()
        {
            using (await refreshLock.AquireLockAsync())
            {
                if (internetAvailabilityMonitor.IsInternetAvailable)
                {
                    await apiClient.Refresh(string.Empty);
                    this.IPRefreshed?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}