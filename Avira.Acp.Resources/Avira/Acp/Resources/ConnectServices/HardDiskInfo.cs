using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract]
    public class HardDiskInfo
    {
        [DataMember(Name = "name")] public string Name { get; set; }

        [DataMember(Name = "size")] public ulong? Size { get; set; }
    }
}