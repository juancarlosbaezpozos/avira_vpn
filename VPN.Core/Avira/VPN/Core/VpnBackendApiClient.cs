using System;
using System.Threading.Tasks;
using Serilog;

namespace Avira.VPN.Core
{
    public class VpnBackendApiClient<T> : ApiClient<T> where T : class
    {
        private string frontingApi;

        private bool usedFronting;

        private const string defaultFrontingApi = "https://iron-dot-cobalt-antenna-219709.appspot.com/v1/";

        public bool UsedFronting => usedFronting;

        public VpnBackendApiClient(string baseUri, string relativeUri, string storageKey)
            : base(baseUri, relativeUri, storageKey)
        {
            ReadFrontingConfig();
        }

        internal VpnBackendApiClient(ISettings settings, IHttpClientFactory httpClientFactory, string baseUri,
            string relativeUri, string storageKey, IAuthenticator authenticator = null)
            : base(settings, httpClientFactory, baseUri, relativeUri, storageKey, authenticator)
        {
            ReadFrontingConfig();
        }

        private void ReadFrontingConfig()
        {
            if (settings == null)
            {
                frontingApi = defaultFrontingApi;
            }
            else
            {
                frontingApi = settings.Get("fronting_api", defaultFrontingApi);
            }
        }

        private async Task<string> GetViaFronting(string uri)
        {
            Log.Debug("Switching to Fronting API: " + frontingApi);
            _ = string.Empty;
            try
            {
                string result = await base.Get(frontingApi + uri);
                usedFronting = true;
                return result;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "GET request using Fronting API failed.");
                throw ex;
            }
        }

        public override async Task<string> Get(string uri)
        {
            try
            {
                string result = await base.Get(uri);
                usedFronting = false;
                return result;
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "GET request failed.");
            }

            return await GetViaFronting(uri);
        }

        private async Task<string> PostViaFronting(string uri, string parameters)
        {
            Log.Debug("Switching to Fronting API: " + frontingApi);
            _ = string.Empty;
            try
            {
                string result = await base.Post(frontingApi + uri, parameters);
                usedFronting = true;
                return result;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "POST request using Fronting API failed.");
                throw ex;
            }
        }

        public override async Task<string> Post(string uri, string parameters)
        {
            try
            {
                string result = await base.Post(uri, parameters);
                usedFronting = false;
                return result;
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "POST request failed.");
            }

            return await PostViaFronting(uri, parameters);
        }
    }
}