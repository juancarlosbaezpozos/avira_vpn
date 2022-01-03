using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace Avira.VPN.Core
{
    public class ApiClient<T> : IApiClient<T> where T : class
    {
        protected readonly string baseUri;

        protected readonly string relativeUri;

        private bool reconnectApiUpdateSubscribed;

        protected readonly ISettings settings;

        private readonly IHttpClientFactory httpClientFactory;

        private readonly string storageKey;

        private IAuthenticator authenticator;

        private JsonSerializerSettings jsonSerializerSettings;

        private const string UserAgentHeaderName = "User-Agent";

        public bool ClearOnHttpRequestError { get; set; }

        protected string Host { get; set; }

        public T Data { get; private set; }

        public bool MultipleApiUpdateOnReconnect { get; set; } = true;


        public event EventHandler DataChanged;

        public ApiClient(string baseUri, string relativeUri, string storageKey)
            : this(DiContainer.Resolve<ISettings>(), (IHttpClientFactory)new HttpClientFactory(), baseUri, relativeUri,
                storageKey, DiContainer.Resolve<IAuthenticator>())
        {
        }

        public ApiClient(ISettings settings, IHttpClientFactory httpClientFactory, string baseUri, string relativeUri,
            string storageKey, IAuthenticator authenticator = null)
        {
            jsonSerializerSettings = new JsonSerializerSettings
            {
                Error = delegate(object sender, ErrorEventArgs e) { e.ErrorContext.Handled = true; }
            };
            this.baseUri = baseUri;
            this.relativeUri = relativeUri;
            this.settings = settings;
            this.httpClientFactory = httpClientFactory;
            this.storageKey = storageKey;
            this.authenticator = authenticator;
            string value = this.settings?.Get(storageKey);
            Data = (T)(string.IsNullOrEmpty(value)
                ? ((object)(T)Activator.CreateInstance(typeof(T)))
                : ((object)JsonConvert.DeserializeObject<T>(value, jsonSerializerSettings)));
        }

        public void Clear()
        {
            Data = (T)Activator.CreateInstance(typeof(T));
            settings?.Set(storageKey, string.Empty);
        }

        public async Task Refresh(string parameters, JObject body = null)
        {
            try
            {
                await UpdateDataFromApi(parameters, body);
            }
            catch (Exception ex)
            {
                if (ClearOnHttpRequestError && ex is HttpRequestException)
                {
                    UpdateCache(null);
                    return;
                }

                string text = baseUri + (string.IsNullOrEmpty(relativeUri) ? "" : relativeUri);
                Log.Debug(ex, "Failed to retrieve " + text + ". Falling back to stored " + storageKey + " cache.");
                UpdateDataFromCache();
                PostponeUpdateDataFromApi(parameters, body);
            }
        }

        public void UpdateCache(T data)
        {
            Data = data;
            settings?.Set(storageKey, JsonConvert.SerializeObject(data));
        }

        private async Task UpdateDataFromApi(string parameters, JObject body)
        {
            _ = string.Empty;
            string value = (parameters.IsValidJson()
                ? (await Post(relativeUri, parameters))
                : ((body == null)
                    ? (await Get(relativeUri + parameters))
                    : (await Post(relativeUri + parameters, body.ToString()))));
            settings?.Set(storageKey, value);
            Data = JsonConvert.DeserializeObject<T>(value, jsonSerializerSettings);
            this.DataChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateDataFromCache()
        {
            string value = settings?.Get(storageKey);
            Data = (string.IsNullOrEmpty(value)
                ? null
                : JsonConvert.DeserializeObject<T>(value, jsonSerializerSettings));
            if (Data != null)
            {
                this.DataChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void PostponeUpdateDataFromApi(string parameters, JObject body)
        {
            IInternetAvailabilityMonitor internetMonitor = DiContainer.Resolve<IInternetAvailabilityMonitor>();
            if (internetMonitor == null || internetMonitor.IsInternetAvailable)
            {
                return;
            }

            string requestUrl = baseUri + (string.IsNullOrEmpty(relativeUri) ? "" : relativeUri);
            Log.Debug("Postpone data update from " + requestUrl + ". Parameters: " + parameters);
            EventHandler handler = null;
            handler = async delegate
            {
                reconnectApiUpdateSubscribed = false;
                while (internetMonitor.IsInternetAvailable)
                {
                    try
                    {
                        await UpdateDataFromApi(parameters, body);
                        internetMonitor.InternetConnected -= handler;
                        return;
                    }
                    catch (WebException exception)
                    {
                        Log.Debug(exception,
                            string.Format("Exception for update from {0} {1}. Internet connection available : {2}",
                                new object[3] { requestUrl, parameters, internetMonitor.IsInternetAvailable }));
                    }
                    catch
                    {
                        return;
                    }

                    await Task.Delay(3000);
                }
            };
            if (MultipleApiUpdateOnReconnect)
            {
                internetMonitor.InternetConnected += handler;
            }
            else if (!reconnectApiUpdateSubscribed)
            {
                internetMonitor.InternetConnected += handler;
                reconnectApiUpdateSubscribed = true;
            }
        }

        private void InitHttpClient(HttpClient httpClient)
        {
            string value = DiContainer.Resolve<IDevice>()?.UserAgentString;
            if (!string.IsNullOrEmpty(value))
            {
                if (httpClient.DefaultRequestHeaders.Contains("User-Agent"))
                {
                    httpClient.DefaultRequestHeaders.Remove("User-Agent");
                }

                httpClient.DefaultRequestHeaders.Add("User-Agent", value);
            }
        }

        private void InitRequest(HttpClient httpClient)
        {
            if (string.IsNullOrEmpty(authenticator?.AccessToken))
            {
                httpClient.DefaultRequestHeaders.Authorization = null;
            }
            else
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", authenticator.AccessToken);
            }

            if (!string.IsNullOrEmpty(Host))
            {
                httpClient.DefaultRequestHeaders.Host = Host;
            }
        }

        public virtual async Task<string> Get(string uri)
        {
            return await ApiRequest(uri, HttpMethod.Get, string.Empty);
        }

        public virtual async Task<string> Post(string uri, string parameters)
        {
            return await ApiRequest(uri, HttpMethod.Post, parameters);
        }

        public virtual async Task<string> Put(string uri, string parameters)
        {
            return await ApiRequest(uri, HttpMethod.Put, parameters);
        }

        private async Task<string> ApiRequest(string uri, HttpMethod method, string parameters)
        {
            string text = (IsAbsoluteUrl(uri) ? uri : (baseUri + uri));
            using HttpClient httpClient = httpClientFactory.NewInstance(null);
            InitHttpClient(httpClient);
            InitRequest(httpClient);
            Log.Debug(string.Format("{0}-Api-Request: {1} {2}", new object[3] { method, text, parameters }));
            HttpResponseMessage httpResponseMessage;
            if (method == HttpMethod.Get)
            {
                httpResponseMessage = await httpClient.GetAsync(text);
            }
            else if (method == HttpMethod.Post)
            {
                StringContent content = new StringContent(parameters, Encoding.UTF8, "application/json");
                httpResponseMessage = await httpClient.PostAsync(text, content);
            }
            else
            {
                if (!(method == HttpMethod.Put))
                {
                    throw new Exception("Unexpected HttpMethod");
                }

                StringContent content2 = new StringContent(parameters, Encoding.UTF8, "application/json");
                httpResponseMessage = await httpClient.PutAsync(text, content2);
            }

            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Log.Debug(string.Format("{0} Failed: {1}", new object[2] { method, httpResponseMessage }));
                throw new Exception(httpResponseMessage.ReasonPhrase);
            }

            string text2 = await httpResponseMessage.Content.ReadAsStringAsync();
            Log.Debug(string.Format("{0}-Api-Response: {1}", new object[2] { method, text2 }));
            return text2;
        }

        private bool IsAbsoluteUrl(string url)
        {
            Uri result;
            return Uri.TryCreate(url, UriKind.Absolute, out result);
        }
    }
}