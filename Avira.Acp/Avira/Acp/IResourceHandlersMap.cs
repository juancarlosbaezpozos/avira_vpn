using System.Collections.Generic;

namespace Avira.Acp
{
    internal interface IResourceHandlersMap : IResourceRepository<ResourceLocation>
    {
        string Add(ResourceLocation resourceLocation, RequestHandler handler, string owner);

        string AddSubstitute(ResourceLocation resourceLocation, IResourceProvider providerSubstitute);

        bool RemoveSubstitute(ResourceLocation resourceLocation);

        RequestHandler Get(ResourceLocation resourceLocation);

        ICollection<ResourceLocation> GetAllResourceLocations();

        bool Remove(string resourceId, string owner);

        bool IsResourceRegistered(ResourceLocation resourceLocation);
    }
}