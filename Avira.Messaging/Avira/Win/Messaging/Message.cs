using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.Win.Messaging
{
    public class Message
    {
        public bool HiddenParams;

        public string Method { get; set; }

        public int Id { get; set; }

        public JToken Params { get; set; }

        public JToken Result { get; set; }

        public JToken Error { get; set; }

        public MessageType MessageType { get; set; }

        protected static int GenerateId()
        {
            return Environment.TickCount;
        }

        public static Message CreateRequest(string method)
        {
            return new Message
            {
                Method = method,
                MessageType = MessageType.Request,
                Id = GenerateId()
            };
        }

        public static Message CreateResponse(Message request, JToken result)
        {
            return new Message
            {
                MessageType = MessageType.Response,
                Id = request.Id,
                Result = result
            };
        }

        public static Message CreateResponse(Message request, JValue result)
        {
            return new Message
            {
                MessageType = MessageType.Response,
                Id = request.Id,
                Result = result
            };
        }

        public static Message CreateResponse<T>(Message request, T result)
        {
            return new Message
            {
                MessageType = MessageType.Response,
                Id = request.Id,
                Result = ToJObject(result)
            };
        }

        public static Message CreateResponse(Message request, string result)
        {
            return new Message
            {
                MessageType = MessageType.Response,
                Id = request.Id,
                Result = (JToken)result
            };
        }

        public static Message CreateFailedResponse(Message request, JsonRpcErrors errorCode)
        {
            return CreateFailedResponse(request, errorCode, errorCode.ToString());
        }

        public static Message CreateFailedResponse(Message request, JsonRpcErrors errorCode, string errorMessage)
        {
            JObject error = new JObject
            {
                ["code"] = (JToken)(int)errorCode,
                ["message"] = (JToken)errorMessage
            };
            return new Message
            {
                Method = (request?.Method ?? string.Empty),
                MessageType = MessageType.Response,
                Id = (request?.Id ?? 0),
                Error = error
            };
        }

        public static Message CreateNotification(string notification)
        {
            return new Message
            {
                Method = notification,
                MessageType = MessageType.Notification,
                Id = 0
            };
        }

        public override string ToString()
        {
            return MessageSerializer.Serialize(this);
        }

        public static JToken ToJObject<T>(T data)
        {
            string value = JsonConvert.SerializeObject(data);
            if (data is string)
            {
                return (JToken)data.ToString();
            }

            return JsonConvert.DeserializeObject<JToken>(value);
        }

        public static object ToType(Type t, JToken data)
        {
            if (t == typeof(string))
            {
                return data.ToString();
            }

            return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(data), t);
        }
    }
}