using System.Net;
using System.Net.Http;

namespace Avira.VPN.Core
{
    public interface IHttpClientFactory
    {
        HttpClient NewInstance(CookieContainer cookieContainer);
    }
}