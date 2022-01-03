using System;
using System.Net;
using System.Threading.Tasks;

namespace Avira.Common.Acp.AppClient
{
    public interface IResourceClient<T> where T : class
    {
        Task<Tuple<T, HttpStatusCode?>> Get();

        Task<Tuple<T, HttpStatusCode?>> Post(string payload);

        void Subscribe(Action<T> callback);
    }
}