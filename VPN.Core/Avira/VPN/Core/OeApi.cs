using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.Core
{
    public class OeApi : IOeApi
    {
        private readonly string apiUrl;

        private readonly IApiClient<JObject> apiClient;

        public OeApi(string apiUrl)
            : this(
                new ApiClient<JObject>(DiContainer.Resolve<ISettings>(), new HttpClientFactory(), apiUrl, string.Empty,
                    "OeApi", DiContainer.Resolve<IAuthenticator>()), apiUrl)
        {
        }

        internal OeApi(IApiClient<JObject> apiClient, string apiUrl)
        {
            this.apiClient = apiClient;
            this.apiUrl = apiUrl;
        }

        public async Task SendHeartbeat(JObject customData)
        {
            await UpdateAppEvents("Heartbeat", string.Empty, customData);
        }

        public async Task SendFeatureUsed(string feature, JObject customData)
        {
            await UpdateAppEvents("FeatureUsed", feature, customData);
        }

        public async Task SendAppOpen(string trigger, JObject customData)
        {
            await UpdateAppEvents("AppOpen", trigger, customData);
        }

        public async Task<JObject> UpdateAppEvents(string eventType, string eventName, JObject parameters)
        {
            JObject jObject = new JObject
            {
                ["event_type"] = (JToken)eventType,
                ["service"] = (JToken)"vpn",
                ["parameters"] = parameters
            };
            if (!string.IsNullOrEmpty(eventName))
            {
                jObject["name"] = (JToken)eventName;
            }

            JObject value = new JObject
            {
                ["data"] = new JObject
                {
                    ["type"] = (JToken)"app-events",
                    ["attributes"] = jObject
                }
            };
            string text = await apiClient.Post(apiUrl + "v2/app-events", JsonConvert.SerializeObject(value));
            return string.IsNullOrEmpty(text) ? new JObject() : JObject.Parse(text);
        }

        public async Task<long> GetDeviceId()
        {
            return await TryGetDeviceId();
        }

        private async Task<JToken> Get(string resource, string filter = null)
        {
            string uri = apiUrl + resource + "?" + (filter ?? string.Empty);
            return JObject.Parse(await apiClient.Get(uri))["data"];
        }

        private async Task<long> TryGetDeviceId()
        {
            try
            {
                JToken jToken = await Get("v2/devices");
                if (!(jToken is JArray))
                {
                    return 0L;
                }

                foreach (JToken item in jToken as JArray)
                {
                    if (string.Compare(item["attributes"]!["hardware_id"]!.ToString(),
                            DiContainer.Resolve<IApplicationIds>()?.DeviceId) == 0)
                    {
                        return long.Parse(item["id"]!.ToString());
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to obtain DeviceId.");
            }

            return 0L;
        }
    }
}