using System.Net;
using System.Net.Http;

namespace Avira.VPN.Core
{
    public class HttpClientFactory : IHttpClientFactory
    {
        public HttpClient NewInstance(CookieContainer cookieContainer)
        {
            if (cookieContainer == null)
            {
                return new HttpClient();
            }

            return new HttpClient(new HttpClientHandler
            {
                CookieContainer = cookieContainer
            });
        }
    }
}