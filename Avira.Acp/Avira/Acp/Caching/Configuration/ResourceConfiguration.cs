using System;
using System.Collections.Generic;

namespace Avira.Acp.Caching.Configuration
{
    public class ResourceConfiguration
    {
        public ResourceLocation ResourceLocation { get; private set; }

        public int? DefaultMaxAge { get; private set; }

        public ResourceCacheLevel CacheLevel { get; private set; }

        public IDictionary<string, ResourceLocation> Relationships { get; private set; }

        public TimeSpan? DefaultMayAgeAsTimeSpan
        {
            get
            {
                if (!DefaultMaxAge.HasValue)
                {
                    return null;
                }

                return TimeSpan.FromSeconds(DefaultMaxAge.Value);
            }
        }

        public ResourceConfiguration(ResourceLocation resourceLocation)
        {
            ResourceLocation = resourceLocation;
            CacheLevel = ResourceCacheLevel.Default;
            DefaultMaxAge = null;
            Relationships = new Dictionary<string, ResourceLocation>();
        }

        public ResourceConfiguration WithCacheLevel(ResourceCacheLevel cacheLevel)
        {
            CacheLevel = cacheLevel;
            return this;
        }

        public ResourceConfiguration WithDefaultMaxAge(TimeSpan defaultMaxAge)
        {
            DefaultMaxAge = (int)defaultMaxAge.TotalSeconds;
            return this;
        }

        public ResourceConfiguration WithRelationships(IDictionary<string, ResourceLocation> relationships)
        {
            Relationships = relationships;
            return this;
        }
    }
}