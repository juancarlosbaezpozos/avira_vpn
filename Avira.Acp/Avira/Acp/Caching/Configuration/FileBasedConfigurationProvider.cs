using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.Text;

namespace Avira.Acp.Caching.Configuration
{
    public class FileBasedConfigurationProvider : ConfigurationProvider
    {
        public FileBasedConfigurationProvider(string path)
            : base(ReadConfigurationsFromFile(path))
        {
        }

        private static IEnumerable<ResourceConfiguration> ReadConfigurationsFromFile(string path)
        {
            try
            {
                using FileStream stream = File.OpenRead(path);
                return JsonSerializer.DeserializeFromStream<IEnumerable<ResourceConfiguration>>(stream);
            }
            catch (Exception)
            {
                return Enumerable.Empty<ResourceConfiguration>();
            }
        }
    }
}