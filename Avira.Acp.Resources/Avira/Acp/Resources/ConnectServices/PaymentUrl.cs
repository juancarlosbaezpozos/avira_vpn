using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract(Name = "payment-urls")]
    public class PaymentUrl
    {
        [DataMember(Name = "url")] public string Url { get; set; }

        [DataMember(Name = "operation")] public string Operation { get; set; }

        [DataMember(Name = "scope")] public string Scope { get; set; }

        [DataMember(Name = "target")] public string Target { get; set; }
    }
}