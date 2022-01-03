using Avira.Acp.Messages;

namespace Avira.Acp.Caching
{
    public interface ICache
    {
        void Clear(ResourceLocation resourceLocation);

        bool Get(Request request, out Response response);

        void Add(ResourceLocation resourceLocation, Response response);
    }
}