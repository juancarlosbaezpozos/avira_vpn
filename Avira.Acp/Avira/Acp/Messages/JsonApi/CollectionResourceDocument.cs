using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Avira.Acp.Messages.JsonApi
{
    [DataContract]
    public class CollectionResourceDocument<T> : Document
    {
        [DataMember(Name = "data")] public List<Resource<T>> Data { get; set; }
    }
}