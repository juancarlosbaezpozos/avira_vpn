using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core.Win
{
    public interface IHttpAsyncHelper
    {
        string Authorization { get; set; }

        Task<bool> WaitForInternetConnection(int timeout);

        Task<bool> IsConnectedToInternet();

        Task<string> Get(string url);

        Task<string> Put(string url, string data);

        Task<string> Post(string url, JObject data);
    }
}