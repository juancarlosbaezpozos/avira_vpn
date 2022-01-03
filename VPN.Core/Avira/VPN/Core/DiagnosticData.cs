using System;
using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class DiagnosticData
    {
        [JsonProperty(PropertyName = "id")] public string DiagnosticId { get; set; }

        [JsonProperty(PropertyName = "date")] public DateTime Date { get; set; }
    }
}