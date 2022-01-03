using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract]
    public class MemoryInfo
    {
        [DataMember(Name = "size")] public long? TotalSize { get; set; }

        [DataMember(Name = "freq")] public int Frequency { get; set; }
    }
}