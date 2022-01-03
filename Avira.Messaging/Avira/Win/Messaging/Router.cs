using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Serilog;

namespace Avira.Win.Messaging
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

        private void MessengerOnMessageReceived(object sender, MessageReceivedEvent messageReceivedEvent)
        {
            Envelope envelope = Envelope.Unpack(messageReceivedEvent.Message);
            IMessenger responseMessenger = (IMessenger)sender;
            bool flag = false;
            if (envelope.IsSubscription())
            {
                if (!subscribers.TryGetValue(responseMessenger, out var callback))
                {
                    callback = delegate(Message m) { SendNotification(responseMessenger, m, envelope.Subscribe); };
                    subscribers[responseMessenger] = callback;
                }

                CallService(envelope, delegate(IService service) { service.Subscribe(envelope.Subscribe, callback); });
                flag = true;
            }
            else if (envelope.IsUnsubscription())
            {
                Unsubscribe(envelope, responseMessenger);
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

        private void Unsubscribe(Envelope envelope, IMessenger responseMessenger)
        {
            if (subscribers.TryGetValue(responseMessenger, out var callback))
            {
                CallService(envelope,
                    delegate(IService service) { service.Unsubscribe(envelope.Unsubscribe, callback); });
            }
        }

        public void SendNotification(IMessenger messenger, Message message, string service)
        {
            try
            {
                messenger.Send(Envelope.Pack(message, string.Empty));
            }
            catch (Exception ex)
            {
                if (ex is MessengerClosedException || ex is IOException)
                {
                    Log.Warning(ex, $"Could not send notification (pipe closed): {message}.");
                    Envelope envelope = Envelope.Unpack(Envelope.CreateUnsubscribeMessage(message.Method, service));
                    Unsubscribe(envelope, messenger);
                }
                else
                {
                    Log.Error(ex, $"Could not send notification: {message}.");
                }
            }
        }

        public void SendMessage(IMessenger messenger, Message message)
        {
            try
            {
                messenger.Send(Envelope.Pack(message, string.Empty));
            }
            catch (Exception ex)
            {
                if (ex is MessengerClosedException || ex is IOException)
                {
                    Log.Warning(ex, $"Failed to send message (pipe closed) : {message}.");
                }
                else
                {
                    Log.Error(ex, $"Failed to send message: {message}.");
                }
            }
        }

        private bool CallService(Envelope envelope, Action<IService> serviceAction)
        {
            bool result = false;
            foreach (KeyValuePair<string, IService> route in routes)
            {
                if (envelope.Message != null && envelope.Message.Method.StartsWith(route.Key))
                {
                    serviceAction(route.Value);
                    result = true;
                }
                else if (envelope.IsSubscription() && envelope.Subscribe.StartsWith(route.Key))
                {
                    serviceAction(route.Value);
                    result = true;
                }
                else if (envelope.IsUnsubscription() && envelope.Unsubscribe.StartsWith(route.Key))
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