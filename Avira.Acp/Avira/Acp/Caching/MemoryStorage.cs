using System.Collections.Generic;

namespace Avira.Acp.Caching
{
    public class MemoryStorage : IStorage
    {
        private readonly IDictionary<ResourceLocation, CacheEntry> cacheEntriesStorage;

        private readonly IDictionary<ResourceLocation, IList<ResourceLocation>> resourceDependencyStorage;

        public MemoryStorage(IDictionary<ResourceLocation, CacheEntry> cacheEntriesStorage,
            IDictionary<ResourceLocation, IList<ResourceLocation>> resourceDependencyStorage)
        {
            this.cacheEntriesStorage = cacheEntriesStorage;
            this.resourceDependencyStorage = resourceDependencyStorage;
        }

        public MemoryStorage()
            : this(new Dictionary<ResourceLocation, CacheEntry>(),
                new Dictionary<ResourceLocation, IList<ResourceLocation>>())
        {
        }

        public bool TryGetValue(ResourceLocation location, out CacheEntry cacheEntry)
        {
            return cacheEntriesStorage.TryGetValue(location, out cacheEntry);
        }

        public void AddOrUpdate(ResourceLocation location, CacheEntry cacheEntry)
        {
            cacheEntriesStorage[location] = cacheEntry;
        }

        public void AddDependentResources(ResourceLocation location, IEnumerable<ResourceLocation> dependentResources)
        {
            foreach (ResourceLocation dependentResource in dependentResources)
            {
                if (!resourceDependencyStorage.TryGetValue(dependentResource, out var value))
                {
                    value = new List<ResourceLocation>();
                    resourceDependencyStorage[dependentResource] = value;
                }

                if (!value.Contains(location))
                {
                    value.Add(location);
                }
            }
        }

        public void RemoveDependentEntries(ResourceLocation location)
        {
            Stack<ResourceLocation> stack = new Stack<ResourceLocation>();
            stack.Push(location);
            while (stack.Count > 0)
            {
                ResourceLocation key = stack.Pop();
                if (!resourceDependencyStorage.TryGetValue(key, out var value))
                {
                    continue;
                }

                resourceDependencyStorage.Remove(key);
                foreach (ResourceLocation item in value)
                {
                    cacheEntriesStorage.Remove(item);
                    stack.Push(item);
                }
            }
        }
    }
}