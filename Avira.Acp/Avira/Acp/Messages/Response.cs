using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using Avira.Acp.Messages.JsonApi;
using ServiceStack.Text;

namespace Avira.Acp.Messages
{
    public class Response
    {
        protected readonly Message Message;

        public string Acp
        {
            get { return Message.Acp; }
            set { Message.Acp = value; }
        }

        public string Id
        {
            get { return Message.Id; }
            set { Message.Id = value; }
        }

        public HttpStatusCode StatusCode
        {
            get { return Message.StatusCode.Value; }
            set { Message.StatusCode = value; }
        }

        [DataMember(Name = "payload")]
        public JsonObject PayloadData => JsonSerializer.DeserializeFromString<JsonObject>(Message.Payload);

        public IHeaderCollection Headers => Message.Headers;

        internal Response()
            : this(new Message())
        {
        }

        public Response(Message message)
        {
            Message = message;
        }

        internal virtual Message GetAcpMessage()
        {
            return Message;
        }

        public static Response Clone(Response source)
        {
            return new Response(Message.Clone(source.GetAcpMessage()));
        }

        public static Response Create(string id, HttpStatusCode code)
        {
            return new Response
            {
                Acp = "1.0",
                Id = id,
                StatusCode = code
            };
        }

        public static Response Create<T>(string id, HttpStatusCode code, Resource<T> data)
        {
            return Create(id, code, new SingleResourceDocument<T>
            {
                Data = data
            });
        }

        public static Response Create<T>(string id, HttpStatusCode code, Resource<T> data,
            Dictionary<string, string> headerValues)
        {
            return Create(id, code, new SingleResourceDocument<T>
            {
                Data = data
            }, headerValues);
        }

        public static Response Create<T>(string id, HttpStatusCode code, SingleResourceDocument<T> data)
        {
            return Create(id, code, data, null);
        }

        public static Response Create<T>(string id, HttpStatusCode code, SingleResourceDocument<T> data,
            Dictionary<string, string> headerValues)
        {
            Response<T> response = new Response<T>
            {
                Acp = "1.0",
                Id = id,
                StatusCode = code,
                Payload = data
            };
            AddHeaderInformation(headerValues, response.Headers);
            return response;
        }

        public static CollectionResponse<T> CreateCollection<T>(string id, HttpStatusCode code,
            CollectionResourceDocument<T> data) where T : class
        {
            return CreateCollection(id, code, data, null);
        }

        public static CollectionResponse<T> CreateCollection<T>(string id, HttpStatusCode code, List<Resource<T>> data)
        {
            return CreateCollection(id, code, data, null);
        }

        public static CollectionResponse<T> CreateCollection<T>(string id, HttpStatusCode code, List<Resource<T>> data,
            Dictionary<string, string> headerValues)
        {
            return CreateCollection(id, code, new CollectionResourceDocument<T>
            {
                Data = data
            }, headerValues);
        }

        public static CollectionResponse<T> CreateCollection<T>(string id, HttpStatusCode code,
            CollectionResourceDocument<T> data, Dictionary<string, string> headerValues)
        {
            CollectionResponse<T> collectionResponse = new CollectionResponse<T>
            {
                Acp = "1.0",
                Id = id,
                StatusCode = code,
                Payload = data
            };
            AddHeaderInformation(headerValues, collectionResponse.Headers);
            return collectionResponse;
        }

        private static void AddHeaderInformation(Dictionary<string, string> headerValues, IHeaderCollection headers)
        {
            if (headerValues == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> headerValue in headerValues)
            {
                headers.Append(headerValue.Key, headerValue.Value);
            }
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