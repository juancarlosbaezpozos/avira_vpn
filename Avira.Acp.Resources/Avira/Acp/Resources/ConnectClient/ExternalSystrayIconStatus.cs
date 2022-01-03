using System.Runtime.Serialization;

namespace Avira.Acp.Resources.ConnectClient
{
    [DataContract]
    public class ExternalSystrayIconStatus
    {
        [DataMember(Name = "tooltip_text")] public string TooltipText { get; set; }

        [DataMember(Name = "show_custom_icon")]
        public bool ShowCustomIcon { get; set; }

        [DataMember(Name = "custom_icon")] public CustomIconData CustomIcon { get; set; }
    }
}