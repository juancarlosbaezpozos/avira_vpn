using Avira.Acp.Messages;

namespace Avira.Acp.Caching.SmartCache
{
    public interface ISmartCacheLogic
    {
        ResourceLocation ResourceLocation { get; }

        bool TryGetDataFromCache(Request request, out Response response);

        void Cache(Response response, string verb, string host, string path);

        void Cache(Notification notification);

        void Clear();
    }
}