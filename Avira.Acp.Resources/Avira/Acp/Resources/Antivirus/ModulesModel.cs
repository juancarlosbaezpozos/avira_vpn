using System.Runtime.Serialization;

namespace Avira.Acp.Resources.Antivirus
{
    [DataContract]
    public class ModulesModel
    {
        [DataMember(Name = "guard")] public string Guard { get; set; }

        [DataMember(Name = "mailguard")] public string MailGuard { get; set; }

        [DataMember(Name = "webguard")] public string WebGuard { get; set; }

        [DataMember(Name = "firewall")] public string Firewall { get; set; }

        [DataMember(Name = "update")] public string Update { get; set; }

        [DataMember(Name = "license")] public string License { get; set; }

        [DataMember(Name = "extended_ransomware_protection")]
        public string ExtendedRansomwareProtection { get; set; }
    }
}