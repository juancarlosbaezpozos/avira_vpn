using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core.Win
{
    public class HttpAsyncHelper : Singleton<IHttpAsyncHelper, HttpAsyncHelperImpl>
    {
        public static async Task<bool> WaitForInternetConnection(int timeout)
        {
            return await Singleton<IHttpAsyncHelper, HttpAsyncHelperImpl>.Instance.WaitForInternetConnection(timeout);
        }

        public static async Task<string> Put(string requestUrl, string data)
        {
            return await Singleton<IHttpAsyncHelper, HttpAsyncHelperImpl>.Instance.Put(requestUrl, data);
        }

        public static async Task<string> Post(string requestUrl, JObject data)
        {
            return await Singleton<IHttpAsyncHelper, HttpAsyncHelperImpl>.Instance.Post(requestUrl, data);
        }

        public static async Task<bool> IsConnectedToInternet()
        {
            return await Singleton<IHttpAsyncHelper, HttpAsyncHelperImpl>.Instance.IsConnectedToInternet();
        }

        public static async Task<string> Get(string url)
        {
            return await Singleton<IHttpAsyncHelper, HttpAsyncHelperImpl>.Instance.Get(url);
        }

        public static IHttpAsyncHelper CreateInstance()
        {
            return new HttpAsyncHelperImpl();
        }
    }
}