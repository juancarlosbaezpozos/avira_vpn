using System;
using System.Net;

namespace Avira.Common.Core.Networking
{
    public class DnsResolver : IDnsResolver
    {
        public IPAddress[] GetIpAdresses(string hostName)
        {
            try
            {
                return Dns.GetHostAddresses(hostName);
            }
            catch (Exception)
            {
                return new IPAddress[1] { IPAddress.None };
            }
        }

        public bool CanBeResolved(string hostName)
        {
            IPAddress[] ipAdresses = GetIpAdresses(hostName);
            if (ipAdresses.Length != 0 && !ipAdresses[0].Equals(IPAddress.None))
            {
                return !ipAdresses[0].Equals(IPAddress.Any);
            }

            return false;
        }
    }
}