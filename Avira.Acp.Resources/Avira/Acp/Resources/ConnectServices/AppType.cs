using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract]
    public enum AppType
    {
        [DataMember(Name = "app")] App,
        [DataMember(Name = "bundle")] Bundle
    }
}