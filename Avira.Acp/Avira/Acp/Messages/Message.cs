using System.Globalization;
using System.Net;
using System.Runtime.Serialization;
using Avira.Acp.Common;

namespace Avira.Acp.Messages
{
    [DataContract]
    public class Message
    {
        internal static class HttpStatusCodeSerializer
        {
            public static HttpStatusCode? DeserealizeFromJson(string json)
            {
                if (!int.TryParse(json, out var result))
                {
                    return null;
                }

                return (HttpStatusCode)result;
            }

            public static string SerializeToJson(HttpStatusCode? httpStatusCode)
            {
                if (httpStatusCode.HasValue)
                {
                    return ((int)httpStatusCode.Value).ToString(CultureInfo.InvariantCulture);
                }

                return null;
            }
        }

        [DataMember(Name = "acp")] public string Acp { get; set; }

        [DataMember(Name = "id")] public string Id { get; set; }

        [DataMember(Name = "status_code")] public HttpStatusCode? StatusCode { get; set; }

        [DataMember(Name = "sender")] public string Sender { get; set; }

        [DataMember(Name = "path")] public string Path { get; set; }

        [DataMember(Name = "host")] public string Host { get; set; }

        [DataMember(Name = "verb")] public string Verb { get; set; }

        [DataMember(Name = "headers")] public HeaderCollection Headers { get; set; }

        [DataMember(Name = "payload")] public JsonObjectWrapper PayloadData { get; set; }

        public string Payload
        {
            get { return PayloadData?.ToString(); }
            set { PayloadData = ((value == null) ? null : new JsonObjectWrapper(value)); }
        }

        public Message()
        {
            Headers = new HeaderCollection(null);
        }

        public bool IsRequest()
        {
            if (!string.IsNullOrEmpty(Host) && !string.IsNullOrEmpty(Verb) && !string.IsNullOrEmpty(Path) &&
                !string.IsNullOrEmpty(Id) && string.IsNullOrEmpty(Sender))
            {
                return !StatusCode.HasValue;
            }

            return false;
        }

        public bool IsResponse()
        {
            if (StatusCode.HasValue && !string.IsNullOrEmpty(Id) && string.IsNullOrEmpty(Host) &&
                string.IsNullOrEmpty(Path) && string.IsNullOrEmpty(Sender))
            {
                return string.IsNullOrEmpty(Verb);
            }

            return false;
        }

        public bool IsNotification()
        {
            if (string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(Sender) && !string.IsNullOrEmpty(Path) &&
                !string.IsNullOrEmpty(Verb) && string.IsNullOrEmpty(Host))
            {
                return !StatusCode.HasValue;
            }

            return false;
        }

        internal static Message Clone(Message source)
        {
            string json = AcpMessageSerializer.Instance.SerializeToJson(source);
            return AcpMessageSerializer.Instance.DeserializeFromJson(json);
        }
    }
}