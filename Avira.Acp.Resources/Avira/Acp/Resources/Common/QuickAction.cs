using System.Runtime.Serialization;
using ServiceStack.Text;

namespace Avira.Acp.Resources.Common
{
    [DataContract(Name = "app-actions")]
    public class QuickAction
    {
        [DataMember(Name = "text")] public string Text { get; set; }

        [DataMember(Name = "tag")] public string Tag { get; set; }

        [DataMember(Name = "enabled")] public bool Enabled { get; set; }

        [DataMember(Name = "order")] public int Order { get; set; }

        [DataMember(Name = "action_uri")] public string ActionUri { get; set; }

        [DataMember(Name = "action_verb")] public string ActionVerb { get; set; }

        [DataMember(Name = "action_payload")] public JsonObject ActionPayload { get; set; }
    }
}