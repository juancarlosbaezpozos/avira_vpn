using System;
using Avira.Acp.Caching.Configuration;
using Avira.Acp.Messages;

namespace Avira.Acp.Caching
{
    public class ResponseCache : ICache
    {
        private readonly object lockObject = new object();

        private readonly IStorage storage;

        private readonly IConfiguration configuration;

        private readonly IResourceLocationDependencyResolver resourceLocationDependencyResolver;

        internal ResponseCache(IStorage storage, IConfiguration configuration,
            IResourceLocationDependencyResolver resourceLocationDependencyResolver)
        {
            this.storage = storage;
            this.configuration = configuration;
            this.resourceLocationDependencyResolver = resourceLocationDependencyResolver;
        }

        public ResponseCache(IStorage storage, IConfiguration configuration)
            : this(storage, configuration, new ResourceLocationDependencyResolver(configuration))
        {
        }

        public bool Get(Request request, out Response response)
        {
            lock (lockObject)
            {
                ResourceLocation resourceLocation = request.ResourceLocation;
                if (!storage.TryGetValue(resourceLocation, out var cacheEntry) || IsExpired(cacheEntry,
                        configuration.GetResourceConfiguration(resourceLocation).DefaultMayAgeAsTimeSpan))
                {
                    response = null;
                    return false;
                }

                response = Response.Clone(cacheEntry.Response);
                response.Id = request.Id;
                return true;
            }
        }

        public void Add(ResourceLocation resourceLocation, Response response)
        {
            lock (lockObject)
            {
                storage.AddOrUpdate(resourceLocation, new CacheEntry(Response.Clone(response), DateTime.Now));
                storage.AddDependentResources(resourceLocation,
                    resourceLocationDependencyResolver.GetDependendResources(resourceLocation));
            }
        }

        public void Clear(ResourceLocation resourceLocation)
        {
            lock (lockObject)
            {
                storage.RemoveDependentEntries(resourceLocation);
            }
        }

        private bool IsExpired(CacheEntry cacheEntry, TimeSpan? defaultMaxAge)
        {
            TimeSpan value = DateTime.Now - cacheEntry.ResponseTimeStamp;
            TimeSpan? maxAge = GetMaxAge(cacheEntry, defaultMaxAge);
            return value >= maxAge;
        }

        private TimeSpan? GetMaxAge(CacheEntry cacheEntry, TimeSpan? defaultMaxAge)
        {
            if (cacheEntry.Response.Headers.AviraCacheControl.MaxAge.HasValue)
            {
                return cacheEntry.Response.Headers.AviraCacheControl.MaxAge ?? defaultMaxAge;
            }

            return cacheEntry.Response.Headers.CacheControl.MaxAge ?? defaultMaxAge;
        }
    }
}