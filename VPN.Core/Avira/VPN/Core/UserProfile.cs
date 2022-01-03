using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class UserProfile
    {
        [JsonProperty(PropertyName = "first_name")]
        public string FirstName { get; set; }

        [JsonProperty(PropertyName = "last_name")]
        public string LastName { get; set; }
    }
}