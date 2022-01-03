using ServiceStack.Text;

namespace Avira.Acp.Messages.JsonApi
{
    public class Request<T> : Request
    {
        public SingleResourceDocument<T> Payload { get; set; }

        public T PayloadDataAttributes
        {
            get
            {
                if (Payload?.Data == null || Payload.Data.Attributes == null)
                {
                    return default(T);
                }

                return Payload.Data.Attributes;
            }
        }

        public Resource<T> PayloadData => Payload?.Data;

        internal Request()
        {
        }

        public Request(Message message)
            : base(message)
        {
            Payload = JsonSerializer.DeserializeFromString<SingleResourceDocument<T>>(message.Payload);
        }

        public static Request<T> ConvertFrom(Request request)
        {
            Request<T> request2 = request as Request<T>;
            if (request2 == null)
            {
                request2 = new Request<T>(request.GetAcpMessage());
            }

            return request2;
        }

        internal override Message GetAcpMessage()
        {
            Message.Payload = JsonSerializer.SerializeToString(Payload);
            return Message;
        }
    }
}