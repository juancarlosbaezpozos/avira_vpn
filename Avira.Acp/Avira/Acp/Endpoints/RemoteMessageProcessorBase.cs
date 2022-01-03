using System;
using Avira.Acp.Logging;
using Avira.Acp.Messages;

namespace Avira.Acp.Endpoints
{
    public abstract class RemoteMessageProcessorBase : IRemoteMessageProcessor, IDisposable
    {
        private readonly ILogger logger = LoggerFacade.GetCurrentClassLogger();

        private readonly string ownViaHeaderValue;

        protected readonly IAcpMessageBroker MessageBroker;

        protected readonly IRemoteResourceRegistrator RemoteResourceRegistrator;

        protected RemoteMessageProcessorBase(IAcpMessageBroker messageBroker,
            IRemoteResourceRegistrator remoteResourceRegistrator, string localHost)
        {
            MessageBroker = messageBroker;
            RemoteResourceRegistrator = remoteResourceRegistrator;
            ownViaHeaderValue = "acp/1.0 " + localHost;
        }

        public void ProcessMessage(Message acpMessage, ResponseHandler responseHandler)
        {
            if (acpMessage != null && !acpMessage.Headers.Contains("Via", ownViaHeaderValue))
            {
                if (acpMessage.IsRequest())
                {
                    HandleRequest(new Request(acpMessage), responseHandler);
                    return;
                }

                if (acpMessage.IsResponse())
                {
                    HandleResponse(new Response(acpMessage));
                    return;
                }

                if (acpMessage.IsNotification())
                {
                    HandleNotification(new Notification(acpMessage));
                    return;
                }

                logger.Warn("Received unknown message for id: '{0}'", acpMessage.Id);
            }
        }

        public virtual void UnregisterRemoteResources()
        {
            UnregisterRemoteResources(string.Empty);
        }

        protected void UnregisterRemoteResources(string owner)
        {
            RemoteResourceRegistrator.RemoveAllResourceRegistrations(owner);
        }

        public abstract void Initialize(string newRemoteHost, IAdapter newAdapter);

        protected abstract void HandleResponse(Response response);

        protected abstract void HandleNotification(Notification notification);

        protected abstract void HandleRequest(Request request, ResponseHandler responseHandler);

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnregisterRemoteResources();
            }
        }
    }
}