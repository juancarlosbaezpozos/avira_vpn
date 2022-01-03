using System.Collections.Generic;
using System.Linq;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp.Endpoints
{
    public class RemoteResourceRegistrator : IRemoteResourceRegistrator
    {
        private readonly IAcpMessageBroker messageBroker;

        private readonly Dictionary<ResourceLocation, RemoteLocalIdPair> resourceLocations =
            new Dictionary<ResourceLocation, RemoteLocalIdPair>();

        private readonly Dictionary<ResourceLocation, RemoteLocalIdPair> subscriptions =
            new Dictionary<ResourceLocation, RemoteLocalIdPair>();

        private readonly object resourceLocationsLockObject = new object();

        private readonly object subscriptionsLockObject = new object();

        public RemoteResourceRegistrator(IAcpMessageBroker messageBroker)
        {
            this.messageBroker = messageBroker;
        }

        public void RegisterResource(string remoteResourceId, ResourceLocation resourceLocation,
            RequestHandler requestHandler)
        {
            RegisterResource(remoteResourceId, resourceLocation, requestHandler, string.Empty);
        }

        public void RegisterResource(string remoteResourceId, ResourceLocation resourceLocation,
            RequestHandler requestHandler, string owner)
        {
            lock (resourceLocationsLockObject)
            {
                if (!ResourceIsRegistered(resourceLocation) &&
                    RegisterResourceInternal(remoteResourceId, resourceLocation, requestHandler, owner))
                {
                    Unsubscribe(resourceLocation, owner);
                }
            }
        }

        public string CreateSubscription(string remoteSubscriptionId, ResourceLocation resourceLocation,
            NotificationHandler notificationHandler, string owner)
        {
            if (SubscriptionIsCreated(resourceLocation) || ResourceIsRegistered(resourceLocation))
            {
                return null;
            }

            lock (subscriptionsLockObject)
            {
                subscriptions[resourceLocation] = null;
            }

            string text = messageBroker.CreateSubscription(resourceLocation, notificationHandler, owner);
            lock (subscriptionsLockObject)
            {
                subscriptions[resourceLocation] = new RemoteLocalIdPair
                {
                    RemoteId = remoteSubscriptionId,
                    LocalId = text
                };
                return text;
            }
        }

        public void UnregisterResource(string remoteId, string owner)
        {
            lock (resourceLocationsLockObject)
            {
                ResourceLocation registrationResourceLocationByRemoteId =
                    GetRegistrationResourceLocationByRemoteId(remoteId);
                if (registrationResourceLocationByRemoteId != null)
                {
                    Unregister(registrationResourceLocationByRemoteId, owner);
                }
            }
        }

        public void RemoveSubscription(string remoteId, string owner)
        {
            lock (resourceLocationsLockObject)
            {
                ResourceLocation subscriptionResourceLocationByRemoteId =
                    GetSubscriptionResourceLocationByRemoteId(remoteId);
                if (subscriptionResourceLocationByRemoteId != null)
                {
                    Unsubscribe(subscriptionResourceLocationByRemoteId, owner);
                }
            }
        }

        public void RemoveAllResourceRegistrations(string owner)
        {
            Unsubscribe(new ResourceLocation(messageBroker.HostName, "/subscriptions"), owner);
            List<ResourceLocation> list;
            lock (subscriptionsLockObject)
            {
                list = subscriptions.Keys.ToList();
            }

            foreach (ResourceLocation item in list)
            {
                Unsubscribe(item, owner);
            }

            lock (resourceLocations)
            {
                foreach (ResourceLocation item2 in resourceLocations.Keys.ToList())
                {
                    Unregister(item2, owner);
                }
            }
        }

        public void RemoveRemoteResourceLocations(CollectionResponse<ResourceLocation> response)
        {
            lock (resourceLocationsLockObject)
            {
                Dictionary<ResourceLocation, RemoteLocalIdPair>.KeyCollection locations = resourceLocations.Keys;
                response.Payload.Data.RemoveAll((Resource<ResourceLocation> resource) =>
                    locations.Contains(resource.Attributes));
            }
        }

        public void RemoveRemoteSubscriptionLocations(CollectionResponse<ResourceLocation> response)
        {
            lock (resourceLocationsLockObject)
            {
                Dictionary<ResourceLocation, RemoteLocalIdPair>.KeyCollection locations = subscriptions.Keys;
                response.Payload.Data.RemoveAll((Resource<ResourceLocation> resource) =>
                    locations.Contains(resource.Attributes));
            }
        }

        public bool ResourceIsRegistered(ResourceLocation resourceLocation)
        {
            lock (resourceLocationsLockObject)
            {
                return resourceLocations.ContainsKey(resourceLocation);
            }
        }

        public bool ResourceIsRegistered(string localResourceId)
        {
            lock (resourceLocationsLockObject)
            {
                return resourceLocations.Any((KeyValuePair<ResourceLocation, RemoteLocalIdPair> r) =>
                    r.Value.LocalId.Equals(localResourceId));
            }
        }

        public bool SubscriptionIsCreated(ResourceLocation resourceLocation)
        {
            lock (subscriptionsLockObject)
            {
                return subscriptions.ContainsKey(resourceLocation);
            }
        }

        private bool RegisterResourceInternal(string remoteResourceId, ResourceLocation resourceLocation,
            RequestHandler requestHandler, string owner)
        {
            resourceLocations[resourceLocation] = null;
            string text = messageBroker.RegisterResource(resourceLocation, requestHandler, owner);
            if (text == null)
            {
                lock (resourceLocationsLockObject)
                {
                    resourceLocations.Remove(resourceLocation);
                }

                return false;
            }

            lock (resourceLocationsLockObject)
            {
                resourceLocations[resourceLocation] = new RemoteLocalIdPair
                {
                    RemoteId = remoteResourceId,
                    LocalId = text
                };
            }

            return true;
        }

        private ResourceLocation GetSubscriptionResourceLocationByRemoteId(string remoteId)
        {
            KeyValuePair<ResourceLocation, RemoteLocalIdPair> keyValuePair;
            lock (subscriptionsLockObject)
            {
                keyValuePair = subscriptions.FirstOrDefault((KeyValuePair<ResourceLocation, RemoteLocalIdPair> s) =>
                    s.Value.RemoteId == remoteId);
            }

            if (keyValuePair.Equals(default(KeyValuePair<ResourceLocation, RemoteLocalIdPair>)))
            {
                return null;
            }

            return keyValuePair.Key;
        }

        private ResourceLocation GetRegistrationResourceLocationByRemoteId(string remoteId)
        {
            KeyValuePair<ResourceLocation, RemoteLocalIdPair> keyValuePair;
            lock (resourceLocationsLockObject)
            {
                keyValuePair = resourceLocations.FirstOrDefault((KeyValuePair<ResourceLocation, RemoteLocalIdPair> s) =>
                    s.Value.RemoteId == remoteId);
            }

            if (keyValuePair.Equals(default(KeyValuePair<ResourceLocation, RemoteLocalIdPair>)))
            {
                return null;
            }

            return keyValuePair.Key;
        }

        private void Unsubscribe(ResourceLocation resourceLocation, string owner)
        {
            if (resourceLocation == null)
            {
                return;
            }

            RemoteLocalIdPair value;
            lock (subscriptionsLockObject)
            {
                if (!subscriptions.TryGetValue(resourceLocation, out value))
                {
                    return;
                }
            }

            if (value != null)
            {
                messageBroker.RemoveSubscription(value.LocalId, owner);
            }

            lock (subscriptionsLockObject)
            {
                subscriptions.Remove(resourceLocation);
            }
        }

        private void Unregister(ResourceLocation resourceLocation, string owner)
        {
            if (resourceLocation == null)
            {
                return;
            }

            RemoteLocalIdPair value;
            lock (resourceLocationsLockObject)
            {
                if (!resourceLocations.TryGetValue(resourceLocation, out value))
                {
                    return;
                }
            }

            if (value != null)
            {
                messageBroker.UnregisterResource(value.LocalId, owner);
            }

            lock (resourceLocationsLockObject)
            {
                resourceLocations.Remove(resourceLocation);
            }
        }
    }
}