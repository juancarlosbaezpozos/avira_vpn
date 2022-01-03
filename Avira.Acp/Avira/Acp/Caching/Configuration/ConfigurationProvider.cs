using System.Collections.Generic;
using System.Linq;

namespace Avira.Acp.Caching.Configuration
{
    public class ConfigurationProvider : IConfiguration
    {
        private readonly List<ResourceConfiguration> resourceConfigurations;

        public ConfigurationProvider(IEnumerable<ResourceConfiguration> resourceConfigurations)
        {
            this.resourceConfigurations = resourceConfigurations.ToList();
        }

        public ResourceConfiguration GetResourceConfiguration(ResourceLocation resourceLocation)
        {
            return resourceConfigurations.FirstOrDefault((ResourceConfiguration c) =>
                c.ResourceLocation.CheckMatch(resourceLocation));
        }
    }
}