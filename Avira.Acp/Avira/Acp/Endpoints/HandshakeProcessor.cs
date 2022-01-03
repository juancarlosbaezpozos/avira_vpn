using System;
using System.Net;
using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp.Endpoints
{
    public class HandshakeProcessor : IHandshakeProcessor
    {
        public HandshakeResult ProcessRequest(Request<HandshakeRequestData> handshakeRequest, string endpointId)
        {
            Response response = Response.Create(handshakeRequest.Id, HttpStatusCode.Created, new Resource<string>
            {
                Attributes = null,
                Id = endpointId,
                Type = "endpoints"
            });
            return new HandshakeResult(RemovePipePrefix(handshakeRequest.PayloadDataAttributes.NamedPipeName),
                response);
        }

        public void ProcessResponse(Response handshakeResponse)
        {
            if (handshakeResponse.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception("Handshake failed: Endpoint was not created. " + handshakeResponse.PayloadData);
            }
        }

        public Request<HandshakeRequestData> CreateHandshakeRequest(string localHost, string remoteHost,
            string clientPipeName)
        {
            return Request.Create("POST", remoteHost, "/endpoints", new Resource<HandshakeRequestData>
            {
                Attributes = new HandshakeRequestData
                {
                    ChannelType = "named-pipe",
                    Host = localHost,
                    NamedPipeName = clientPipeName
                },
                Type = "endpoints"
            });
        }

        private string RemovePipePrefix(string pipeName)
        {
            if (pipeName.StartsWith("\\\\.\\pipe\\"))
            {
                pipeName = pipeName.Substring("\\\\.\\pipe\\".Length);
            }

            return pipeName;
        }
    }
}