using System.Net;
using Avira.Acp.Common;
using ServiceStack.Text;

namespace Avira.Acp.Messages
{
    public class AcpMessageSerializer
    {
        private static AcpMessageSerializer instance;

        public static AcpMessageSerializer Instance => instance ?? (instance = new AcpMessageSerializer());

        private AcpMessageSerializer()
        {
            JsConfig<JsonObjectWrapper>.RawSerializeFn = JsonObjectWrapper.SerializeToJson;
            JsConfig<JsonObjectWrapper>.RawDeserializeFn = JsonObjectWrapper.DeserializeFromJson;
            JsConfig<HttpStatusCode?>.RawSerializeFn = Message.HttpStatusCodeSerializer.SerializeToJson;
            JsConfig<HttpStatusCode?>.RawDeserializeFn = Message.HttpStatusCodeSerializer.DeserealizeFromJson;
            JsConfig<HeaderCollection>.RawSerializeFn = HeaderCollection.SerializeToJson;
            JsConfig<HeaderCollection>.RawDeserializeFn = HeaderCollection.DeserealizeFromJson;
        }

        internal string SerializeToJson(Message message)
        {
            return JsonSerializer.SerializeToString(message);
        }

        public string SerializeToJson(Request message)
        {
            return JsonSerializer.SerializeToString(message.GetAcpMessage());
        }

        public string SerializeToJson(Response message)
        {
            return JsonSerializer.SerializeToString(message.GetAcpMessage());
        }

        public string SerializeToJson(Notification message)
        {
            return JsonSerializer.SerializeToString(message.GetAcpMessage());
        }

        public Message DeserializeFromJson(string json)
        {
            return JsonSerializer.DeserializeFromString<Message>(json);
        }
    }
}