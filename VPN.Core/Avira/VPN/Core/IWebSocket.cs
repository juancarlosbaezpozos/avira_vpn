using System;
using System.Threading.Tasks;
using Avira.Messaging;

namespace Avira.VPN.Core
{
    public interface IWebSocket
    {
        WsStatus Status { get; }

        event EventHandler<WsClosedEventArgs> OnClose;

        event EventHandler<WsErrorEventArgs> OnError;

        event EventHandler<EventArgs<string>> OnMessage;

        Task<bool> Connect(string url);

        Task<int> Close();

        Task Send(string message);
    }
}