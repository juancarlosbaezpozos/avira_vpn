using System;

namespace Avira.Win.Messaging
{
    public class MessageConnector
    {
        protected class ForwardMessenger : IMessenger, IDisposable
        {
            public ForwardMessenger Receiver { get; set; }

            public event EventHandler<MessageReceivedEvent> MessageReceived;

            public event EventHandler ConnectionReestablished;

            public void InvokeMessageReceived(string message)
            {
                this.MessageReceived?.Invoke(this, new MessageReceivedEvent
                {
                    Message = message
                });
            }

            public void Send(string message)
            {
                Receiver.InvokeMessageReceived(message);
            }

            protected virtual void OnConnectionReestablished()
            {
                this.ConnectionReestablished?.Invoke(this, EventArgs.Empty);
            }

            public void Dispose()
            {
            }
        }

        private readonly ForwardMessenger source;

        private readonly ForwardMessenger destination;

        public IMessenger Source => source;

        public IMessenger Destination => destination;

        public MessageConnector()
        {
            source = new ForwardMessenger();
            destination = new ForwardMessenger();
            source.Receiver = destination;
            destination.Receiver = source;
        }
    }
}