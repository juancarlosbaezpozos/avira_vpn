using System.Net;

namespace Avira.VPN.Core.Win
{
    public interface IDnsWrapper
    {
        IPHostEntry GetHostEntry(string hostNameOrAddress);
    }
}