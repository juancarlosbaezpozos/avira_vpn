using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avira.Acp.Caching.Configuration;

namespace Avira.Acp.Caching
{
    public class ResourceLocationDependencyResolver : IResourceLocationDependencyResolver
    {
        private readonly IConfiguration configuration;

        public ResourceLocationDependencyResolver(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IEnumerable<ResourceLocation> GetDependendResources(ResourceLocation resourceLocation)
        {
            ResourceConfiguration resourceLocationConfiguration =
                configuration.GetResourceConfiguration(resourceLocation);
            yield return resourceLocationConfiguration.ResourceLocation;
            if (resourceLocationConfiguration.Relationships == null)
            {
                yield break;
            }

            Match match = Regex.Match(resourceLocation.Path, "include=([^&]+)");
            if (!match.Success)
            {
                yield break;
            }

            string[] array = match.Groups[1].Value.Split(',');
            foreach (string key in array)
            {
                if (resourceLocationConfiguration.Relationships.TryGetValue(key, out var value))
                {
                    yield return value;
                }
            }
        }
    }
}