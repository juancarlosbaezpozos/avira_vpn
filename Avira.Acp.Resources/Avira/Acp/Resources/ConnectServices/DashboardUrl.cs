using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract(Name = "dashboard-urls")]
    public class DashboardUrl
    {
        [DataMember(Name = "url")] public string Url { get; set; }

        [DataMember(Name = "target")] public string Target { get; set; }
    }
}