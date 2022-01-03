using System;
using System.Collections.Generic;
using System.Linq;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    internal class RegisteredSubscriptionHandlers : BaseResourceRepository<ResourceLocation>,
        IRegisteredSubscriptionHandlers, IResourceRepository<ResourceLocation>
    {
        private const string ResourceType = "subscriptions";

        private readonly List<ResourceSubscription> subscriptions = new List<ResourceSubscription>();

        public string Add(ResourceLocation resourceLocation, NotificationHandler handler, string owner)
        {
            if (!resourceLocation.IsValid())
            {
                throw new ArgumentException("Resource location is not valid");
            }

            ResourceSubscription resourceSubscription = new ResourceSubscription(owner, resourceLocation, handler);
            lock (subscriptions)
            {
                subscriptions.Add(resourceSubscription);
            }

            OnCreated(new CreatedEventArgs<ResourceLocation>(owner, resourceSubscription.ResourceLocation,
                resourceSubscription.Id, "subscriptions"));
            return resourceSubscription.Id;
        }

        public IEnumerable<NotificationHandler> Get(ResourceLocation resourceLocation)
        {
            if (resourceLocation == null)
            {
                return Enumerable.Empty<NotificationHandler>();
            }

            lock (subscriptions)
            {
                return (from s in subscriptions.FindAll((ResourceSubscription subscription) =>
                        subscription.ResourceLocation.CheckMatch(resourceLocation))
                    select s.Handler).ToList();
            }
        }

        public IEnumerable<NotificationHandler> Get(ResourceLocation resourceLocation, string excludedOwner)
        {
            if (string.IsNullOrEmpty(excludedOwner))
            {
                return Get(resourceLocation);
            }

            if (resourceLocation == null)
            {
                return Enumerable.Empty<NotificationHandler>();
            }

            lock (subscriptions)
            {
                return (from s in subscriptions.FindAll((ResourceSubscription subscription) =>
                        subscription.ResourceLocation.CheckMatch(resourceLocation) &&
                        subscription.Owner != excludedOwner)
                    select s.Handler).ToList();
            }
        }

        public bool Remove(string subscriptionId, string owner)
        {
            bool flag;
            lock (subscriptions)
            {
                flag = subscriptions.RemoveAll((ResourceSubscription subscription) =>
                    subscription.Id.Equals(subscriptionId)) > 0;
            }

            if (flag)
            {
                OnDeleted(new DeletedEventArgs(owner, subscriptionId, "subscriptions"));
            }

            return flag;
        }

        public override List<Resource<ResourceLocation>> ReadAll(string filter)
        {
            lock (subscriptions)
            {
                return subscriptions.Select((ResourceSubscription subscription) => new Resource<ResourceLocation>
                {
                    Id = subscription.Id,
                    Attributes = subscription.ResourceLocation,
                    Type = "subscriptions"
                }).ToList();
            }
        }
    }
}