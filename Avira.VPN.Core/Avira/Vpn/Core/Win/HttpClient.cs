using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Serilog;

namespace Avira.VPN.Core.Win
{
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute",
        Justification = "WebRequest.GetResponse throws exception.")]
    [SuppressMessage("ReSharper", "PossibleNullReferenceException",
        Justification = "WebRequest.Create throws exception.")]
    public class HttpClient : IHttpClient
    {
        private readonly IAuthenticator auth2;

        public int Timeout { get; set; } = 20000;


        public Uri Url { get; set; }

        public string UserAgentString { get; set; } = "Mozilla/5.0 (Windows NT " + OsVersion + ") " +
                                                      ProductSettings.UacProductName + "/" +
                                                      ProductSettings.ShortProductVersion;


        public string AuthenticationToken => auth2?.AccessToken;

        public string Host { get; set; }

        private static string OsVersion
        {
            get
            {
                string text = Path.Combine(Environment.SystemDirectory, "kernel32.dll");
                if (!File.Exists(text))
                {
                    return "0.0";
                }

                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(text);
                return $"{versionInfo.FileMajorPart}.{versionInfo.FileMinorPart}";
            }
        }

        public HttpClient(Uri url)
        {
            Url = url;
        }

        public HttpClient()
        {
        }

        protected HttpClient(Uri url, IAuthenticator auth2)
            : this(url)
        {
            this.auth2 = auth2;
        }

        public virtual string Get(string uri)
        {
            Uri uri2 = (IsAbsoluteUrl(uri) ? new Uri(uri) : ConstructRequestUri(uri));
            Log.Debug("HttpClient request: " + uri2);
            string text = string.Empty;
            try
            {
                text = InternalGet(uri2);
            }
            catch (WebException ex)
            {
                if (!IsConnectionRefused(ex) || !WebRequest.DefaultWebProxy.GetProxy(uri2).IsLoopback)
                {
                    throw;
                }

                Log.Warning(ex, "HttpClient request failed. It seems like proxy settings are invalid!");
            }

            if (string.IsNullOrEmpty(text) && WebRequest.DefaultWebProxy != null)
            {
                Log.Warning("HttpClient retrying request without proxy.");
                WebRequest.DefaultWebProxy = null;
                text = InternalGet(uri2);
            }

            return text;
        }

        public string GetWithRetries(string uri, int numberOfRetries)
        {
            for (int i = 0; i < numberOfRetries; i++)
            {
                try
                {
                    return Get(uri);
                }
                catch (WebException)
                {
                    Log.Debug($"[HttpClient.GetWithRetries] Got an exception, trying to reconnect #{i}");
                    if (i == numberOfRetries - 1)
                    {
                        throw;
                    }

                    Thread.Sleep(1000);
                }
            }

            return string.Empty;
        }

        protected bool IsConnectionRefused(WebException ex)
        {
            if (ex.InnerException != null && ex.InnerException is SocketException)
            {
                return ((SocketException)ex.InnerException).SocketErrorCode == SocketError.ConnectionRefused;
            }

            return false;
        }

        protected string InternalGet(Uri uri)
        {
            HttpWebRequest httpWebRequest = WebRequest.Create(uri) as HttpWebRequest;
            InitializeRequest(httpWebRequest);
            using StreamReader streamReader = new StreamReader(httpWebRequest.GetResponse().GetResponseStream());
            string text = streamReader.ReadToEnd();
            Log.Debug("HttpClient response:" + text.Substring(0, Math.Min(text.Length, 128)));
            return text;
        }

        private Uri ConstructRequestUri(string relativeUri)
        {
            if (Url != null)
            {
                if (!string.IsNullOrEmpty(relativeUri))
                {
                    return new Uri(Url, relativeUri);
                }

                return Url;
            }

            return new Uri(relativeUri);
        }

        public string Post(string json)
        {
            return Post(string.Empty, json);
        }

        public virtual string Post(string uri, string jsonData)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(jsonData);
            return Post(uri, bytes, "text/json");
        }

        public string Post(string uri, byte[] data, string contentType)
        {
            HttpWebRequest httpWebRequest =
                WebRequest.Create(IsAbsoluteUrl(uri) ? new Uri(uri) : ConstructRequestUri(uri)) as HttpWebRequest;
            InitializeRequest(httpWebRequest);
            httpWebRequest.ContentType = contentType;
            httpWebRequest.Method = "POST";
            using (Stream stream = httpWebRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }

            using StreamReader streamReader =
                new StreamReader((httpWebRequest.GetResponse() as HttpWebResponse).GetResponseStream());
            return streamReader.ReadToEnd();
        }

        private void InitializeRequest(HttpWebRequest httpWebRequest)
        {
            httpWebRequest.Timeout = Timeout;
            httpWebRequest.UserAgent = UserAgentString;
            if (!string.IsNullOrEmpty(Host))
            {
                httpWebRequest.Host = Host;
            }

            if (!string.IsNullOrEmpty(AuthenticationToken))
            {
                httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, AuthenticationToken);
            }
        }

        public void DownloadFileAsync(string uri, string destination,
            AsyncCompletedEventHandler downloadCompletedEventHandler)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadFileCompleted += delegate(object s, AsyncCompletedEventArgs e)
            {
                try
                {
                    downloadCompletedEventHandler(s, e);
                }
                finally
                {
                    webClient?.Dispose();
                }
            };
            webClient.DownloadFileAsync(new Uri(uri), destination);
        }

        private bool IsAbsoluteUrl(string url)
        {
            Uri result;
            return Uri.TryCreate(url, UriKind.Absolute, out result);
        }
    }
}