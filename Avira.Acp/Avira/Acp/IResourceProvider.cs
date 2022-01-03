using Avira.Acp.Messages;

namespace Avira.Acp
{
    public interface IResourceProvider
    {
        ResourceLocation ResourceLocation { get; }

        void HandleMessage(Request request);
    }
}