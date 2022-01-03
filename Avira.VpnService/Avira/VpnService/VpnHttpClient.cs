using System;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Avira.VpnService.Properties;
using Serilog;

namespace Avira.VpnService
{
    public class VpnHttpClient : HttpClient
    {
        private bool usedFronting;

        public bool UsedFronting => usedFronting;

        public VpnHttpClient(IAuthenticator authentification)
            : base(new Uri(Settings.Default.VpnBackendUrl), authentification)
        {
        }

        public override string Get(string uri)
        {
            try
            {
                string result = base.Get(uri);
                usedFronting = false;
                return result;
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "GET request to " + uri + " failed.");
            }

            Log.Debug("Switching to Fronting API: " + Settings.Default.FrontingApi);
            return GetViaFronting(uri);
        }

        internal string GetViaFronting(string uri)
        {
            string result = string.Empty;
            try
            {
                base.Host = new Uri(Settings.Default.FrontingApi).Host;
                result = base.Get(Settings.Default.FrontingApi + uri);
                usedFronting = true;
                return result;
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "GET request to " + uri + " using Fronting API failed.");
                return result;
            }
            finally
            {
                base.Host = string.Empty;
            }
        }

        public override string Post(string uri, string jsonData)
        {
            try
            {
                string result = base.Post(uri, jsonData);
                usedFronting = false;
                return result;
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "POST request to " + uri + " failed.");
            }

            Log.Debug("Switching to Fronting API: " + Settings.Default.FrontingApi);
            return PostViaFronting(uri, jsonData);
        }

        internal string PostViaFronting(string uri, string jsonData)
        {
            string result = string.Empty;
            try
            {
                base.Host = new Uri(Settings.Default.FrontingApi).Host;
                result = base.Post(Settings.Default.FrontingApi + uri, jsonData);
                usedFronting = true;
                return result;
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "POST request to " + uri + " using Fronting API failed.");
                return result;
            }
            finally
            {
                base.Host = string.Empty;
            }
        }
    }
}