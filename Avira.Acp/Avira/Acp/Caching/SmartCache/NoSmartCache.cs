using Avira.Acp.Messages;

namespace Avira.Acp.Caching.SmartCache
{
    public class NoSmartCache : ISmartCacheLogic
    {
        public ResourceLocation ResourceLocation { get; }

        public bool TryGetDataFromCache(Request request, out Response response)
        {
            response = null;
            return false;
        }

        public void CacheAsync(Response response, string verb, string host, string path)
        {
        }

        public void Cache(Response response, string verb, string host, string path)
        {
        }

        public void Cache(Notification notification)
        {
        }

        public void Clear()
        {
        }
    }
}