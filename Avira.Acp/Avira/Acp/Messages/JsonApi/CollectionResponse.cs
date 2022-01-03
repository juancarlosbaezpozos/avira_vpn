using ServiceStack.Text;

namespace Avira.Acp.Messages.JsonApi
{
    public class CollectionResponse<T> : Response
    {
        public CollectionResourceDocument<T> Payload { get; set; }

        internal CollectionResponse()
        {
        }

        public CollectionResponse(Message message)
            : base(message)
        {
            Payload = JsonSerializer.DeserializeFromString<CollectionResourceDocument<T>>(message.Payload);
        }

        public static CollectionResponse<T> ConvertFrom(Response response)
        {
            CollectionResponse<T> collectionResponse = response as CollectionResponse<T>;
            if (collectionResponse == null)
            {
                collectionResponse = new CollectionResponse<T>(response.GetAcpMessage());
            }

            return collectionResponse;
        }

        internal override Message GetAcpMessage()
        {
            Message.Payload = JsonSerializer.SerializeToString(Payload);
            return Message;
        }
    }
}