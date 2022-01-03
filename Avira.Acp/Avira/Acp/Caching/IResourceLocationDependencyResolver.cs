using System.Collections.Generic;

namespace Avira.Acp.Caching
{
    public interface IResourceLocationDependencyResolver
    {
        IEnumerable<ResourceLocation> GetDependendResources(ResourceLocation resourceLocation);
    }
}