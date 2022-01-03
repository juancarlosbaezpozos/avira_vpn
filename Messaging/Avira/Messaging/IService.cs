using System;

namespace Avira.Messaging
{
    public interface IService
    {
        void Request(Message message, Action<Message> onResponse, Action<Message> onError);

        void Subscribe(string messageCommand, Action<Message> onMessage);

        void Unsubscribe(string messageCommand, Action<Message> onMessage);
    }
}