using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public interface IOeResource
    {
        long Id { get; }

        JObject JsonObject { get; }
    }
}