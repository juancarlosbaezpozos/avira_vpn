using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp.Messages
{
    public class Notification
    {
        internal readonly Message Message;

        public string Acp
        {
            get { return Message.Acp; }
            set { Message.Acp = value; }
        }

        public string Sender
        {
            get { return Message.Sender; }
            set { Message.Sender = value; }
        }

        public string Path
        {
            get { return Message.Path; }
            set { Message.Path = value; }
        }

        public string Verb
        {
            get { return Message.Verb; }
            set { Message.Verb = value; }
        }

        public IHeaderCollection Headers => Message.Headers;

        internal Notification()
            : this(new Message())
        {
        }

        public Notification(Message message)
        {
            Message = message;
        }

        public static Notification Clone(Notification source)
        {
            return new Notification(Message.Clone(source.GetAcpMessage()));
        }

        public static Notification Create<T>(string verb, string path, string sender, SingleResourceDocument<T> payload)
            where T : class
        {
            return new Notification<T>
            {
                Acp = "1.0",
                Verb = verb,
                Path = path,
                Sender = sender,
                Payload = payload
            };
        }

        public static Notification CreateCollection<T>(string verb, string path, string sender,
            CollectionResourceDocument<T> payload) where T : class
        {
            return new CollectionNotification<T>
            {
                Acp = "1.0",
                Verb = verb,
                Path = path,
                Sender = sender,
                Payload = payload
            };
        }

        public string GetRawPayload()
        {
            return GetAcpMessage().Payload;
        }

        internal virtual Message GetAcpMessage()
        {
            return Message;
        }

        public override string ToString()
        {
            return AcpMessageSerializer.Instance.SerializeToJson(this);
        }
    }
}