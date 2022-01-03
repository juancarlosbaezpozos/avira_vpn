using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Serilog;

namespace Avira.Messaging
{
    public class Router
    {
        private ConcurrentDictionary<string, IService> routes = new ConcurrentDictionary<string, IService>();

        private IMessenger messenger;

        private ConcurrentDictionary<IMessenger, Action<Message>> subscribers =
            new ConcurrentDictionary<IMessenger, Action<Message>>();

        public Router(IMessenger messenger)
        {
            this.messenger = messenger;
            this.messenger.MessageReceived += MessengerOnMessageReceived;
        }

        private void MessengerOnMessageReceived(object sender, EventArgs<string> messageReceivedEvent)
        {
            Envelope envelope = Envelope.Unpack(messageReceivedEvent.Value);
            IMessenger responseMessenger = (IMessenger)sender;
            bool flag = false;
            if (envelope.IsSubscription())
            {
                if (!subscribers.TryGetValue(responseMessenger, out var callback))
                {
                    callback = delegate(Message m) { SendMessage(responseMessenger, m); };
                    subscribers[responseMessenger] = callback;
                }

                CallService(envelope, delegate(IService service) { service.Subscribe(envelope.Subscribe, callback); });
                flag = true;
            }
            else if (envelope.IsUnsubscription())
            {
                if (subscribers.TryGetValue(responseMessenger, out var callback2))
                {
                    CallService(envelope,
                        delegate(IService service) { service.Unsubscribe(envelope.Unsubscribe, callback2); });
                }

                flag = true;
            }
            else if (envelope.Message != null && envelope.Message.MessageType == MessageType.Request)
            {
                flag = CallService(envelope,
                    delegate(IService service)
                    {
                        service.Request(envelope.Message, delegate(Message m) { SendMessage(responseMessenger, m); },
                            null);
                    });
            }

            if (!flag)
            {
                string message =
                    Envelope.Pack(Message.CreateFailedResponse(envelope.Message, JsonRpcErrors.MethodNotFound),
                        string.Empty);
                responseMessenger.Send(message);
            }
        }

        public void SendMessage(IMessenger messenger, Message message)
        {
            try
            {
                messenger.Send(Envelope.Pack(message, string.Empty));
            }
            catch (Exception exception)
            {
                Log.Error(exception, $"Failed to send message: {message}");
            }
        }

        private bool CallService(Envelope envelope, Action<IService> serviceAction)
        {
            bool result = false;
            foreach (KeyValuePair<string, IService> route in routes)
            {
                if (envelope.Message != null && envelope.Message.Method.StartsWith(route.Key, StringComparison.Ordinal))
                {
                    serviceAction(route.Value);
                    result = true;
                }
                else if (envelope.IsSubscription() &&
                         envelope.Subscribe.StartsWith(route.Key, StringComparison.Ordinal))
                {
                    serviceAction(route.Value);
                    result = true;
                }
                else if (envelope.IsUnsubscription() &&
                         envelope.Unsubscribe.StartsWith(route.Key, StringComparison.Ordinal))
                {
                    serviceAction(route.Value);
                    result = true;
                }
            }

            return result;
        }

        public void AddRoute(string testRoute, IService service)
        {
            routes[testRoute] = service;
        }

        public void AddAllRoutes(ReflectionService reflectionService)
        {
            foreach (string route in reflectionService.GetRoutes())
            {
                AddRoute(route, reflectionService);
            }
        }
    }
}