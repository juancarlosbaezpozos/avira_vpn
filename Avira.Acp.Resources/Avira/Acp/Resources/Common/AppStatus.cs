using System.Collections.Generic;
using System.Runtime.Serialization;
using Avira.Acp.Common;
using ServiceStack.Text;

namespace Avira.Acp.Resources.Common
{
    [DataContract(Name = "app-statuses")]
    public class AppStatus
    {
        [DataMember(Name = "section")] public string Section { get; set; }

        [DataMember(Name = "custom_value")] public JsonObjectWrapper RawValue { get; set; }

        public virtual Dictionary<string, string> Value
        {
            get { return JsonSerializer.DeserializeFromString<Dictionary<string, string>>(RawValue.ToString()); }
            set { RawValue = new JsonObjectWrapper(JsonSerializer.SerializeToString(value)); }
        }
    }
}