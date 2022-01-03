using System;
using System.Runtime.Serialization;
using Avira.Acp.Common;
using Avira.Acp.Resources.Common;
using ServiceStack.Text;

namespace Avira.Acp.Resources.Antivirus
{
    [DataContract(Name = "app-statuses")]
    public class AvAppStatus : AppStatus
    {
        public object SectionValue
        {
            get { return JsonSerializer.DeserializeFromString(base.RawValue?.ToString(), GetSectionType()); }
            set { base.RawValue = new JsonObjectWrapper(JsonSerializer.SerializeToString(value, GetSectionType())); }
        }

        [DataMember(Name = "source")] public string Source { get; set; }

        public AvAppStatus()
        {
        }

        public AvAppStatus(string section)
        {
            base.Section = section;
        }

        private Type GetSectionType()
        {
            return base.Section switch
            {
                "last_scan" => typeof(LastScanSection),
                "modules" => typeof(ModulesModel),
                "about" => typeof(AboutModel),
                "app_state" => typeof(AppStateModel),
                _ => typeof(object),
            };
        }
    }
}