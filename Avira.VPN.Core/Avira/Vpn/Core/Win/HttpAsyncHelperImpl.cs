using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.Core.Win
{
    public class HttpAsyncHelperImpl : IHttpAsyncHelper
    {
        internal class NativeMethods
        {
            public const int InternetConnectionProxy = 4;

            [DllImport("wininet.dll", CharSet = CharSet.Auto)]
            public static extern bool InternetGetConnectedState(ref int lpdwFlags, int dwReserved);
        }

        private const string NcsiResponse = "Microsoft NCSI";

        private const string NcsiUrl = "http://www.msftncsi.com/ncsi.txt";

        private const int MaxLoggingResponseLength = 1024;

        public string Authorization { get; set; }

        public async Task<bool> WaitForInternetConnection(int timeout)
        {
            bool haveInternetConnection = false;
            int tick = Environment.TickCount;
            while (Environment.TickCount - tick < timeout)
            {
                if (await IsConnectedToInternet())
                {
                    haveInternetConnection = true;
                    break;
                }

                await Task.Delay(100);
            }

            if (Environment.TickCount - tick > 5)
            {
                Log.Debug(
                    $"WaitForInternetConnection - waitTime:{Environment.TickCount - tick}, statuts:{haveInternetConnection}");
            }

            return haveInternetConnection;
        }

        public async Task<bool> IsConnectedToInternet()
        {
            try
            {
                return await Get("http://www.msftncsi.com/ncsi.txt") == "Microsoft NCSI";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> Get(string url)
        {
            Log.Debug("HttpAsync request: GET " + url);
            string result = string.Empty;
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            PrepareRequest(httpWebRequest);
            using (WebResponse response = await httpWebRequest.GetResponseAsync())
            {
                Stream responseStream = response.GetResponseStream();
                if (responseStream != null)
                {
                    using StreamReader requestStreamReader = new StreamReader(responseStream);
                    result = await requestStreamReader.ReadToEndAsync();
                }
            }

            Log.Debug("HttpAsync response: " + ((result.Length <= 1024) ? result : result.Substring(0, 1024)));
            return result;
        }

        public async Task<string> Put(string url, string data)
        {
            return await Request(url, data, "PUT");
        }

        public async Task<string> Post(string url, JObject data)
        {
            return await Request(url, data.ToString(), "POST");
        }

        public async Task<string> Request(string url, string data, string method)
        {
            Log.Debug("HttpAsync request: " + method + " " + url);
            string result = string.Empty;
            using (WebClient webClient = new WebClient())
            {
                UpdateProxy(webClient);
                if (!string.IsNullOrEmpty(Authorization))
                {
                    webClient.Headers.Add(HttpRequestHeader.Authorization, Authorization);
                }

                try
                {
                    byte[] bytes =
                        await webClient.UploadDataTaskAsync(new Uri(url), method, Encoding.UTF8.GetBytes(data));
                    result = Encoding.UTF8.GetString(bytes);
                }
                catch (WebException ex)
                {
                    Log.Warning(ex, "Request failed.");
                    using StreamReader requestStreamReader = new StreamReader(ex.Response.GetResponseStream());
                    result = await requestStreamReader.ReadToEndAsync();
                }
            }

            Log.Debug("HttpAsync response: " + ((result.Length <= 1024) ? result : result.Substring(0, 1024)));
            return result;
        }

        private static bool UsingProxy()
        {
            int lpdwFlags = 0;
            NativeMethods.InternetGetConnectedState(ref lpdwFlags, 0);
            bool num = (lpdwFlags & 4) != 0;
            DefaultProxySection defaultProxySection =
                ConfigurationManager.GetSection("system.net/defaultProxy") as DefaultProxySection;
            if (!num)
            {
                return defaultProxySection != null;
            }

            return true;
        }

        private void PrepareRequest(WebRequest webRequest)
        {
            try
            {
                if (UsingProxy())
                {
                    webRequest.Proxy = WebRequest.DefaultWebProxy;
                }

                if (!string.IsNullOrEmpty(Authorization))
                {
                    webRequest.Headers.Add("Authorization", Authorization);
                }
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to prepare request.");
            }
        }

        private void UpdateProxy(WebClient webClient)
        {
            try
            {
                if (UsingProxy())
                {
                    webClient.Proxy = WebRequest.DefaultWebProxy;
                }
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to set proxy.");
            }
        }
    }
}