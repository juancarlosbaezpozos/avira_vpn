using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Avira.Messaging
{
    public class MessageSerializer
    {
        public static Message FromJObject(JObject message)
        {
            Message message2 = new Message();
            if (message.Value<int?>("id")!.HasValue)
            {
                message2.Id = message["id"].Value<int>();
            }

            if (message.SelectToken("method") != null)
            {
                message2.Method = message["method"]!.ToString();
            }

            if (message.SelectToken("result") != null)
            {
                message2.Result = message["result"];
                message2.MessageType = MessageType.Response;
            }

            if (message.SelectToken("error") != null)
            {
                message2.Error = message["error"];
                message2.MessageType = MessageType.Response;
            }

            if (message.SelectToken("params") != null)
            {
                message2.Params = message["params"];
                message2.MessageType = ((message2.Id == 0) ? MessageType.Notification : MessageType.Request);
            }

            if (message.SelectToken("hiddenParams") != null)
            {
                message2.HiddenParams = Convert.ToBoolean(message["hiddenParams"]);
            }

            return message2;
        }

        public static JObject ToJObject(Message message)
        {
            JObject jObject = new JObject();
            jObject["jsonrpc"] = (JToken)"2.0";
            switch (message.MessageType)
            {
                case MessageType.Request:
                    jObject["method"] = (JToken)message.Method;
                    jObject["id"] = (JToken)message.Id;
                    jObject["params"] = message.Params ?? message.Params;
                    jObject["hiddenParams"] = (JToken)message.HiddenParams;
                    break;
                case MessageType.Response:
                    jObject["id"] = (JToken)message.Id;
                    jObject["result"] = message.Result ?? message.Result;
                    jObject["error"] = message.Error ?? message.Error;
                    break;
                case MessageType.Notification:
                    jObject["method"] = (JToken)message.Method;
                    jObject["params"] = message.Params;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return jObject;
        }

        public static string Serialize(Message message)
        {
            return Regex.Replace(ToJObject(message).ToString(), "([\\r\\n\\t ])+", string.Empty);
        }
    }
}