using System;

namespace Avira.Win.Messaging
{
    public class Multiplexer : IMessenger, IDisposable
    {
        public event EventHandler<MessageReceivedEvent> MessageReceived;

        public event EventHandler ConnectionReestablished;

        public Multiplexer(IChannelConnectNotifier channelFactory)
        {
            channelFactory.PipeConnected += delegate(object sender, PipeConnectionArgs args)
            {
                args.Messenger.MessageReceived += delegate(object o, MessageReceivedEvent @event)
                {
                    this.MessageReceived?.Invoke(o, @event);
                };
                args.Messenger.ConnectionReestablished += delegate(object o, EventArgs @event)
                {
                    this.ConnectionReestablished?.Invoke(o, @event);
                };
            };
        }

        public void Send(string message)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}