using System.Net;

namespace Avira.VPN.Core.Win
{
    public class DnsWrapper : IDnsWrapper
    {
        public IPHostEntry GetHostEntry(string hostNameOrAddress)
        {
            return Dns.GetHostEntry(hostNameOrAddress);
        }
    }
}