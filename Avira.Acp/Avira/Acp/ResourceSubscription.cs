using Avira.Acp.Messages;

namespace Avira.Acp
{
    internal class ResourceSubscription
    {
        private readonly SequentialDispatcher<Notification> sequentialDispatcher;

        public ResourceLocation ResourceLocation { get; private set; }

        public NotificationHandler Handler => sequentialDispatcher.DispatchAsync;

        public string Id { get; private set; }

        public string Owner { get; private set; }

        public ResourceSubscription(string owner, ResourceLocation resourceLocation, NotificationHandler handler)
        {
            sequentialDispatcher = new SequentialDispatcher<Notification>(handler.Invoke);
            ResourceLocation = resourceLocation;
            Id = UniqueIdProvider.Get();
            Owner = owner;
        }
    }
}