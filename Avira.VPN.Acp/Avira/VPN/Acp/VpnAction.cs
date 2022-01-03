using System.Runtime.Serialization;

namespace Avira.VPN.Acp
{
    [DataContract(Name = "vpn-actions")]
    public class VpnAction
    {
        [DataMember(Name = "command")] public string Command { get; set; }

        [DataMember(Name = "id")] public string Id { get; internal set; }

        [DataMember(Name = "tag")] public string Tag { get; internal set; }
    }
}