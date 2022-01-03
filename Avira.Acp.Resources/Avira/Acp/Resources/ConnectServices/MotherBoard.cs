using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract]
    public class MotherBoard
    {
        [DataMember(Name = "brand")] public string Brand { get; set; }

        [DataMember(Name = "model")] public string Model { get; set; }
    }
}