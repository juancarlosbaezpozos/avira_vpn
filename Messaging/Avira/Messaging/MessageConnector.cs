using System;

namespace Avira.Messaging
{
    public sealed class MessageConnector : IDisposable
    {
        internal class ForwardMessenger : IMessenger, IDisposable
        {
            public ForwardMessenger Receiver { get; set; }

            public event EventHandler<EventArgs<string>> MessageReceived;

            public event EventHandler ConnectionReestablished;

            public void InvokeMessageReceived(string message)
            {
                this.MessageReceived?.Invoke(this, new EventArgs<string>(message));
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

        public void Dispose()
        {
            if (source != null)
            {
                source.Dispose();
            }

            if (destination != null)
            {
                destination.Dispose();
            }
        }
    }
}