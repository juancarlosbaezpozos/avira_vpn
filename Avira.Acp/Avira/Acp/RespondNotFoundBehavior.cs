using System.Collections.Generic;
using System.Net;
using Avira.Acp.Messages;

namespace Avira.Acp
{
    public class RespondNotFoundBehavior : IResourceNotFoundBehaviour
    {
        public void OnResourceRegistered(ResourceLocation resourceLocation, RequestHandler requestHandler)
        {
        }

        public void ProcessUnhandledRequest(RequestCallInfo requestCallInfo)
        {
            requestCallInfo.ResponseHandler(Response.Create(requestCallInfo.Request.Id, HttpStatusCode.NotFound));
        }

        public List<RequestCallInfo> GetUnprocessedRequests()
        {
            return new List<RequestCallInfo>();
        }
    }
}