using System;
using System.Runtime.Serialization;

namespace Avira.Acp.Resources.Antivirus
{
    [DataContract]
    public class LastScanSection
    {
        [DataMember(Name = "date")] public string RawDate { get; set; }

        public DateTime? Date
        {
            get { return ValueConverter.DateTimeFromAvDate(RawDate); }
            set { RawDate = ValueConverter.AvDateFromDateTime(value); }
        }

        [DataMember(Name = "result")] public AvScanResultModel Result { get; set; }
    }
}