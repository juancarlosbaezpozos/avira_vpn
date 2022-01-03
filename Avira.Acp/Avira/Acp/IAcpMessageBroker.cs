using System.Collections.Generic;
using Avira.Acp.Messages;

namespace Avira.Acp
{
    public interface IAcpMessageBroker
    {
        string HostName { get; }

        void DispatchRequest<T>(Request message, ResponseHandler<T> responseHandler);

        void DispatchRequest<T>(Request message, CollectionResponseHandler<T> responseHandler);

        void DispatchRequest(Request message, ResponseHandler responseHandler);

        string RegisterResource<T>(ResourceLocation resourceLocation, CollectionRequestHandler<T> handler);

        string RegisterResource<T>(ResourceLocation resourceLocation, RequestHandler<T> requestHandler);

        string RegisterResource(ResourceLocation resourceLocation, RequestHandler requestHandler);

        string RegisterResource(ResourceLocation resourceLocation, RequestHandler requestHandlerstring, string owner);

        string RegisterResourceProviderSubstitute(ResourceLocation resourceLocation,
            IResourceProvider providerSubstitute);

        bool UnregisterResourceProviderSubstitute(ResourceLocation resourceLocation);

        bool UnregisterResource(string resourceId);

        bool UnregisterResource(string resourceId, string owner);

        string CreateSubscription<T>(ResourceLocation resourceLocation, NotificationHandler<T> notificationHandler);

        string CreateSubscription(ResourceLocation resourceLocation, NotificationHandler notificationHandler);

        string CreateSubscription(ResourceLocation resourceLocation, NotificationHandler notificationHandler,
            string owner);

        bool RemoveSubscription(string subscriptionId);

        bool RemoveSubscription(string subscriptionId, string owner);

        void HandleResponse(Response message);

        void DispatchNotification(Notification notification);

        void DispatchNotification(Notification notification, string excludedOwner);

        bool HasSubscribers(ResourceLocation resourceLocation);

        bool IsResourceRegistered(ResourceLocation resourceLocation);

        ICollection<ResourceLocation> GetRegisteredResourceLocations();
    }
}