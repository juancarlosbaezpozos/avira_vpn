using System.Runtime.Serialization;

namespace Avira.Acp.Messages.JsonApi
{
    [DataContract]
    public class SingleResourceDocument<T> : Document
    {
        [DataMember(Name = "data")] public Resource<T> Data { get; set; }
    }
}