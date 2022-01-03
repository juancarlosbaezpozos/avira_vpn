using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public interface IApiClient<T>
    {
        T Data { get; }

        bool MultipleApiUpdateOnReconnect { get; set; }

        event EventHandler DataChanged;

        Task Refresh(string parameters, JObject body = null);

        void Clear();

        void UpdateCache(T data);

        Task<string> Get(string uri);

        Task<string> Post(string uri, string parameters);

        Task<string> Put(string uri, string parameters);
    }
}