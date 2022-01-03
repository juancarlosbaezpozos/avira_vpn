using System.Collections.Generic;

namespace Avira.Acp
{
    internal interface IRegisteredSubscriptionHandlers : IResourceRepository<ResourceLocation>
    {
        string Add(ResourceLocation resourceLocation, NotificationHandler handler, string owner);

        IEnumerable<NotificationHandler> Get(ResourceLocation resourceLocation);

        IEnumerable<NotificationHandler> Get(ResourceLocation resourceLocation, string excludedOwner);

        bool Remove(string subscriptionId, string owner);
    }
}