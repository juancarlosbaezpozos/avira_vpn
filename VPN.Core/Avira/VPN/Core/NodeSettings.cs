using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.Core
{
    public sealed class NodeSettings : INodeSettings
    {
        private readonly IHttpClientFactory httpClientFactory;

        private readonly string uri;

        private NodeSessionInfo sessionInfo;

        public NodeSettings(IHttpClientFactory httpClientFactory, string uri)
        {
            this.httpClientFactory = httpClientFactory;
            this.uri = uri;
        }

        public NodeSettings()
            : this(new HttpClientFactory(), VpnSettings.VpnNodeApi)
        {
        }

        public async Task UpdateFeatures(NodeSessionInfo nodeSessionInfo)
        {
            sessionInfo = nodeSessionInfo;
            await UpdateFeatures();
        }

        public async Task UpdateFeatures()
        {
            if (sessionInfo == null)
            {
                return;
            }

            JObject value = new JObject
            {
                ["ads"] = (JToken)DiContainer.Resolve<IAppSettings>().Get().AdBlocking,
                ["malware"] = (JToken)DiContainer.Resolve<IAppSettings>().Get().MalwareProtection,
                ["phishing"] = (JToken)DiContainer.Resolve<IAppSettings>().Get().MalwareProtection
            };
            JObject value2 = new JObject
                { ["streaming"] = (JToken)DiContainer.Resolve<IFeatures>().IsActive("streaming") };
            JObject jObject = new JObject
            {
                ["selected_region"] = (JToken)sessionInfo.SelectedRegionId,
                ["fronted"] = (JToken)sessionInfo.Fronted,
                ["features"] = value2,
                ["filters"] = value
            };
            Log.Debug("Updating node features : " + jObject.ToString());
            using HttpClient httpClient = httpClientFactory.NewInstance(null);
            StringContent content = new StringContent(jObject.ToString(), Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(uri + "/v1/settings", content);
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Log.Warning($"Failed to set the node settings. Status code : {httpResponseMessage.StatusCode}");
            }
            else if (httpResponseMessage.Content == null)
            {
                Log.Warning("Failed to set the node settings. Empty list of features.");
            }
            else
            {
                Log.Debug("Active node settings : " + await (httpResponseMessage.Content?.ReadAsStringAsync()));
            }
        }
    }
}