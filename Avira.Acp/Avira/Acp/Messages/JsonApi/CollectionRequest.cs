using ServiceStack.Text;

namespace Avira.Acp.Messages.JsonApi
{
    public class CollectionRequest<T> : Request
    {
        public CollectionResourceDocument<T> Payload { get; set; }

        internal CollectionRequest()
        {
        }

        public CollectionRequest(Message message)
            : base(message)
        {
            Payload = JsonSerializer.DeserializeFromString<CollectionResourceDocument<T>>(message.Payload);
        }

        public static CollectionRequest<T> ConvertFrom(Request request)
        {
            CollectionRequest<T> collectionRequest = request as CollectionRequest<T>;
            if (collectionRequest == null)
            {
                collectionRequest = new CollectionRequest<T>(request.GetAcpMessage());
            }

            return collectionRequest;
        }

        internal override Message GetAcpMessage()
        {
            Message.Payload = JsonSerializer.SerializeToString(Payload);
            return Message;
        }
    }
}