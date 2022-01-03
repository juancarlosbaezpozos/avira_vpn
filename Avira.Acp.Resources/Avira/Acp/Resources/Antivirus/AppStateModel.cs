using System.Runtime.Serialization;

namespace Avira.Acp.Resources.Antivirus
{
    [DataContract]
    public class AppStateModel
    {
        [DataMember(Name = "status")] public string Status { get; set; }

        [DataMember(Name = "display_text")] public string DisplayText { get; set; }

        [DataMember(Name = "systray_state")] public string SystrayState { get; set; }

        [DataMember(Name = "last_event_id")] public long LastEventId { get; set; }

        [DataMember(Name = "action_id")] public int ActionId { get; set; }

        [DataMember(Name = "action_required_count")]
        public int ActionRequiredCount { get; set; }

        [DataMember(Name = "last_event_date")] public string LastEventDate { get; set; }
    }
}