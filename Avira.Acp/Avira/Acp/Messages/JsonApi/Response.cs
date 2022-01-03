using ServiceStack.Text;

namespace Avira.Acp.Messages.JsonApi
{
    public class Response<T> : Response
    {
        public SingleResourceDocument<T> Payload { get; set; }

        internal Response()
        {
        }

        public Response(Message message)
            : base(message)
        {
            Payload = JsonSerializer.DeserializeFromString<SingleResourceDocument<T>>(message.Payload);
        }

        public static Response<T> ConvertFrom(Response response)
        {
            Response<T> response2 = response as Response<T>;
            if (response2 == null)
            {
                response2 = new Response<T>(response.GetAcpMessage());
            }

            return response2;
        }

        internal override Message GetAcpMessage()
        {
            Message.Payload = JsonSerializer.SerializeToString(Payload);
            return Message;
        }
    }
}