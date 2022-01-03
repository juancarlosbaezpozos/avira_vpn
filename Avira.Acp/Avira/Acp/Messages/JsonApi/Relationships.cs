using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Avira.Acp.Messages.JsonApi
{
    [DataContract]
    public class Relationships
    {
        [DataMember(Name = "data")] public List<Relationship> Data { get; set; }
    }
}