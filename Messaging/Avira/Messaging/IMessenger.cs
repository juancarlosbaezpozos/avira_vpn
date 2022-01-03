using System;

namespace Avira.Messaging
{
    public interface IMessenger : IDisposable
    {
        event EventHandler<EventArgs<string>> MessageReceived;

        event EventHandler ConnectionReestablished;

        void Send(string message);
    }
}