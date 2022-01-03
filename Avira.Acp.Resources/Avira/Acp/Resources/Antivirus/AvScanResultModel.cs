using System.Runtime.Serialization;

namespace Avira.Acp.Resources.Antivirus
{
    [DataContract]
    public class AvScanResultModel
    {
        [DataMember(Name = "number_files")] public int Files { get; set; }

        [DataMember(Name = "number_directories")]
        public int Folders { get; set; }

        [DataMember(Name = "number_malware")] public int Malware { get; set; }

        [DataMember(Name = "number_warnings")] public int Errors { get; set; }
    }
}