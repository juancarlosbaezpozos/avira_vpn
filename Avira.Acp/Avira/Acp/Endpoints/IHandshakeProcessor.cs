using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp.Endpoints
{
    public interface IHandshakeProcessor
    {
        HandshakeResult ProcessRequest(Request<HandshakeRequestData> handshakeRequest, string endpointId);

        Request<HandshakeRequestData>
            CreateHandshakeRequest(string localHost, string remoteHost, string clientPipeName);

        void ProcessResponse(Response handshakeResponse);
    }
}