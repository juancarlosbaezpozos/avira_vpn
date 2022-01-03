using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Avira.Win.Messaging
{
    public class ServiceInterface : IService
    {
        private readonly IMessenger messenger;

        private readonly string serviceName;

        private readonly ConcurrentDictionary<int, Action<Message>> requests =
            new ConcurrentDictionary<int, Action<Message>>();

        private readonly ConcurrentDictionary<string, ConcurrentList<Action<Message>>> subscriptions =
            new ConcurrentDictionary<string, ConcurrentList<Action<Message>>>();

        public ServiceInterface(IMessenger messenger, string serviceName)
        {
            this.serviceName = serviceName;
            this.messenger = messenger;
            this.messenger.MessageReceived += Messenger_MessageReceived;
        }

        private void Messenger_MessageReceived(object sender, MessageReceivedEvent e)
        {
            Envelope envelope = Envelope.Unpack(e.Message);
            if (envelope.Message.MessageType == MessageType.Response)
            {
                if (requests.TryGetValue(envelope.Message.Id, out var value))
                {
                    value?.Invoke(envelope.Message);
                }
            }
            else if (envelope.Message.MessageType == MessageType.Notification)
            {
                DispatchNotification(envelope);
            }
        }

        private void DispatchNotification(Envelope envelope)
        {
            foreach (KeyValuePair<string, ConcurrentList<Action<Message>>> subscription in subscriptions)
            {
                if (envelope.Message.Method.StartsWith(subscription.Key) && subscription.Value != null)
                {
                    CallMessageHandlers(subscription.Value, envelope.Message);
                }
            }
        }

        private void CallMessageHandlers(ConcurrentList<Action<Message>> messageHandlers, Message message)
        {
            foreach (Action<Message> messageHandler in messageHandlers)
            {
                messageHandler(message);
            }
        }

        public void Request(Message message, Action<Message> onResponse, Action<Message> onError)
        {
            requests[message.Id] = onResponse;
            messenger.Send(Envelope.Pack(message, serviceName));
        }

        public void Subscribe(string messageCommand, Action<Message> onMessage)
        {
            if (!subscriptions.TryGetValue(messageCommand, out var value) || value == null)
            {
                value = new ConcurrentList<Action<Message>>();
                subscriptions[messageCommand] = value;
                string message = Envelope.CreateSubscribeMessage(messageCommand, serviceName);
                messenger.Send(message);
            }

            value.Add(onMessage);
        }

        public void Unsubscribe(string messageCommand, Action<Message> onMessage)
        {
            if (subscriptions.TryGetValue(messageCommand, out var value) && value != null)
            {
                value.Remove(onMessage);
                if (value.Count == 0)
                {
                    string message = Envelope.CreateUnsubscribeMessage(messageCommand, serviceName);
                    messenger.Send(message);
                }
            }
        }
    }
}