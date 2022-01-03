using System.Collections.Generic;

namespace Avira.Acp
{
    public interface IResourceNotFoundBehaviour
    {
        void OnResourceRegistered(ResourceLocation resourceLocation, RequestHandler requestHandler);

        void ProcessUnhandledRequest(RequestCallInfo requestCallInfo);

        List<RequestCallInfo> GetUnprocessedRequests();
    }
}