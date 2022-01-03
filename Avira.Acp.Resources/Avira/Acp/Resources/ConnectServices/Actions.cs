using System.Collections.Generic;
using System.Runtime.Serialization;
using Avira.Acp.Common;
using ServiceStack.Text;

namespace Avira.Acp.Resources.ConnectServices
{
    [DataContract(Name = "actions")]
    public class Actions
    {
        [DataMember(Name = "action_type")] public string ActionType { get; set; }

        [DataMember(Name = "status")] public string Status { get; set; }

        [DataMember(Name = "error_code")] public string ErrorCode { get; set; }

        [DataMember(Name = "error_description")]
        public string ErrorDescription { get; set; }

        [DataMember(Name = "custom")] public JsonObjectWrapper RawCustom { get; set; }

        public virtual Dictionary<string, string> Custom
        {
            get { return JsonSerializer.DeserializeFromString<Dictionary<string, string>>(RawCustom.ToString()); }
            set { RawCustom = new JsonObjectWrapper(JsonSerializer.SerializeToString(value)); }
        }

        [DataMember(Name = "date_added")] public string DateAdded { get; set; }

        [DataMember(Name = "date_updated")] public string DateUpdated { get; set; }
    }
}