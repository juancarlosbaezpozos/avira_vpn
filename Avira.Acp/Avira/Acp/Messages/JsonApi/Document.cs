using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Avira.Acp.Common;
using Avira.Acp.Extensions;
using ServiceStack.Text;

namespace Avira.Acp.Messages.JsonApi
{
    public abstract class Document
    {
        [DataMember(Name = "included")] public JsonObjectWrapper Included { get; set; }

        [DataMember(Name = "errors")] public List<Error> Errors { get; set; }

        [DataMember(Name = "meta")] public JsonObjectWrapper Meta { get; set; }

        [DataMember(Name = "links")] public JsonObjectWrapper Links { get; set; }

        public bool IsError()
        {
            return Errors != null;
        }

        public string GetErrorMessage()
        {
            if (!IsError())
            {
                return string.Empty;
            }

            string text = string.Empty;
            foreach (Error error in Errors)
            {
                text = ((text == string.Empty) ? text : $"{text}\r\n");
                text = $"{text}{error.Title}: {error.Detail}";
            }

            return text;
        }

        public IEnumerable<Resource<T>> GetIncludedResources<T>()
        {
            string acpTypeName = typeof(T).GetAcpTypeName();
            if (Included == null)
            {
                return Enumerable.Empty<Resource<T>>();
            }

            return from resource in JsonSerializer.DeserializeFromString<IEnumerable<Resource<T>>>(Included.ToString())
                where resource.Type == acpTypeName
                select resource;
        }

        public Resource<TTo> FindRelated<TTo, TFrom>(Resource<TFrom> fromResource, string relationshipName)
        {
            if (Included == null)
            {
                return null;
            }

            Relationship relationship = fromResource?.GetRelationships(relationshipName)?.Data?.FirstOrDefault();
            if (relationship == null)
            {
                return null;
            }

            return JsonSerializer.DeserializeFromString<IEnumerable<Resource<TTo>>>(Included.ToString())
                .FirstOrDefault((Resource<TTo> included) =>
                    included.Type == relationship.Type && included.Id == relationship.Id);
        }

        public T GetMeta<T>()
        {
            if (Meta == null)
            {
                return default(T);
            }

            return JsonSerializer.DeserializeFromString<T>(Meta.ToString());
        }

        public T GetLinks<T>()
        {
            if (Links == null)
            {
                return default(T);
            }

            return JsonSerializer.DeserializeFromString<T>(Links.ToString());
        }
    }
}