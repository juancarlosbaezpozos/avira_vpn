using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.Common.Acp.AppClient
{
    public class UserProfile : ResourceClient<UserProfileData>
    {
        public UserProfile(IAcpCommunicator acpCommunicator)
            : base(acpCommunicator, "launcher", "/profiles")
        {
        }

        public override UserProfileData DeserializePayload(string payload)
        {
            JsonConvert.DeserializeObject<JObject>(payload)!.TryGetValue("data", out var value);
            JArray jArray = value as JArray;
            return (((jArray == null) ? (value as JObject) : (jArray[0] as JObject))?.GetValue("attributes") as JObject)
                .ToObject<UserProfileData>();
        }
    }
}