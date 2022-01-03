using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public interface IAppSettings
    {
        AppSettingsData Get();

        void Set(AppSettingsData value);

        AppSettingsData Update(JObject value);
    }
}