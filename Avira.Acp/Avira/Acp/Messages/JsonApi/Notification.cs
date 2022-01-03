using ServiceStack.Text;

namespace Avira.Acp.Messages.JsonApi
{
    public class Notification<T> : Notification
    {
        public SingleResourceDocument<T> Payload { get; set; }

        internal Notification()
        {
        }

        public Notification(Message message)
            : base(message)
        {
            Payload = JsonSerializer.DeserializeFromString<SingleResourceDocument<T>>(message.Payload);
        }

        public static Notification<T> ConvertFrom(Notification notification)
        {
            Notification<T> notification2 = notification as Notification<T>;
            if (notification2 == null)
            {
                notification2 = new Notification<T>(notification.GetAcpMessage());
            }

            return notification2;
        }

        internal override Message GetAcpMessage()
        {
            Message.Payload = JsonSerializer.SerializeToString(Payload);
            return Message;
        }
    }
}