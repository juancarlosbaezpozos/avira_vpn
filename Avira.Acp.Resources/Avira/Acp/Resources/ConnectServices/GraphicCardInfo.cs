using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract]
    public class GraphicCardInfo
    {
        [DataMember(Name = "name")] public string Name { get; set; }
    }
}