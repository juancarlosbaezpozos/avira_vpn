using System.Runtime.Serialization;

namespace Avira.Acp.Caching.SmartCache
{
    [DataContract]
    public class SmartCacheResourceLocation
    {
        [DataMember(Name = "id")] public string Id { get; set; }

        [DataMember(Name = "resourceLocation")]
        public ResourceLocation ResourceLocation { get; set; }

        public SmartCacheResourceLocation()
        {
        }

        public SmartCacheResourceLocation(ResourceLocation resourceLocation)
        {
            ResourceLocation = resourceLocation;
            Id = ResourceLocation.GetHashCode().ToString();
        }

        public SmartCacheResourceLocation(string host, string path)
            : this(new ResourceLocation(host, path))
        {
        }
    }
}