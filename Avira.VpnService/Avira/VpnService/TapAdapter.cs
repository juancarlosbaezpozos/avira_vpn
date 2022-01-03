using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Avira.VPN.Core.Win;
using Serilog;

namespace Avira.VpnService
{
    internal class TapAdapter
    {
        internal static class NativeMethods
        {
            public enum ForwardProtocol
            {
                Other = 1,
                Local = 2,
                NetMGMT = 3,
                ICMP = 4,
                EGP = 5,
                GGP = 6,
                Hello = 7,
                RIP = 8,
                IS_IS = 9,
                ES_IS = 10,
                CISCO = 11,
                BBN = 12,
                OSPF = 13,
                BGP = 14,
                NT_AUTOSTATIC = 10002,
                NT_STATIC = 10006,
                NT_STATIC_NON_DOD = 10007
            }

            public enum ForwardType
            {
                Other = 1,
                Invalid,
                Direct,
                Indirect
            }

            public struct MIB_IPFORWARDROW
            {
                public uint dwForwardDest;

                public uint dwForwardMask;

                public int dwForwardPolicy;

                public uint dwForwardNextHop;

                public uint dwForwardIfIndex;

                public ForwardType dwForwardType;

                public ForwardProtocol dwForwardProto;

                public int dwForwardAge;

                public int dwForwardNextHopAS;

                public int dwForwardMetric1;

                public int dwForwardMetric2;

                public int dwForwardMetric3;

                public int dwForwardMetric4;

                public int dwForwardMetric5;
            }

            public struct MIB_IPINTERFACE_ROW
            {
                public uint Family;

                public ulong InterfaceLuid;

                public uint InterfaceIndex;

                public uint MaxReassemblySize;

                public ulong InterfaceIdentifier;

                public uint MinRouterAdvertisementInterval;

                public uint MaxRouterAdvertisementInterval;

                public byte AdvertisingEnabled;

                public byte ForwardingEnabled;

                public byte WeakHostSend;

                public byte WeakHostReceive;

                public byte UseAutomaticMetric;

                public byte UseNeighborUnreachabilityDetection;

                public byte ManagedAddressConfigurationSupported;

                public byte OtherStatefulConfigurationSupported;

                public byte AdvertiseDefaultRoute;

                public uint RouterDiscoveryBehavior;

                public uint DadTransmits;

                public uint BaseReachableTime;

                public uint RetransmitTime;

                public uint PathMtuDiscoveryTimeout;

                public uint LinkLocalAddressBehavior;

                public uint LinkLocalAddressTimeout;

                public uint ZoneIndice0;

                public uint ZoneIndice1;

                public uint ZoneIndice2;

                public uint ZoneIndice3;

                public uint ZoneIndice4;

                public uint ZoneIndice5;

                public uint ZoneIndice6;

                public uint ZoneIndice7;

                public uint ZoneIndice8;

                public uint ZoneIndice9;

                public uint ZoneIndice10;

                public uint ZoneIndice11;

                public uint ZoneIndice12;

                public uint ZoneIndice13;

                public uint ZoneIndice14;

                public uint ZoneIndice15;

                public uint SitePrefixLength;

                public uint Metric;

                public uint NlMtu;

                public byte Connected;

                public byte SupportsWakeUpPatterns;

                public byte SupportsNeighborDiscovery;

                public byte SupportsRouterDiscovery;

                public uint ReachableTime;

                public byte TransmitOffload;

                public byte ReceiveOffload;

                public byte DisableDefaultRoutes;
            }

            public const uint AF_INET = 2u;

            public const int NO_ERROR = 0;

            [DllImport("iphlpapi")]
            public static extern uint GetIpInterfaceEntry(ref MIB_IPINTERFACE_ROW pRoute);

            [DllImport("iphlpapi")]
            public static extern uint SetIpInterfaceEntry(ref MIB_IPINTERFACE_ROW pRoute);
        }

        private const string TapWindowsAdapterV9 = "Phantom TAP-Windows Adapter V9";

        private const string DeviceName = "phantomtap";

        private static Regex tunNameRegEx = new Regex("Phantom TAP-Windows Adapter V9*", RegexOptions.IgnoreCase);

        private readonly IWhiteList whiteList;

        private string name;

        private NetworkInterface selectedInterface;

        private NetworkInterface tunInterface;

        public static string DeviceDescription => "Phantom TAP-Windows Adapter V9";

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    name = GetName();
                }

                return name;
            }
        }

        public ulong Used
        {
            get
            {
                try
                {
                    if (tunInterface == null)
                    {
                        tunInterface = Interface;
                    }

                    return (ulong)(tunInterface.GetIPStatistics().BytesReceived +
                                   tunInterface.GetIPStatistics().BytesSent);
                }
                catch
                {
                    return 0uL;
                }
            }
        }

        private NetworkInterface Interface => GetAdapter();

        public TapAdapter(IWhiteList whiteList)
        {
            this.whiteList = whiteList;
        }

        public void Enable()
        {
            RunNetShInterface("interface set interface name=\"{0}\" admin=enable");
        }

        private static NetworkInterface SelectInterfaceWithMinimumMetric()
        {
            uint num = uint.MaxValue;
            NetworkInterface networkInterface = null;
            NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface networkInterface2 in allNetworkInterfaces)
            {
                int num2 = -1;
                try
                {
                    num2 = networkInterface2.GetIPProperties().GetIPv4Properties().Index;
                }
                catch (Exception)
                {
                    Log.Debug("No ipv4 properties for {0}", networkInterface2.Name);
                    continue;
                }

                uint networkInterfaceMetric = GetNetworkInterfaceMetric((uint)num2);
                if (num > networkInterfaceMetric)
                {
                    num = networkInterfaceMetric;
                    networkInterface = networkInterface2;
                }
            }

            if (networkInterface != null)
            {
                if (tunNameRegEx.IsMatch(networkInterface.Description))
                {
                    throw new VpnException("Selected interface is same as tun interface", ErrorType.TapAdapterError);
                }

                Log.Debug("selected metric : " + num + " ( " + networkInterface.Name + " )");
                return networkInterface;
            }

            throw new VpnException("Can't select interface with minimum metric", ErrorType.TapAdapterError);
        }

        private static uint GetNetworkInterfaceMetric(uint interfaceIndex)
        {
            NativeMethods.MIB_IPINTERFACE_ROW mIB_IPINTERFACE_ROW = default(NativeMethods.MIB_IPINTERFACE_ROW);
            mIB_IPINTERFACE_ROW.Family = 2u;
            mIB_IPINTERFACE_ROW.InterfaceIndex = interfaceIndex;
            NativeMethods.MIB_IPINTERFACE_ROW pRoute = mIB_IPINTERFACE_ROW;
            if (NativeMethods.GetIpInterfaceEntry(ref pRoute) != 0)
            {
                return 0u;
            }

            return pRoute.Metric;
        }

        private static void SetNetworkInterfaceMetric(uint interfaceIndex, uint metric, bool automatic)
        {
            NativeMethods.MIB_IPINTERFACE_ROW mIB_IPINTERFACE_ROW = default(NativeMethods.MIB_IPINTERFACE_ROW);
            mIB_IPINTERFACE_ROW.Family = 2u;
            mIB_IPINTERFACE_ROW.InterfaceIndex = interfaceIndex;
            NativeMethods.MIB_IPINTERFACE_ROW pRoute = mIB_IPINTERFACE_ROW;
            if (NativeMethods.GetIpInterfaceEntry(ref pRoute) != 0)
            {
                throw new Exception("Can't get interface via GetIpInterfaceEntry");
            }

            pRoute.UseAutomaticMetric = (byte)(automatic ? 1 : 0);
            pRoute.Metric = metric;
            pRoute.SitePrefixLength = 0u;
            if (NativeMethods.SetIpInterfaceEntry(ref pRoute) != 0)
            {
                throw new Exception("Can't set new metric on interface via SetIpInterfaceEntry");
            }
        }

        public void Disable()
        {
            RunNetShInterface("interface set interface name=\"{0}\" admin=disable");
        }

        public void PrepareConfiguration()
        {
            using NetCfg netCfg = NetCfg.CreateInstance();
            foreach (Adapter item in from adapter in Adapter.GetAdapters(netCfg.Get())
                     where adapter.GetId() == "phantomtap"
                     select adapter)
            {
                item.Enable("ms_tcpip");
                item.Enable("ms_tcpip6");
            }

            if (netCfg.Get().Apply() != 0)
            {
                throw new Exception("INetCfg::Apply failed");
            }
        }

        public void ChooseMetric()
        {
            try
            {
                selectedInterface = SelectInterfaceWithMinimumMetric();
                uint networkInterfaceMetric =
                    GetNetworkInterfaceMetric((uint)Interface.GetIPProperties().GetIPv4Properties().Index);
                SetNetworkInterfaceMetric((uint)Interface.GetIPProperties().GetIPv4Properties().Index, 1u,
                    automatic: false);
                SetNetworkInterfaceMetric((uint)selectedInterface.GetIPProperties().GetIPv4Properties().Index,
                    networkInterfaceMetric, automatic: false);
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to SetNetworkInterfaceMetric.");
            }
        }

        public void RestoreMetric()
        {
            if (selectedInterface != null)
            {
                try
                {
                    SetNetworkInterfaceMetric((uint)Interface.GetIPProperties().GetIPv4Properties().Index, 1u,
                        automatic: true);
                    SetNetworkInterfaceMetric((uint)selectedInterface.GetIPProperties().GetIPv4Properties().Index, 1u,
                        automatic: true);
                }
                catch (Exception exception)
                {
                    Log.Warning(exception, "Failed to set network interface metric.");
                }
            }
        }

        public void ResetInterface()
        {
            tunInterface = null;
        }

        private static string GetName()
        {
            try
            {
                return GetAdapter().Name;
            }
            catch (VpnException)
            {
                return GetAdapterNameWmi();
            }
        }

        private static string GetAdapterNameWmi()
        {
            foreach (ManagementBaseObject item in
                     new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter").Get())
            {
                if (tunNameRegEx.IsMatch((string)item["Name"]))
                {
                    return (string)item["NetConnectionID"];
                }
            }

            throw new VpnException("Couldn't find Tap Adapter. It is not present", ErrorType.TapAdapterError);
        }

        private static NetworkInterface GetAdapter()
        {
            using (IEnumerator<NetworkInterface> enumerator =
                   (from networkInterface in NetworkInterface.GetAllNetworkInterfaces()
                       where networkInterface.Description.Equals("Phantom TAP-Windows Adapter V9")
                       select networkInterface).GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    return enumerator.Current;
                }
            }

            throw new VpnException("Couldn't find Tap Adapter. It is not present.", ErrorType.TapAdapterError);
        }

        private void RunNetShInterface(string argumentsFormatString)
        {
            if (RunNetSh(string.Format(argumentsFormatString, Name)) != 0)
            {
                name = GetName();
                if (RunNetSh(string.Format(argumentsFormatString, Name)) != 0)
                {
                    Log.Warning("netsh command failed: " + string.Format(argumentsFormatString, Name));
                }
            }
        }

        private int RunNetSh(string arguments)
        {
            using ProcessWrapper process = new ProcessWrapper(new Process());
            return new ProcessRunner(process, whiteList)
            {
                FileName = Path.Combine(Environment.SystemDirectory, "netsh"),
                Arguments = arguments
            }.StartAndWaitForExit();
        }
    }
}