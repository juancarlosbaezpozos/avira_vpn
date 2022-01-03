using System.Threading.Tasks;

namespace Avira.VPN.Core
{
    public interface IUdpPortScanner
    {
        Task<bool> IsUdpPortOpened(int port);

        Task<bool> AreIPSecPortsOpen();
    }
}