using Avira.Acp.Messages.JsonApi;
using ServiceStack.Text;

namespace Avira.Acp.Messages
{
    public class Request
    {
        protected readonly Message Message;

        public string Acp
        {
            get { return Message.Acp; }
            set { Message.Acp = value; }
        }

        public string Host
        {
            get { return Message.Host; }
            set { Message.Host = value; }
        }

        public string Verb
        {
            get { return Message.Verb; }
            set { Message.Verb = value; }
        }

        public string Path
        {
            get { return Message.Path; }
            set { Message.Path = value; }
        }

        public string Id
        {
            get { return Message.Id; }
            set { Message.Id = value; }
        }

        public ResourceLocation ResourceLocation => new ResourceLocation
        {
            Host = Message.Host,
            Path = Message.Path
        };

        public IHeaderCollection Headers => Message.Headers;

        internal Request()
            : this(new Message())
        {
        }

        public Request(Message message)
        {
            Message = message;
        }

        internal virtual Message GetAcpMessage()
        {
            return Message;
        }

        public static Request Clone(Request source)
        {
            return new Request(Message.Clone(source.GetAcpMessage()));
        }

        public static Request Create(string verb, string host, string path)
        {
            return new Request
            {
                Acp = "1.0",
                Id = UniqueIdProvider.Get(),
                Verb = verb,
                Host = host,
                Path = path
            };
        }

        public static Request Create<T>(string verb, string host, string path, T payload)
        {
            return new Request(new Message
            {
                Payload = payload.ToJson()
            })
            {
                Acp = "1.0",
                Id = UniqueIdProvider.Get(),
                Verb = verb,
                Host = host,
                Path = path
            };
        }

        public static Request<T> Create<T>(string verb, string host, string path, Resource<T> payloadData)
        {
            return new Request<T>
            {
                Acp = "1.0",
                Id = UniqueIdProvider.Get(),
                Verb = verb,
                Host = host,
                Path = path,
                Payload = new SingleResourceDocument<T>
                {
                    Data = payloadData
                }
            };
        }

        public Resource<T> GetPayloadData<T>(Request request) where T : class
        {
            return (request as Request<T>)?.Payload?.Data;
        }

        public string GetRawPayload()
        {
            return GetAcpMessage().Payload;
        }

        public override string ToString()
        {
            return AcpMessageSerializer.Instance.SerializeToJson(this);
        }
    }
}