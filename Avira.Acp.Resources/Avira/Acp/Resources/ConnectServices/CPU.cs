using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract]
    public class CPU
    {
        [DataMember(Name = "threads")] public int Threads { get; set; }

        [DataMember(Name = "name")] public string Name { get; set; }

        [DataMember(Name = "freq")] public int Frequency { get; set; }
    }
}