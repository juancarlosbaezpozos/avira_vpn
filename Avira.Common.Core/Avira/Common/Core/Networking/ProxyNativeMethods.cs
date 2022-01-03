using System;
using System.Configuration;
using System.Net.Configuration;
using System.Runtime.InteropServices;

namespace Avira.Common.Core.Networking
{
    public class ProxyNativeMethods
    {
        [Flags]
        public enum InternetConnectionState_e
        {
            INTERNET_CONNECTION_MODEM = 0x1,
            INTERNET_CONNECTION_LAN = 0x2,
            INTERNET_CONNECTION_PROXY = 0x4,
            INTERNET_RAS_INSTALLED = 0x10,
            INTERNET_CONNECTION_OFFLINE = 0x20,
            INTERNET_CONNECTION_CONFIGURED = 0x40
        }

        [DllImport("wininet.dll", CharSet = CharSet.Auto)]
        private static extern bool InternetGetConnectedState(ref InternetConnectionState_e lpdwFlags, int dwReserved);

        public static bool ConnectingThroughProxy()
        {
            InternetConnectionState_e lpdwFlags = (InternetConnectionState_e)0;
            InternetGetConnectedState(ref lpdwFlags, 0);
            bool num = (lpdwFlags & InternetConnectionState_e.INTERNET_CONNECTION_PROXY) != 0;
            DefaultProxySection defaultProxySection =
                ConfigurationManager.GetSection("system.net/defaultProxy") as DefaultProxySection;
            if (!num)
            {
                return defaultProxySection != null;
            }

            return true;
        }
    }
}