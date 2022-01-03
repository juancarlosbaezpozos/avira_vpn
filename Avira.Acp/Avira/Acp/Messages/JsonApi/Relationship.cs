using System.Runtime.Serialization;

namespace Avira.Acp.Messages.JsonApi
{
    [DataContract]
    public class Relationship
    {
        [DataMember(Name = "type")] public string Type { get; set; }

        [DataMember(Name = "id")] public string Id { get; set; }
    }
}