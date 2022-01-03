using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.Core
{
    public class RemoteConfiguration : IRemoteConfiguration
    {
        private IApiClient<RemoteConfigurationData> apiClient;

        private readonly Func<JObject> generatePayload;

        private readonly PersistentTimer persistentTimer;

        public List<RemoteFeatureData> RemoteFeatures { get; private set; }

        public List<string> Buckets { get; private set; }

        public event EventHandler ConfigurationChanged;

        public RemoteConfiguration(IApiClient<RemoteConfigurationData> apiClient, Func<JObject> generatePayload,
            TimeSpan refreshInterval)
        {
            this.apiClient = apiClient;
            this.generatePayload = generatePayload;
            RemoteFeatures = this.apiClient?.Data?.RemoteFeatures ?? new List<RemoteFeatureData>();
            Buckets = this.apiClient?.Data?.Buckets ?? new List<string>();
            persistentTimer = new PersistentTimer(delegate { Refresh().CatchAll(); },
                (int)refreshInterval.TotalMilliseconds, (int)refreshInterval.TotalMilliseconds, "RemoteFeatureRequest");
        }

        public async Task Refresh()
        {
            try
            {
                JObject value = generatePayload();
                await apiClient.Refresh(JsonConvert.SerializeObject(value));
                if (apiClient.Data != null)
                {
                    if (apiClient.Data.RemoteFeatures != null)
                    {
                        RemoteFeatures = apiClient.Data.RemoteFeatures;
                    }

                    if (apiClient.Data.Buckets != null)
                    {
                        Buckets = apiClient.Data.Buckets;
                    }

                    this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "Could not retrive remote configuration.");
            }
        }
    }
}