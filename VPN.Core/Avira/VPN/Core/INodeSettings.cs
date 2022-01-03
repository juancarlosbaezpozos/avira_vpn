using System.Threading.Tasks;

namespace Avira.VPN.Core
{
    public interface INodeSettings
    {
        Task UpdateFeatures();

        Task UpdateFeatures(NodeSessionInfo nodeSessionInfo);
    }
}