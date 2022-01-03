using System;
using System.Collections.Generic;
using Avira.Acp.Logging;

namespace Avira.Acp.Caching
{
    public class NonThrowingStorage : IStorage
    {
        private readonly IStorage wrappedStorage;

        private readonly ILogger logger;

        public NonThrowingStorage(IStorage wrappedStorage)
            : this(wrappedStorage, LoggerFacade.GetCurrentClassLogger())
        {
        }

        internal NonThrowingStorage(IStorage wrappedStorage, ILogger logger)
        {
            this.wrappedStorage = wrappedStorage;
            this.logger = logger;
        }

        public bool TryGetValue(ResourceLocation location, out CacheEntry cacheEntry)
        {
            try
            {
                return wrappedStorage.TryGetValue(location, out cacheEntry);
            }
            catch (Exception arg)
            {
                logger.Warn($"Reading value from storage failed: {arg}");
                cacheEntry = null;
                return false;
            }
        }

        public void AddOrUpdate(ResourceLocation location, CacheEntry cacheEntry)
        {
            try
            {
                wrappedStorage.AddOrUpdate(location, cacheEntry);
            }
            catch (Exception arg)
            {
                logger.Warn($"Adding/updating value to storage failed: {arg}");
            }
        }

        public void AddDependentResources(ResourceLocation location, IEnumerable<ResourceLocation> dependentResources)
        {
            try
            {
                wrappedStorage.AddDependentResources(location, dependentResources);
            }
            catch (Exception arg)
            {
                logger.Warn($"Adding dependent resource to storage failed: {arg}");
            }
        }

        public void RemoveDependentEntries(ResourceLocation location)
        {
            try
            {
                wrappedStorage.RemoveDependentEntries(location);
            }
            catch (Exception arg)
            {
                logger.Warn($"Removing dependent entries from storage failed: {arg}");
            }
        }
    }
}