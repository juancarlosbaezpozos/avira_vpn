using System;
using System.Collections.Concurrent;
using System.Threading;
using Serilog;

namespace Avira.Win.Messaging
{
    public class HostRouter : IMessenger, IDisposable
    {
        private class HostConnection
        {
            public string Host { get; set; }

            public IMessenger Messenger { get; set; }

            public Func<IMessenger> MessengerFactory { get; set; }
        }

        private ConcurrentDictionary<string, HostConnection> hosts = new();

        public event EventHandler<MessageReceivedEvent> MessageReceived;

        public event EventHandler ConnectionReestablished;

        public void AddConnection(string host, Func<IMessenger> messengerFactory)
        {
            HostConnection hostConnection = new HostConnection
            {
                Host = host,
                MessengerFactory = messengerFactory
            };
            hosts[hostConnection.Host] = hostConnection;
            if (hostConnection.Messenger != null)
            {
                hostConnection.Messenger.MessageReceived += delegate(object sender, MessageReceivedEvent @event)
                {
                    this.MessageReceived?.Invoke(this, @event);
                };
                hostConnection.Messenger.ConnectionReestablished += delegate(object sender, EventArgs @event)
                {
                    this.ConnectionReestablished?.Invoke(this, @event);
                };
            }
        }

        public void Send(string message)
        {
            Envelope envelope = Envelope.Unpack(message);
            if (!TryResolveHost(envelope.ServiceName, out var messenger))
            {
                NotifyBrokenConnection(message);
                return;
            }

            string propertyValue = GetRestrictedLog(envelope) ?? message;
            Log.Debug("ScriptiongObject::SendMessage: {0}", propertyValue);
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    messenger.Send(message);
                }
                catch (ObjectDisposedException)
                {
                    NotifyBrokenConnection(message);
                }
                catch (TimeoutException)
                {
                    NotifyBrokenConnection(message);
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "SendMessage failed.");
                }
            });
        }

        private string GetRestrictedLog(Envelope envelope)
        {
            string result = null;
            if (envelope.Message != null && envelope.Message.HiddenParams)
            {
                envelope.Message.Params = null;
                result = Envelope.Pack(envelope.Message, envelope.ServiceName);
            }

            return result;
        }

        private bool TryResolveHost(string host, out IMessenger messenger)
        {
            if (!hosts.TryGetValue(host, out var value))
            {
                throw new Exception("Could not resolve host " + host);
            }

            if (value.Messenger == null && value.MessengerFactory != null)
            {
                TryConnect(value);
            }

            messenger = value.Messenger;
            return messenger != null;
        }

        private void NotifyBrokenConnection(string message)
        {
            Log.Debug("Notifying that the messaging connection is broken!");
            Message message2 = Message.CreateFailedResponse(Envelope.Unpack(message).Message,
                JsonRpcErrors.ConnectionBroken, "Can't connect to the service.");
            this.MessageReceived?.Invoke(this, new MessageReceivedEvent
            {
                Message = Envelope.Pack(message2, string.Empty)
            });
        }

        private void TryConnect(HostConnection hostConnection)
        {
            try
            {
                hostConnection.Messenger = hostConnection.MessengerFactory();
                hostConnection.Messenger.MessageReceived += delegate(object sender, MessageReceivedEvent @event)
                {
                    this.MessageReceived?.Invoke(this, @event);
                };
                hostConnection.Messenger.ConnectionReestablished += delegate(object sender, EventArgs @event)
                {
                    this.ConnectionReestablished?.Invoke(this, @event);
                };
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to connect.");
            }
        }

        public void Dispose()
        {
            foreach (HostConnection value in hosts.Values)
            {
                if (value.Messenger != null)
                {
                    value.Messenger.Dispose();
                }
            }
        }
    }
}