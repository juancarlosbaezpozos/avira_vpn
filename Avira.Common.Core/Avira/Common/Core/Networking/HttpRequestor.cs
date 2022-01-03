using System;
using System.IO;
using System.Net;

namespace Avira.Common.Core.Networking
{
    public class HttpRequestor : IHttpRequestor
    {
        private enum ProxyPresence
        {
            Present,
            NotPresent,
            Invalid
        }

        private readonly bool allowUnsecureConnections;

        public bool AllowAutoRedirect { get; set; }

        public HttpRequestor(bool allowUnsecureConnections = false)
        {
            this.allowUnsecureConnections = allowUnsecureConnections;
            ServicePointManager.Expect100Continue = false;
            AllowAutoRedirect = true;
        }

        private static bool IsSecureUri(Uri uri)
        {
            if (uri.IsAbsoluteUri)
            {
                if (!(uri.Scheme == Uri.UriSchemeHttps) && !(uri.Scheme == "wss"))
                {
                    return uri.Scheme == Uri.UriSchemeFile;
                }

                return true;
            }

            return false;
        }

        public string GetResponse(Uri requestUri)
        {
            try
            {
                using WebResponse webResponse = GetWebResponse(requestUri);
                using StreamReader streamReader = new StreamReader(webResponse.GetResponseStream());
                return streamReader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Log.Debug("Getting http response failed: " + ex.Message);
                return string.Empty;
            }
        }

        public WebResponse GetWebResponse(Uri requestUri)
        {
            if (!allowUnsecureConnections && !IsSecureUri(requestUri))
            {
                Log.Error("Error in createing a webrequest. The given URL '{0}' is not secure.",
                    requestUri.AbsoluteUri);
                return null;
            }

            return CreateWebRequest(requestUri).GetResponse();
        }

        public IWebProxy GetProxy(Uri requestUri)
        {
            return CreateWebRequest(requestUri).Proxy;
        }

        private WebRequest CreateWebRequest(Uri requestUri)
        {
            ResetDefaultProxyIfInvalid();
            WebRequest webRequest = WebRequest.Create(requestUri);
            if (ProxyNativeMethods.ConnectingThroughProxy())
            {
                webRequest.Proxy = GetDefaultProxy();
            }
            else
            {
                webRequest.Proxy = null;
            }

            HttpWebRequest httpWebRequest = webRequest as HttpWebRequest;
            if (httpWebRequest != null)
            {
                httpWebRequest.AllowAutoRedirect = AllowAutoRedirect;
            }

            return webRequest;
        }

        private void ResetDefaultProxyIfInvalid()
        {
            if (!IsDefaultProxyValid())
            {
                SetDefaultProxy(null);
            }
        }

        private bool IsDefaultProxyValid()
        {
            return GetDefaultProxyPresence() != ProxyPresence.Invalid;
        }

        private ProxyPresence GetDefaultProxyPresence()
        {
            try
            {
                return (WebRequest.DefaultWebProxy == null) ? ProxyPresence.NotPresent : ProxyPresence.Present;
            }
            catch
            {
                return ProxyPresence.Invalid;
            }
        }

        private void SetDefaultProxy(IWebProxy newDefaultWebProxy)
        {
            WebRequest.DefaultWebProxy = newDefaultWebProxy;
        }

        private IWebProxy GetDefaultProxy()
        {
            return WebRequest.DefaultWebProxy;
        }
    }
}