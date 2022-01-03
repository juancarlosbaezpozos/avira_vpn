using System;
using System.Threading.Tasks;
using Avira.Acp;
using Avira.Acp.Messages;

namespace Avira.Common.Acp.AppClient
{
    public interface IAcpCommunicator
    {
        event EventHandler<EventArgs> Connected;

        Task<Response> GetRequest(string host, string path);

        Task<Response> PostRequest(string host, string path, string payload);

        void RegisterRepository<T>(BaseResourceRepository<T> repository, string path) where T : class;

        string Subscribe(string host, string path, NotificationHandler notificationHandler);

        void Unsubscribe(string subscriptionId);

        bool IsConnected();
    }
}