using Avira.Acp.Messages;

namespace Avira.Acp
{
    internal interface IResponseMessageHandler
    {
        Request RegisterRequestMessage(Request request, ResponseHandler responseHandler);

        void HandleResponse(Response response);

        Request GetRequest(Response response);
    }
}