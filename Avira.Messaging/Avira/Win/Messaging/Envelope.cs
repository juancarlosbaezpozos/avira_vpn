using Newtonsoft.Json.Linq;

namespace Avira.Win.Messaging
{
    public class Envelope
    {
        public Message Message { get; set; }

        public string ServiceName { get; set; }

        public string Subscribe { get; set; }

        public string Unsubscribe { get; set; }

        public static string Pack(Message message, string serviceName)
        {
            return new JObject
            {
                ["message"] = MessageSerializer.ToJObject(message),
                ["service"] = (JToken)serviceName
            }.ToString();
        }

        public static Envelope Unpack(string message)
        {
            JObject jObject = JObject.Parse(message);
            JToken jToken = jObject["message"];
            Message message2 = null;
            if (jToken != null)
            {
                message2 = MessageSerializer.FromJObject((JObject)jToken);
            }

            return new Envelope
            {
                Message = message2,
                ServiceName = jObject["service"]!.ToString(),
                Subscribe = ((jObject["subscribe"] == null) ? string.Empty : jObject["subscribe"]!.ToString()),
                Unsubscribe = ((jObject["unsubscribe"] == null) ? string.Empty : jObject["unsubscribe"]!.ToString())
            };
        }

        public static string CreateSubscribeMessage(string messageCommand, string serviceName)
        {
            return new JObject
            {
                ["service"] = (JToken)serviceName,
                ["subscribe"] = (JToken)messageCommand
            }.ToString();
        }

        public static string CreateUnsubscribeMessage(string messageCommand, string serviceName)
        {
            return new JObject
            {
                ["service"] = (JToken)serviceName,
                ["unsubscribe"] = (JToken)messageCommand
            }.ToString();
        }

        public bool IsSubscription()
        {
            return !string.IsNullOrEmpty(Subscribe);
        }

        public bool IsUnsubscription()
        {
            return !string.IsNullOrEmpty(Unsubscribe);
        }
    }
}