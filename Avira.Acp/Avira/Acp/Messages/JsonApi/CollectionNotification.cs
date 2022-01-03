using ServiceStack.Text;

namespace Avira.Acp.Messages.JsonApi
{
    public class CollectionNotification<T> : Notification
    {
        public CollectionResourceDocument<T> Payload { get; set; }

        internal CollectionNotification()
        {
        }

        public CollectionNotification(Message message)
            : base(message)
        {
            Payload = JsonSerializer.DeserializeFromString<CollectionResourceDocument<T>>(message.Payload);
        }

        public static CollectionNotification<T> ConvertFrom(Notification notification)
        {
            CollectionNotification<T> collectionNotification = notification as CollectionNotification<T>;
            if (collectionNotification == null)
            {
                collectionNotification = new CollectionNotification<T>(notification.GetAcpMessage());
            }

            return collectionNotification;
        }

        internal override Message GetAcpMessage()
        {
            Message.Payload = JsonSerializer.SerializeToString(Payload);
            return Message;
        }
    }
}