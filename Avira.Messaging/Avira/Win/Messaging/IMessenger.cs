using System;

namespace Avira.Win.Messaging
{
    public interface IMessenger : IDisposable
    {
        event EventHandler<MessageReceivedEvent> MessageReceived;

        event EventHandler ConnectionReestablished;

        void Send(string message);
    }
}