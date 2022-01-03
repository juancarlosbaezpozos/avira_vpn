using System.Collections.Generic;
using Avira.Acp.Extensions;

namespace Avira.Acp
{
    public class QueueUnhandledRequestsBehaviour : IResourceNotFoundBehaviour
    {
        private readonly IAcpMessageBroker messageBroker;

        private readonly List<RequestCallInfo> queuedRequests = new List<RequestCallInfo>();

        public QueueUnhandledRequestsBehaviour(IAcpMessageBroker messageBroker)
        {
            this.messageBroker = messageBroker;
        }

        public void OnResourceRegistered(ResourceLocation resourceLocation, RequestHandler requestHandler)
        {
            ProcessQueuedRequests(GetQueuedRequests(resourceLocation));
        }

        public void ProcessUnhandledRequest(RequestCallInfo requestCallInfo)
        {
            lock (queuedRequests)
            {
                queuedRequests.Add(requestCallInfo);
            }
        }

        public List<RequestCallInfo> GetUnprocessedRequests()
        {
            return queuedRequests;
        }

        private List<RequestCallInfo> GetQueuedRequests(ResourceLocation resourceLocation)
        {
            lock (queuedRequests)
            {
                return queuedRequests.MoveAll((RequestCallInfo q) => resourceLocation.CheckMatch(q.ResourceLocation));
            }
        }

        private void ProcessQueuedRequests(List<RequestCallInfo> requests)
        {
            requests.ForEach(delegate(RequestCallInfo r)
            {
                messageBroker.DispatchRequest(r.Request, r.ResponseHandler);
            });
        }
    }
}