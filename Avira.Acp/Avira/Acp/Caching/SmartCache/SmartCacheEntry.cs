using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Avira.Acp.Common;
using Avira.Acp.Messages.JsonApi;
using ServiceStack.Text;

namespace Avira.Acp.Caching.SmartCache
{
    [DataContract]
    public class SmartCacheEntry<T>
    {
        [DataMember(Name = "id")] public string Id { get; set; }

        [DataMember(Name = "expirationDate")] public DateTime ExpirationDate { get; set; }

        [DataMember(Name = "originalResource")]
        public Resource<JsonObjectWrapper> OriginalResource { get; set; }

        [DataMember(Name = "resource")] public Resource<T> Resource { get; set; }

        [DataMember(Name = "header")] public Dictionary<string, string> Header { get; set; }

        public SmartCacheEntry()
        {
        }

        public SmartCacheEntry(Resource<JsonObjectWrapper> originalResource, string id,
            Dictionary<string, string> headerValues, DateTime expirationDate)
        {
            Resource = ConvertResource(originalResource);
            Id = id;
            Header = headerValues;
            ExpirationDate = expirationDate;
            OriginalResource = originalResource;
        }

        public bool IsExpired()
        {
            return ExpirationDate <= DateTime.Now;
        }

        private Resource<T> ConvertResource(Resource<JsonObjectWrapper> originalResource)
        {
            return new Resource<T>
            {
                Attributes = ((originalResource.Attributes?.Data != null)
                    ? JsonSerializer.DeserializeFromString<T>(originalResource.Attributes.Data)
                    : default(T)),
                Id = originalResource.Id,
                Links = originalResource.Links,
                Meta = originalResource.Meta,
                Relationships = originalResource.Relationships,
                Type = originalResource.Type
            };
        }
    }
}