using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectClient
{
    [DataContract]
    public class CustomIconData
    {
        [DataMember(Name = "path")] public string Path { get; set; }

        [DataMember(Name = "signature_path")] public string SignaturePath { get; set; }

        [DataMember(Name = "identifier")] public string Identifier { get; set; }
    }
}