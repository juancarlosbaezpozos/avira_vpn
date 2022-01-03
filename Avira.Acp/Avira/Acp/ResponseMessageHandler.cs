using System.Collections.Generic;
using Avira.Acp.Messages;

namespace Avira.Acp
{
    internal class ResponseMessageHandler : IResponseMessageHandler
    {
        private readonly Dictionary<string, RegisteredResponseEntry> responseHandlers =
            new Dictionary<string, RegisteredResponseEntry>();

        public Request RegisterRequestMessage(Request request, ResponseHandler responseHandler)
        {
            string text = UniqueIdProvider.Get();
            lock (responseHandlers)
            {
                RegisteredResponseEntry value = new RegisteredResponseEntry(request.Id, request, responseHandler);
                responseHandlers[text] = value;
                request.Id = text;
                return request;
            }
        }

        public Request GetRequest(Response response)
        {
            lock (responseHandlers)
            {
                if (responseHandlers.TryGetValue(response.Id, out var value))
                {
                    return value.Request;
                }
            }

            return null;
        }

        public void HandleResponse(Response response)
        {
            RegisteredResponseEntry value;
            lock (responseHandlers)
            {
                if (responseHandlers.TryGetValue(response.Id, out value))
                {
                    responseHandlers.Remove(response.Id);
                }
            }

            if (value != null)
            {
                response.Id = value.ExternalId;
                value.Handle(response);
            }
        }
    }
}