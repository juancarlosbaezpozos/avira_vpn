#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Avira.Acp.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Avira.Common.Acp.AppClient
{
    public class ResourceClient<T> : IResourceClient<T>, IDisposable where T : class
    {
        private IAcpCommunicator acpCommunicator;

        private List<string> subscriptions = new List<string>();

        private JsonSerializerSettings jsonErrorLogger;

        private string host;

        private string path;

        public ResourceClient(IAcpCommunicator acpCommunicator, string host, string path)
        {
            this.acpCommunicator = acpCommunicator;
            this.host = host;
            this.path = path;
            jsonErrorLogger = new JsonSerializerSettings
            {
                Error = delegate(object s, ErrorEventArgs e) { Trace.TraceWarning(e.ErrorContext?.Error?.ToString()); }
            };
        }

        public async Task<Tuple<T, HttpStatusCode?>> Get()
        {
            Response response = await acpCommunicator.GetRequest(host, path);
            Message message =
                response.GetType().GetMethod("GetAcpMessage", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(response, null) as Message;
            return new Tuple<T, HttpStatusCode?>(HasFailed(message) ? null : DeserializePayload(message?.Payload),
                message?.StatusCode);
        }

        public async Task<Tuple<T, HttpStatusCode?>> Post(string payload)
        {
            Response response = await acpCommunicator.PostRequest(host, path, payload);
            Message message =
                response.GetType().GetMethod("GetAcpMessage", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(response, null) as Message;
            return new Tuple<T, HttpStatusCode?>(HasFailed(message) ? null : DeserializePayload(message?.Payload),
                message?.StatusCode);
        }

        public virtual T DeserializePayload(string payload)
        {
            return JsonConvert.DeserializeObject<T>(payload, jsonErrorLogger);
        }

        public T GetData(Notification notification)
        {
            Message message = notification.GetType()
                .GetMethod("GetAcpMessage", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(notification, null) as Message;
            if (HasFailed(message))
            {
                Trace.TraceWarning($"Acp request failed: {message.StatusCode} - {message}");
                return null;
            }

            return DeserializePayload(message.Payload);
        }

        private static bool HasFailed(Message message)
        {
            if (message.StatusCode.HasValue)
            {
                return message.StatusCode != HttpStatusCode.OK;
            }

            return false;
        }

        public T GetData(Response response)
        {
            Message message =
                response.GetType().GetMethod("GetAcpMessage", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(response, null) as Message;
            if (HasFailed(message))
            {
                Trace.TraceWarning($"Acp request failed: {message.StatusCode} - {message}");
                return null;
            }

            return DeserializePayload(message.Payload);
        }

        public void Subscribe(Action<T> callback)
        {
            string item = acpCommunicator.Subscribe(host, path, delegate(Notification n) { callback(GetData(n)); });
            subscriptions.Add(item);
        }

        public void Dispose()
        {
            foreach (string subscription in subscriptions)
            {
                acpCommunicator.Unsubscribe(subscription);
            }
        }
    }
}