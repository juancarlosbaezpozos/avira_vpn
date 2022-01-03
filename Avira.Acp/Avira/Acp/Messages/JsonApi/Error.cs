using System.Runtime.Serialization;

namespace Avira.Acp.Messages.JsonApi
{
    [DataContract]
    public class Error
    {
        [DataMember(Name = "title")] public string Title { get; set; }

        [DataMember(Name = "detail")] public string Detail { get; set; }

        [DataMember(Name = "code")] public string Code { get; set; }
    }
}