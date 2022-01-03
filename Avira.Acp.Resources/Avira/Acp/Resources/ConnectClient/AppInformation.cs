using System.Runtime.Serialization;
using Avira.Acp.Resources.ConnectServices;

namespace Avira.Acp.Resources.ConnectClient
{
    [DataContract]
    public class AppInformation
    {
        [DataMember(Name = "id")] public string Id { get; set; }

        [DataMember(Name = "app_type")] public AppType AppType { get; set; }

        [DataMember(Name = "display_text")] public string DisplayText { get; set; }

        [DataMember(Name = "icon")] public AppIconData Icon { get; set; }

        [DataMember(Name = "order")] public int Order { get; set; }

        [DataMember(Name = "upgradeable")] public bool Upgradeable { get; set; }

        [DataMember(Name = "upgrade_text")] public string UpgradeText { get; set; }

        [DataMember(Name = "license_type")] public string LicenseType { get; set; }

        [DataMember(Name = "prototype")] public string Prototype { get; set; }
    }
}