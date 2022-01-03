using System.Net;

namespace Avira.Common.Core.Networking
{
    public interface IDnsResolver
    {
        IPAddress[] GetIpAdresses(string hostName);
    }
}