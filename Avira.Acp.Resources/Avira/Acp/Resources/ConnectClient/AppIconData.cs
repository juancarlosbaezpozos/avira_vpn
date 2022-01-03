using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectClient
{
    [DataContract]
    public class AppIconData
    {
        [DataMember(Name = "active")] public string Active { get; set; }

        [DataMember(Name = "inactive")] public string Inactive { get; set; }
    }
}