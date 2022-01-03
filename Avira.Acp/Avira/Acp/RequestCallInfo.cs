using Avira.Acp.Messages;

namespace Avira.Acp
{
    public class RequestCallInfo
    {
        public ResourceLocation ResourceLocation { get; set; }

        public Request Request { get; set; }

        public ResponseHandler ResponseHandler { get; set; }

        public RequestCallInfo(ResourceLocation resourceLocation, Request request, ResponseHandler responseHandler)
        {
            ResourceLocation = resourceLocation;
            Request = request;
            ResponseHandler = responseHandler;
        }
    }
}