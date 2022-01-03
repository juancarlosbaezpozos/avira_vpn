using Newtonsoft.Json;

namespace Avira.Common.Acp.AppClient
{
    public class UserProfileData
    {
        [JsonProperty(PropertyName = "first_name")]
        public string FirstName { get; set; }

        [JsonProperty(PropertyName = "last_name")]
        public string LastName { get; set; }
    }
}