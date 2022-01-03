using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public interface IOeApi
    {
        Task<JObject> UpdateAppEvents(string eventType, string eventName, JObject parameters);

        Task SendHeartbeat(JObject customData);

        Task SendFeatureUsed(string feature, JObject customData);

        Task SendAppOpen(string trigger, JObject customData);

        Task<long> GetDeviceId();
    }
}