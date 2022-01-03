using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public interface IDiagnostics
    {
        Task<bool> CollectData(JObject userSelection);

        DiagnosticData SendData();
    }
}