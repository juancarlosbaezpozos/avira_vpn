using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp.Endpoints
{
    public interface IRemoteResourceRegistrator
    {
        void RegisterResource(string remoteResourceId, ResourceLocation resourceLocation,
            RequestHandler requestHandler);

        void RegisterResource(string remoteResourceId, ResourceLocation resourceLocation, RequestHandler requestHandler,
            string owner);

        string CreateSubscription(string remoteSubscriptionId, ResourceLocation resourceLocation,
            NotificationHandler notificationHandler, string owner);

        void UnregisterResource(string remoteResourceId, string owner);

        void RemoveSubscription(string remoteSubscriptionId, string owner);

        void RemoveAllResourceRegistrations(string owner);

        void RemoveRemoteResourceLocations(CollectionResponse<ResourceLocation> response);

        void RemoveRemoteSubscriptionLocations(CollectionResponse<ResourceLocation> response);

        bool SubscriptionIsCreated(ResourceLocation resourceLocation);

        bool ResourceIsRegistered(ResourceLocation resourceLocation);

        bool ResourceIsRegistered(string localResourceId);
    }
}