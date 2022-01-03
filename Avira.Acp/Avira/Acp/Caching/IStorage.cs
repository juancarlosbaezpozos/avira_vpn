using System.Collections.Generic;

namespace Avira.Acp.Caching
{
    public interface IStorage
    {
        bool TryGetValue(ResourceLocation location, out CacheEntry cacheEntry);

        void AddOrUpdate(ResourceLocation location, CacheEntry cacheEntry);

        void AddDependentResources(ResourceLocation location, IEnumerable<ResourceLocation> dependentResources);

        void RemoveDependentEntries(ResourceLocation location);
    }
}