using Newtonsoft.Json.Linq;

namespace Avira.VPN.OeConnector
{
    public interface IResource
    {
        long Id { get; }

        JObject JsonObject { get; }
    }
}