namespace Avira.Acp
{
    internal class ResourceHandlerMapEntry
    {
        public ResourceLocation ResourceLocation { get; private set; }

        public RequestHandler Handler { get; private set; }

        public string Id { get; private set; }

        public ResourceHandlerMapEntry(ResourceLocation resourceLocation, RequestHandler handler)
        {
            ResourceLocation = resourceLocation;
            Handler = handler;
            Id = UniqueIdProvider.Get();
        }
    }
}