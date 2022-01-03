using System.Runtime.Serialization;

namespace Avira.Acp.Endpoints
{
    [DataContract]
    public class HandshakeRequestData : IEndpointData
    {
        [DataMember(Name = "host")] public string Host { get; set; }

        [DataMember(Name = "channel_type")] public string ChannelType { get; set; }

        [DataMember(Name = "named_pipe")] public string NamedPipeName { get; set; }
    }
}