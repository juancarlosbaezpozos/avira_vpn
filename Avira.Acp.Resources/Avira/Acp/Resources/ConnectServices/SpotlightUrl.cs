using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract(Name = "spotlight-urls")]
    public class SpotlightUrl
    {
        [DataMember(Name = "url")] public string Url { get; set; }
    }
}