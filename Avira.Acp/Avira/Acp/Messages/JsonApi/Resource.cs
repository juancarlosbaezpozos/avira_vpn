using System.Collections.Generic;
using System.Runtime.Serialization;
using Avira.Acp.Common;
using ServiceStack.Text;

namespace Avira.Acp.Messages.JsonApi
{
    [DataContract]
    public class Resource<T>
    {
        [DataContract]
        private class SingleRelationship
        {
            [DataMember(Name = "data")] public Relationship Data { get; set; }
        }

        [DataMember(Name = "id")] public string Id { get; set; }

        [DataMember(Name = "type")] public string Type { get; set; }

        [DataMember(Name = "meta")] public JsonObjectWrapper Meta { get; set; }

        [DataMember(Name = "links")] public JsonObjectWrapper Links { get; set; }

        [DataMember(Name = "attributes")] public T Attributes { get; set; }

        [DataMember(Name = "relationships")] public JsonObjectWrapper Relationships { get; set; }

        public void AddRelationship(string resourceName, Relationship relationship)
        {
            if (!string.IsNullOrEmpty(resourceName) && relationship != null)
            {
                Dictionary<string, SingleRelationship> dictionary = ((Relationships != null)
                    ? JsonSerializer.DeserializeFromString<Dictionary<string, SingleRelationship>>(
                        Relationships.ToString())
                    : new Dictionary<string, SingleRelationship>());
                dictionary[resourceName] = new SingleRelationship
                {
                    Data = relationship
                };
                Relationships = new JsonObjectWrapper(JsonSerializer.SerializeToString(dictionary));
            }
        }

        public Relationships GetRelationships(string resourceName)
        {
            if (Relationships == null || resourceName == null)
            {
                return null;
            }

            Dictionary<string, Relationships> dictionary =
                JsonSerializer.DeserializeFromString<Dictionary<string, Relationships>>(Relationships.ToString());
            if (!dictionary.ContainsKey(resourceName))
            {
                return null;
            }

            return dictionary[resourceName];
        }
    }
}