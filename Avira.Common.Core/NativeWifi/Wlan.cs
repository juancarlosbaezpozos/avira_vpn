using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace NativeWifi
{
    public static class Wlan
    {
        public enum WlanIntfOpcode
        {
            AutoconfEnabled = 1,
            BackgroundScanEnabled = 2,
            MediaStreamingMode = 3,
            RadioState = 4,
            BssType = 5,
            InterfaceState = 6,
            CurrentConnection = 7,
            ChannelNumber = 8,
            SupportedInfrastructureAuthCipherPairs = 9,
            SupportedAdhocAuthCipherPairs = 10,
            SupportedCountryOrRegionStringList = 11,
            CurrentOperationMode = 12,
            Statistics = 268435713,
            RSSI = 268435714,
            SecurityStart = 536936448,
            SecurityEnd = 805306367,
            IhvStart = 805306368,
            IhvEnd = 0x3FFFFFFF
        }

        public enum WlanOpcodeValueType
        {
            QueryOnly,
            SetByGroupPolicy,
            SetByUser,
            Invalid
        }

        [Flags]
        public enum WlanGetAvailableNetworkFlags
        {
            IncludeAllAdhocProfiles = 0x1,
            IncludeAllManualHiddenProfiles = 0x2
        }

        internal struct WlanAvailableNetworkListHeader
        {
            public uint numberOfItems;

            public uint index;
        }

        [Flags]
        public enum WlanAvailableNetworkFlags
        {
            Connected = 0x1,
            HasProfile = 0x2
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WlanAvailableNetwork
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string profileName;

            public Dot11Ssid dot11Ssid;

            public Dot11BssType dot11BssType;

            public uint numberOfBssids;

            public bool networkConnectable;

            public WlanReasonCode wlanNotConnectableReason;

            private uint numberOfPhyTypes;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private Dot11PhyType[] dot11PhyTypes;

            public bool morePhyTypes;

            public uint wlanSignalQuality;

            public bool securityEnabled;

            public Dot11AuthAlgorithm dot11DefaultAuthAlgorithm;

            public Dot11CipherAlgorithm dot11DefaultCipherAlgorithm;

            public WlanAvailableNetworkFlags flags;

            private uint reserved;

            public Dot11PhyType[] Dot11PhyTypes
            {
                get
                {
                    Dot11PhyType[] array = new Dot11PhyType[numberOfPhyTypes];
                    Array.Copy(dot11PhyTypes, array, numberOfPhyTypes);
                    return array;
                }
            }
        }

        [Flags]
        public enum WlanProfileFlags
        {
            AllUser = 0x0,
            GroupPolicy = 0x1,
            User = 0x2,
            GetPlaintextKey = 0x4
        }

        [Flags]
        public enum WlanAccess
        {
            ReadAccess = 0x20001,
            ExecuteAccess = 0x20021,
            WriteAccess = 0x70023
        }

        [Flags]
        public enum WlanNotificationSource
        {
            None = 0x0,
            All = 0xFFFF,
            ACM = 0x8,
            MSM = 0x10,
            Security = 0x20,
            IHV = 0x40
        }

        public enum WlanNotificationCodeAcm
        {
            AutoconfEnabled = 1,
            AutoconfDisabled,
            BackgroundScanEnabled,
            BackgroundScanDisabled,
            BssTypeChange,
            PowerSettingChange,
            ScanComplete,
            ScanFail,
            ConnectionStart,
            ConnectionComplete,
            ConnectionAttemptFail,
            FilterListChange,
            InterfaceArrival,
            InterfaceRemoval,
            ProfileChange,
            ProfileNameChange,
            ProfilesExhausted,
            NetworkNotAvailable,
            NetworkAvailable,
            Disconnecting,
            Disconnected,
            AdhocNetworkStateChange
        }

        public enum WlanNotificationCodeMsm
        {
            Associating = 1,
            Associated,
            Authenticating,
            Connected,
            RoamingStart,
            RoamingEnd,
            RadioStateChange,
            SignalQualityChange,
            Disassociating,
            Disconnected,
            PeerJoin,
            PeerLeave,
            AdapterRemoval,
            AdapterOperationModeChange
        }

        public struct WlanNotificationData
        {
            public WlanNotificationSource notificationSource;

            public int notificationCode;

            public Guid interfaceGuid;

            public int dataSize;

            public IntPtr dataPtr;

            public object NotificationCode => notificationSource switch
            {
                WlanNotificationSource.MSM => (WlanNotificationCodeMsm)notificationCode,
                WlanNotificationSource.ACM => (WlanNotificationCodeAcm)notificationCode,
                _ => notificationCode,
            };
        }

        public delegate void
            WlanNotificationCallbackDelegate(ref WlanNotificationData notificationData, IntPtr context);

        [Flags]
        public enum WlanConnectionFlags
        {
            HiddenNetwork = 0x1,
            AdhocJoinOnly = 0x2,
            IgnorePrivacyBit = 0x4,
            EapolPassthrough = 0x8
        }

        public struct WlanConnectionParameters
        {
            public WlanConnectionMode wlanConnectionMode;

            [MarshalAs(UnmanagedType.LPWStr)] public string profile;

            public IntPtr dot11SsidPtr;

            public IntPtr desiredBssidListPtr;

            public Dot11BssType dot11BssType;

            public WlanConnectionFlags flags;
        }

        public enum WlanAdhocNetworkState
        {
            Formed,
            Connected
        }

        internal struct WlanBssListHeader
        {
            internal uint totalSize;

            internal uint numberOfItems;
        }

        public struct WlanBssEntry
        {
            public Dot11Ssid dot11Ssid;

            public uint phyId;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] dot11Bssid;

            public Dot11BssType dot11BssType;

            public Dot11PhyType dot11BssPhyType;

            public int rssi;

            public uint linkQuality;

            public bool inRegDomain;

            public ushort beaconPeriod;

            public ulong timestamp;

            public ulong hostTimestamp;

            public ushort capabilityInformation;

            public uint chCenterFrequency;

            public WlanRateSet wlanRateSet;

            public uint ieOffset;

            public uint ieSize;
        }

        public struct WlanRateSet
        {
            private uint rateSetLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 126)]
            private ushort[] rateSet;

            public ushort[] Rates
            {
                get
                {
                    ushort[] array = new ushort[rateSetLength / 2u];
                    Array.Copy(rateSet, array, array.Length);
                    return array;
                }
            }

            public double GetRateInMbps(int rateIndex)
            {
                if (rateIndex < 0 || rateIndex > rateSet.Length)
                {
                    throw new ArgumentOutOfRangeException("rateIndex");
                }

                return (double)(rateSet[rateIndex] & 0x7FFF) * 0.5;
            }
        }

        public class WlanException : Exception
        {
            private readonly WlanReasonCode reasonCode;

            public WlanReasonCode ReasonCode => reasonCode;

            public override string Message
            {
                get
                {
                    StringBuilder stringBuilder = new StringBuilder(1024);
                    if (WlanReasonCodeToString(reasonCode, stringBuilder.Capacity, stringBuilder, IntPtr.Zero) != 0)
                    {
                        return string.Empty;
                    }

                    return stringBuilder.ToString();
                }
            }

            public WlanException(WlanReasonCode reasonCode)
            {
                this.reasonCode = reasonCode;
            }
        }

        public enum WlanReasonCode
        {
            Success = 0,
            UNKNOWN = 65537,
            RANGE_SIZE = 0x10000,
            BASE = 0x20000,
            AC_BASE = 0x20000,
            AC_CONNECT_BASE = 163840,
            AC_END = 196607,
            PROFILE_BASE = 0x80000,
            PROFILE_CONNECT_BASE = 557056,
            PROFILE_END = 589823,
            MSM_BASE = 196608,
            MSM_CONNECT_BASE = 229376,
            MSM_END = 0x3FFFF,
            MSMSEC_BASE = 0x40000,
            MSMSEC_CONNECT_BASE = 294912,
            MSMSEC_END = 327679,
            NETWORK_NOT_COMPATIBLE = 131073,
            PROFILE_NOT_COMPATIBLE = 131074,
            NO_AUTO_CONNECTION = 163841,
            NOT_VISIBLE = 163842,
            GP_DENIED = 163843,
            USER_DENIED = 163844,
            BSS_TYPE_NOT_ALLOWED = 163845,
            IN_FAILED_LIST = 163846,
            IN_BLOCKED_LIST = 163847,
            SSID_LIST_TOO_LONG = 163848,
            CONNECT_CALL_FAIL = 163849,
            SCAN_CALL_FAIL = 163850,
            NETWORK_NOT_AVAILABLE = 163851,
            PROFILE_CHANGED_OR_DELETED = 163852,
            KEY_MISMATCH = 163853,
            USER_NOT_RESPOND = 163854,
            INVALID_PROFILE_SCHEMA = 524289,
            PROFILE_MISSING = 524290,
            INVALID_PROFILE_NAME = 524291,
            INVALID_PROFILE_TYPE = 524292,
            INVALID_PHY_TYPE = 524293,
            MSM_SECURITY_MISSING = 524294,
            IHV_SECURITY_NOT_SUPPORTED = 524295,
            IHV_OUI_MISMATCH = 524296,
            IHV_OUI_MISSING = 524297,
            IHV_SETTINGS_MISSING = 524298,
            CONFLICT_SECURITY = 524299,
            SECURITY_MISSING = 524300,
            INVALID_BSS_TYPE = 524301,
            INVALID_ADHOC_CONNECTION_MODE = 524302,
            NON_BROADCAST_SET_FOR_ADHOC = 524303,
            AUTO_SWITCH_SET_FOR_ADHOC = 524304,
            AUTO_SWITCH_SET_FOR_MANUAL_CONNECTION = 524305,
            IHV_SECURITY_ONEX_MISSING = 524306,
            PROFILE_SSID_INVALID = 524307,
            TOO_MANY_SSID = 524308,
            UNSUPPORTED_SECURITY_SET_BY_OS = 196609,
            UNSUPPORTED_SECURITY_SET = 196610,
            BSS_TYPE_UNMATCH = 196611,
            PHY_TYPE_UNMATCH = 196612,
            DATARATE_UNMATCH = 196613,
            USER_CANCELLED = 229377,
            ASSOCIATION_FAILURE = 229378,
            ASSOCIATION_TIMEOUT = 229379,
            PRE_SECURITY_FAILURE = 229380,
            START_SECURITY_FAILURE = 229381,
            SECURITY_FAILURE = 229382,
            SECURITY_TIMEOUT = 229383,
            ROAMING_FAILURE = 229384,
            ROAMING_SECURITY_FAILURE = 229385,
            ADHOC_SECURITY_FAILURE = 229386,
            DRIVER_DISCONNECTED = 229387,
            DRIVER_OPERATION_FAILURE = 229388,
            IHV_NOT_AVAILABLE = 229389,
            IHV_NOT_RESPONDING = 229390,
            DISCONNECT_TIMEOUT = 229391,
            INTERNAL_FAILURE = 229392,
            UI_REQUEST_TIMEOUT = 229393,
            TOO_MANY_SECURITY_ATTEMPTS = 229394,
            MSMSEC_MIN = 0x40000,
            MSMSEC_PROFILE_INVALID_KEY_INDEX = 262145,
            MSMSEC_PROFILE_PSK_PRESENT = 262146,
            MSMSEC_PROFILE_KEY_LENGTH = 262147,
            MSMSEC_PROFILE_PSK_LENGTH = 262148,
            MSMSEC_PROFILE_NO_AUTH_CIPHER_SPECIFIED = 262149,
            MSMSEC_PROFILE_TOO_MANY_AUTH_CIPHER_SPECIFIED = 262150,
            MSMSEC_PROFILE_DUPLICATE_AUTH_CIPHER = 262151,
            MSMSEC_PROFILE_RAWDATA_INVALID = 262152,
            MSMSEC_PROFILE_INVALID_AUTH_CIPHER = 262153,
            MSMSEC_PROFILE_ONEX_DISABLED = 262154,
            MSMSEC_PROFILE_ONEX_ENABLED = 262155,
            MSMSEC_PROFILE_INVALID_PMKCACHE_MODE = 262156,
            MSMSEC_PROFILE_INVALID_PMKCACHE_SIZE = 262157,
            MSMSEC_PROFILE_INVALID_PMKCACHE_TTL = 262158,
            MSMSEC_PROFILE_INVALID_PREAUTH_MODE = 262159,
            MSMSEC_PROFILE_INVALID_PREAUTH_THROTTLE = 262160,
            MSMSEC_PROFILE_PREAUTH_ONLY_ENABLED = 262161,
            MSMSEC_CAPABILITY_NETWORK = 262162,
            MSMSEC_CAPABILITY_NIC = 262163,
            MSMSEC_CAPABILITY_PROFILE = 262164,
            MSMSEC_CAPABILITY_DISCOVERY = 262165,
            MSMSEC_PROFILE_PASSPHRASE_CHAR = 262166,
            MSMSEC_PROFILE_KEYMATERIAL_CHAR = 262167,
            MSMSEC_PROFILE_WRONG_KEYTYPE = 262168,
            MSMSEC_MIXED_CELL = 262169,
            MSMSEC_PROFILE_AUTH_TIMERS_INVALID = 262170,
            MSMSEC_PROFILE_INVALID_GKEY_INTV = 262171,
            MSMSEC_TRANSITION_NETWORK = 262172,
            MSMSEC_PROFILE_KEY_UNMAPPED_CHAR = 262173,
            MSMSEC_CAPABILITY_PROFILE_AUTH = 262174,
            MSMSEC_CAPABILITY_PROFILE_CIPHER = 262175,
            MSMSEC_UI_REQUEST_FAILURE = 294913,
            MSMSEC_AUTH_START_TIMEOUT = 294914,
            MSMSEC_AUTH_SUCCESS_TIMEOUT = 294915,
            MSMSEC_KEY_START_TIMEOUT = 294916,
            MSMSEC_KEY_SUCCESS_TIMEOUT = 294917,
            MSMSEC_M3_MISSING_KEY_DATA = 294918,
            MSMSEC_M3_MISSING_IE = 294919,
            MSMSEC_M3_MISSING_GRP_KEY = 294920,
            MSMSEC_PR_IE_MATCHING = 294921,
            MSMSEC_SEC_IE_MATCHING = 294922,
            MSMSEC_NO_PAIRWISE_KEY = 294923,
            MSMSEC_G1_MISSING_KEY_DATA = 294924,
            MSMSEC_G1_MISSING_GRP_KEY = 294925,
            MSMSEC_PEER_INDICATED_INSECURE = 294926,
            MSMSEC_NO_AUTHENTICATOR = 294927,
            MSMSEC_NIC_FAILURE = 294928,
            MSMSEC_CANCELLED = 294929,
            MSMSEC_KEY_FORMAT = 294930,
            MSMSEC_DOWNGRADE_DETECTED = 294931,
            MSMSEC_PSK_MISMATCH_SUSPECTED = 294932,
            MSMSEC_FORCED_FAILURE = 294933,
            MSMSEC_SECURITY_UI_FAILURE = 294934,
            MSMSEC_MAX = 327679
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WlanConnectionNotificationData
        {
            public WlanConnectionMode wlanConnectionMode;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string profileName;

            public Dot11Ssid dot11Ssid;

            public Dot11BssType dot11BssType;

            public bool securityEnabled;

            public WlanReasonCode wlanReasonCode;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
            public string profileXml;
        }

        public enum WlanInterfaceState
        {
            NotReady,
            Connected,
            AdHocNetworkFormed,
            Disconnecting,
            Disconnected,
            Associating,
            Discovering,
            Authenticating
        }

        public struct Dot11Ssid
        {
            public uint SSIDLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] SSID;
        }

        public enum Dot11PhyType : uint
        {
            Unknown = 0u,
            Any = 0u,
            FHSS = 1u,
            DSSS = 2u,
            IrBaseband = 3u,
            OFDM = 4u,
            HRDSSS = 5u,
            ERP = 6u,
            IHV_Start = 0x80000000u,
            IHV_End = uint.MaxValue
        }

        public enum Dot11BssType
        {
            Infrastructure = 1,
            Independent,
            Any
        }

        public struct WlanAssociationAttributes
        {
            public Dot11Ssid dot11Ssid;

            public Dot11BssType dot11BssType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] dot11Bssid;

            public Dot11PhyType dot11PhyType;

            public uint dot11PhyIndex;

            public uint wlanSignalQuality;

            public uint rxRate;

            public uint txRate;

            public PhysicalAddress Dot11Bssid => new PhysicalAddress(dot11Bssid);
        }

        public enum WlanConnectionMode
        {
            Profile,
            TemporaryProfile,
            DiscoverySecure,
            DiscoveryUnsecure,
            Auto,
            Invalid
        }

        public enum Dot11AuthAlgorithm : uint
        {
            IEEE80211_Open = 1u,
            IEEE80211_SharedKey = 2u,
            WPA = 3u,
            WPA_PSK = 4u,
            WPA_None = 5u,
            RSNA = 6u,
            RSNA_PSK = 7u,
            IHV_Start = 0x80000000u,
            IHV_End = uint.MaxValue
        }

        public enum Dot11CipherAlgorithm : uint
        {
            None = 0u,
            WEP40 = 1u,
            TKIP = 2u,
            CCMP = 4u,
            WEP104 = 5u,
            WPA_UseGroup = 0x100u,
            RSN_UseGroup = 0x100u,
            WEP = 257u,
            IHV_Start = 0x80000000u,
            IHV_End = uint.MaxValue
        }

        public struct WlanSecurityAttributes
        {
            [MarshalAs(UnmanagedType.Bool)] public bool securityEnabled;

            [MarshalAs(UnmanagedType.Bool)] public bool oneXEnabled;

            public Dot11AuthAlgorithm dot11AuthAlgorithm;

            public Dot11CipherAlgorithm dot11CipherAlgorithm;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WlanConnectionAttributes
        {
            public WlanInterfaceState isState;

            public WlanConnectionMode wlanConnectionMode;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string profileName;

            public WlanAssociationAttributes wlanAssociationAttributes;

            public WlanSecurityAttributes wlanSecurityAttributes;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WlanInterfaceInfo
        {
            public Guid interfaceGuid;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string interfaceDescription;

            public WlanInterfaceState isState;
        }

        internal struct WlanInterfaceInfoListHeader
        {
            public uint numberOfItems;

            public uint index;
        }

        internal struct WlanProfileInfoListHeader
        {
            public uint numberOfItems;

            public uint index;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WlanProfileInfo
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string profileName;

            public WlanProfileFlags profileFlags;
        }

        [Flags]
        public enum Dot11OperationMode : uint
        {
            Unknown = 0x0u,
            Station = 0x1u,
            AP = 0x2u,
            ExtensibleStation = 0x4u,
            NetworkMonitor = 0x80000000u
        }

        public enum Dot11RadioState : uint
        {
            Unknown,
            On,
            Off
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WlanPhyRadioState
        {
            public int dwPhyIndex;

            public Dot11RadioState dot11SoftwareRadioState;

            public Dot11RadioState dot11HardwareRadioState;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WlanRadioState
        {
            public int numberofItems;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            private WlanPhyRadioState[] phyRadioState;

            public WlanPhyRadioState[] PhyRadioState
            {
                get
                {
                    WlanPhyRadioState[] array = new WlanPhyRadioState[numberofItems];
                    Array.Copy(phyRadioState, array, numberofItems);
                    return array;
                }
            }
        }

        public const uint WLAN_CLIENT_VERSION_XP_SP2 = 1u;

        public const uint WLAN_CLIENT_VERSION_LONGHORN = 2u;

        [DllImport("wlanapi.dll")]
        internal static extern int WlanOpenHandle([In] uint clientVersion, [In] [Out] IntPtr pReserved,
            out uint negotiatedVersion, out IntPtr clientHandle);

        [DllImport("wlanapi.dll")]
        internal static extern int WlanCloseHandle([In] IntPtr clientHandle, [In] [Out] IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        internal static extern int WlanEnumInterfaces([In] IntPtr clientHandle, [In] [Out] IntPtr pReserved,
            out IntPtr ppInterfaceList);

        [DllImport("wlanapi.dll")]
        internal static extern int WlanQueryInterface([In] IntPtr clientHandle,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, [In] WlanIntfOpcode opCode,
            [In] [Out] IntPtr pReserved, out int dataSize, out IntPtr ppData,
            out WlanOpcodeValueType wlanOpcodeValueType);

        [DllImport("wlanapi.dll")]
        public static extern int WlanSetInterface([In] IntPtr clientHandle,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, [In] WlanIntfOpcode opCode, [In] uint dataSize,
            [In] IntPtr pData, [In] [Out] IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        public static extern int WlanScan([In] IntPtr clientHandle,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, [In] IntPtr pDot11Ssid, [In] IntPtr pIeData,
            [In] [Out] IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        public static extern int WlanGetAvailableNetworkList([In] IntPtr clientHandle,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, [In] WlanGetAvailableNetworkFlags flags,
            [In] [Out] IntPtr reservedPtr, out IntPtr availableNetworkListPtr);

        [DllImport("wlanapi.dll")]
        public static extern int WlanSetProfile([In] IntPtr clientHandle,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, [In] WlanProfileFlags flags,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string profileXml,
            [Optional] [In] [MarshalAs(UnmanagedType.LPWStr)] string allUserProfileSecurity, [In] bool overwrite,
            [In] IntPtr pReserved, out WlanReasonCode reasonCode);

        [DllImport("wlanapi.dll")]
        public static extern int WlanGetProfile([In] IntPtr clientHandle,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string profileName, [In] IntPtr pReserved, out IntPtr profileXml,
            [Optional] out WlanProfileFlags flags, [Optional] out WlanAccess grantedAccess);

        [DllImport("wlanapi.dll")]
        public static extern int WlanGetProfileList([In] IntPtr clientHandle,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, [In] IntPtr pReserved, out IntPtr profileList);

        [DllImport("wlanapi.dll")]
        public static extern void WlanFreeMemory(IntPtr pMemory);

        [DllImport("wlanapi.dll")]
        public static extern int WlanReasonCodeToString([In] WlanReasonCode reasonCode, [In] int bufferSize,
            [In] [Out] StringBuilder stringBuffer, IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        public static extern int WlanRegisterNotification([In] IntPtr clientHandle,
            [In] WlanNotificationSource notifSource, [In] bool ignoreDuplicate,
            [In] WlanNotificationCallbackDelegate funcCallback, [In] IntPtr callbackContext, [In] IntPtr reserved,
            out WlanNotificationSource prevNotifSource);

        [DllImport("wlanapi.dll")]
        public static extern int WlanConnect([In] IntPtr clientHandle,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In] ref WlanConnectionParameters connectionParameters, IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        public static extern int WlanDeleteProfile([In] IntPtr clientHandle,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string profileName, IntPtr reservedPtr);

        [DllImport("wlanapi.dll")]
        public static extern int WlanGetNetworkBssList([In] IntPtr clientHandle,
            [In] [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceGuid, [In] IntPtr dot11SsidInt,
            [In] Dot11BssType dot11BssType, [In] bool securityEnabled, IntPtr reservedPtr, out IntPtr wlanBssList);

        [DebuggerStepThrough]
        internal static void ThrowIfError(int win32ErrorCode)
        {
            if (win32ErrorCode != 0)
            {
                throw new Win32Exception(win32ErrorCode);
            }
        }
    }
}