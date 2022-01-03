using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Avira.Common.Core;

namespace NativeWifi
{
    public class WlanClient : IDisposable
    {
        public class WlanInterface
        {
            public delegate void WlanNotificationEventHandler(Wlan.WlanNotificationData notifyData);

            public delegate void WlanConnectionNotificationEventHandler(Wlan.WlanNotificationData notifyData,
                Wlan.WlanConnectionNotificationData connNotifyData);

            public delegate void WlanReasonNotificationEventHandler(Wlan.WlanNotificationData notifyData,
                Wlan.WlanReasonCode reasonCode);

            private struct WlanConnectionNotificationEventData
            {
                public Wlan.WlanNotificationData NotifyData;

                public Wlan.WlanConnectionNotificationData ConnNotifyData;
            }

            private struct WlanReasonNotificationData
            {
                public Wlan.WlanNotificationData NotifyData;

                public Wlan.WlanReasonCode ReasonCode;
            }

            private readonly WlanClient client;

            private Wlan.WlanInterfaceInfo info;

            private bool queueEvents;

            private readonly AutoResetEvent eventQueueFilled = new AutoResetEvent(initialState: false);

            private readonly Queue<object> eventQueue = new Queue<object>();

            public bool Autoconf
            {
                get { return GetInterfaceInt(Wlan.WlanIntfOpcode.AutoconfEnabled) != 0; }
                set { SetInterfaceInt(Wlan.WlanIntfOpcode.AutoconfEnabled, value ? 1 : 0); }
            }

            public Wlan.Dot11BssType BssType
            {
                get { return (Wlan.Dot11BssType)GetInterfaceInt(Wlan.WlanIntfOpcode.BssType); }
                set { SetInterfaceInt(Wlan.WlanIntfOpcode.BssType, (int)value); }
            }

            public Wlan.WlanInterfaceState InterfaceState =>
                (Wlan.WlanInterfaceState)GetInterfaceInt(Wlan.WlanIntfOpcode.InterfaceState);

            public int Channel => GetInterfaceInt(Wlan.WlanIntfOpcode.ChannelNumber);

            public int Rssi => GetInterfaceInt(Wlan.WlanIntfOpcode.RSSI);

            public Wlan.WlanRadioState RadioState
            {
                get
                {
                    Wlan.ThrowIfError(Wlan.WlanQueryInterface(client.clientHandle, info.interfaceGuid,
                        Wlan.WlanIntfOpcode.RadioState, IntPtr.Zero, out var _, out var ppData, out var _));
                    try
                    {
                        return (Wlan.WlanRadioState)Marshal.PtrToStructure(ppData, typeof(Wlan.WlanRadioState));
                    }
                    finally
                    {
                        Wlan.WlanFreeMemory(ppData);
                    }
                }
            }

            public Wlan.Dot11OperationMode CurrentOperationMode =>
                (Wlan.Dot11OperationMode)GetInterfaceInt(Wlan.WlanIntfOpcode.CurrentOperationMode);

            public Wlan.WlanConnectionAttributes CurrentConnection
            {
                get
                {
                    Wlan.ThrowIfError(Wlan.WlanQueryInterface(client.clientHandle, info.interfaceGuid,
                        Wlan.WlanIntfOpcode.CurrentConnection, IntPtr.Zero, out var _, out var ppData, out var _));
                    try
                    {
                        return (Wlan.WlanConnectionAttributes)Marshal.PtrToStructure(ppData,
                            typeof(Wlan.WlanConnectionAttributes));
                    }
                    finally
                    {
                        Wlan.WlanFreeMemory(ppData);
                    }
                }
            }

            public NetworkInterface NetworkInterface
            {
                get
                {
                    NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface networkInterface in allNetworkInterfaces)
                    {
                        if (new Guid(networkInterface.Id).Equals(info.interfaceGuid))
                        {
                            return networkInterface;
                        }
                    }

                    return null;
                }
            }

            public Guid InterfaceGuid => info.interfaceGuid;

            public string InterfaceDescription => info.interfaceDescription;

            public string InterfaceName => NetworkInterface.Name;

            public event WlanNotificationEventHandler WlanNotification;

            public event WlanConnectionNotificationEventHandler WlanConnectionNotification;

            public event WlanReasonNotificationEventHandler WlanReasonNotification;

            internal WlanInterface(WlanClient client, Wlan.WlanInterfaceInfo info)
            {
                this.client = client;
                this.info = info;
            }

            private void SetInterfaceInt(Wlan.WlanIntfOpcode opCode, int value)
            {
                IntPtr intPtr = Marshal.AllocHGlobal(4);
                Marshal.WriteInt32(intPtr, value);
                try
                {
                    Wlan.ThrowIfError(Wlan.WlanSetInterface(client.clientHandle, info.interfaceGuid, opCode, 4u, intPtr,
                        IntPtr.Zero));
                }
                finally
                {
                    Marshal.FreeHGlobal(intPtr);
                }
            }

            private int GetInterfaceInt(Wlan.WlanIntfOpcode opCode)
            {
                Wlan.ThrowIfError(Wlan.WlanQueryInterface(client.clientHandle, info.interfaceGuid, opCode, IntPtr.Zero,
                    out var _, out var ppData, out var _));
                try
                {
                    return Marshal.ReadInt32(ppData);
                }
                finally
                {
                    Wlan.WlanFreeMemory(ppData);
                }
            }

            public void Scan()
            {
                Wlan.ThrowIfError(Wlan.WlanScan(client.clientHandle, info.interfaceGuid, IntPtr.Zero, IntPtr.Zero,
                    IntPtr.Zero));
            }

            private static Wlan.WlanAvailableNetwork[] ConvertAvailableNetworkListPtr(IntPtr availNetListPtr)
            {
                Wlan.WlanAvailableNetworkListHeader wlanAvailableNetworkListHeader =
                    (Wlan.WlanAvailableNetworkListHeader)Marshal.PtrToStructure(availNetListPtr,
                        typeof(Wlan.WlanAvailableNetworkListHeader));
                long num = availNetListPtr.ToInt64() + Marshal.SizeOf(typeof(Wlan.WlanAvailableNetworkListHeader));
                Wlan.WlanAvailableNetwork[] array =
                    new Wlan.WlanAvailableNetwork[wlanAvailableNetworkListHeader.numberOfItems];
                for (int i = 0; i < wlanAvailableNetworkListHeader.numberOfItems; i++)
                {
                    array[i] = (Wlan.WlanAvailableNetwork)Marshal.PtrToStructure(new IntPtr(num),
                        typeof(Wlan.WlanAvailableNetwork));
                    num += Marshal.SizeOf(typeof(Wlan.WlanAvailableNetwork));
                }

                return array;
            }

            public Wlan.WlanAvailableNetwork[] GetAvailableNetworkList(Wlan.WlanGetAvailableNetworkFlags flags)
            {
                Wlan.ThrowIfError(Wlan.WlanGetAvailableNetworkList(client.clientHandle, info.interfaceGuid, flags,
                    IntPtr.Zero, out var availableNetworkListPtr));
                try
                {
                    return ConvertAvailableNetworkListPtr(availableNetworkListPtr);
                }
                finally
                {
                    Wlan.WlanFreeMemory(availableNetworkListPtr);
                }
            }

            private static Wlan.WlanBssEntry[] ConvertBssListPtr(IntPtr bssListPtr)
            {
                Wlan.WlanBssListHeader wlanBssListHeader =
                    (Wlan.WlanBssListHeader)Marshal.PtrToStructure(bssListPtr, typeof(Wlan.WlanBssListHeader));
                long num = bssListPtr.ToInt64() + Marshal.SizeOf(typeof(Wlan.WlanBssListHeader));
                Wlan.WlanBssEntry[] array = new Wlan.WlanBssEntry[wlanBssListHeader.numberOfItems];
                for (int i = 0; i < wlanBssListHeader.numberOfItems; i++)
                {
                    array[i] = (Wlan.WlanBssEntry)Marshal.PtrToStructure(new IntPtr(num), typeof(Wlan.WlanBssEntry));
                    num += Marshal.SizeOf(typeof(Wlan.WlanBssEntry));
                }

                return array;
            }

            public Wlan.WlanBssEntry[] GetNetworkBssList()
            {
                Wlan.ThrowIfError(Wlan.WlanGetNetworkBssList(client.clientHandle, info.interfaceGuid, IntPtr.Zero,
                    Wlan.Dot11BssType.Any, securityEnabled: false, IntPtr.Zero, out var wlanBssList));
                try
                {
                    return ConvertBssListPtr(wlanBssList);
                }
                finally
                {
                    Wlan.WlanFreeMemory(wlanBssList);
                }
            }

            public Wlan.WlanBssEntry[] GetNetworkBssList(Wlan.Dot11Ssid ssid, Wlan.Dot11BssType bssType,
                bool securityEnabled)
            {
                IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(ssid));
                Marshal.StructureToPtr(ssid, intPtr, fDeleteOld: false);
                try
                {
                    Wlan.ThrowIfError(Wlan.WlanGetNetworkBssList(client.clientHandle, info.interfaceGuid, intPtr,
                        bssType, securityEnabled, IntPtr.Zero, out var wlanBssList));
                    try
                    {
                        return ConvertBssListPtr(wlanBssList);
                    }
                    finally
                    {
                        Wlan.WlanFreeMemory(wlanBssList);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(intPtr);
                }
            }

            protected void Connect(Wlan.WlanConnectionParameters connectionParams)
            {
                Wlan.ThrowIfError(Wlan.WlanConnect(client.clientHandle, info.interfaceGuid, ref connectionParams,
                    IntPtr.Zero));
            }

            public void Connect(Wlan.WlanConnectionMode connectionMode, Wlan.Dot11BssType bssType, string profile)
            {
                Wlan.WlanConnectionParameters connectionParams = default(Wlan.WlanConnectionParameters);
                connectionParams.wlanConnectionMode = connectionMode;
                connectionParams.profile = profile;
                connectionParams.dot11BssType = bssType;
                connectionParams.flags = (Wlan.WlanConnectionFlags)0;
                Connect(connectionParams);
            }

            public bool ConnectSynchronously(Wlan.WlanConnectionMode connectionMode, Wlan.Dot11BssType bssType,
                string profile, int connectTimeout)
            {
                queueEvents = true;
                try
                {
                    Connect(connectionMode, bssType, profile);
                    while (queueEvents && eventQueueFilled.WaitOne(connectTimeout, exitContext: true))
                    {
                        lock (eventQueue)
                        {
                            while (eventQueue.Count != 0)
                            {
                                object obj = eventQueue.Dequeue();
                                if (obj is WlanConnectionNotificationEventData)
                                {
                                    WlanConnectionNotificationEventData wlanConnectionNotificationEventData =
                                        (WlanConnectionNotificationEventData)obj;
                                    if (wlanConnectionNotificationEventData.NotifyData.notificationSource ==
                                        Wlan.WlanNotificationSource.ACM)
                                    {
                                        Wlan.WlanNotificationCodeAcm notificationCode =
                                            (Wlan.WlanNotificationCodeAcm)wlanConnectionNotificationEventData.NotifyData
                                                .notificationCode;
                                        if (notificationCode == Wlan.WlanNotificationCodeAcm.ConnectionComplete &&
                                            wlanConnectionNotificationEventData.ConnNotifyData.profileName == profile)
                                        {
                                            return true;
                                        }

                                        break;
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    queueEvents = false;
                    lock (eventQueue)
                    {
                        eventQueue.Clear();
                    }
                }

                return false;
            }

            public void Connect(Wlan.WlanConnectionMode connectionMode, Wlan.Dot11BssType bssType, Wlan.Dot11Ssid ssid,
                Wlan.WlanConnectionFlags flags)
            {
                Wlan.WlanConnectionParameters connectionParams = default(Wlan.WlanConnectionParameters);
                connectionParams.wlanConnectionMode = connectionMode;
                connectionParams.dot11SsidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(ssid));
                Marshal.StructureToPtr(ssid, connectionParams.dot11SsidPtr, fDeleteOld: false);
                connectionParams.dot11BssType = bssType;
                connectionParams.flags = flags;
                Connect(connectionParams);
                Marshal.DestroyStructure(connectionParams.dot11SsidPtr, ssid.GetType());
                Marshal.FreeHGlobal(connectionParams.dot11SsidPtr);
            }

            public void DeleteProfile(string profileName)
            {
                Wlan.ThrowIfError(Wlan.WlanDeleteProfile(client.clientHandle, info.interfaceGuid, profileName,
                    IntPtr.Zero));
            }

            public Wlan.WlanReasonCode SetProfile(Wlan.WlanProfileFlags flags, string profileXml, bool overwrite)
            {
                Wlan.ThrowIfError(Wlan.WlanSetProfile(client.clientHandle, info.interfaceGuid, flags, profileXml, null,
                    overwrite, IntPtr.Zero, out var reasonCode));
                return reasonCode;
            }

            public string GetProfileXml(string profileName)
            {
                Wlan.WlanProfileFlags flags = Wlan.WlanProfileFlags.GetPlaintextKey;
                Wlan.ThrowIfError(Wlan.WlanGetProfile(client.clientHandle, info.interfaceGuid, profileName, IntPtr.Zero,
                    out var profileXml, out flags, out var _));
                try
                {
                    return Marshal.PtrToStringUni(profileXml);
                }
                finally
                {
                    Wlan.WlanFreeMemory(profileXml);
                }
            }

            public Wlan.WlanProfileInfo[] GetProfiles()
            {
                Wlan.ThrowIfError(Wlan.WlanGetProfileList(client.clientHandle, info.interfaceGuid, IntPtr.Zero,
                    out var profileList));
                try
                {
                    Wlan.WlanProfileInfoListHeader structure =
                        (Wlan.WlanProfileInfoListHeader)Marshal.PtrToStructure(profileList,
                            typeof(Wlan.WlanProfileInfoListHeader));
                    Wlan.WlanProfileInfo[] array = new Wlan.WlanProfileInfo[structure.numberOfItems];
                    long num = profileList.ToInt64() + Marshal.SizeOf(structure);
                    for (int i = 0; i < structure.numberOfItems; i++)
                    {
                        num += Marshal.SizeOf(array[i] =
                            (Wlan.WlanProfileInfo)Marshal.PtrToStructure(new IntPtr(num),
                                typeof(Wlan.WlanProfileInfo)));
                    }

                    return array;
                }
                finally
                {
                    Wlan.WlanFreeMemory(profileList);
                }
            }

            internal void OnWlanConnection(Wlan.WlanNotificationData notifyData,
                Wlan.WlanConnectionNotificationData connNotifyData)
            {
                if (this.WlanConnectionNotification != null)
                {
                    this.WlanConnectionNotification(notifyData, connNotifyData);
                }

                if (queueEvents)
                {
                    WlanConnectionNotificationEventData wlanConnectionNotificationEventData =
                        default(WlanConnectionNotificationEventData);
                    wlanConnectionNotificationEventData.NotifyData = notifyData;
                    wlanConnectionNotificationEventData.ConnNotifyData = connNotifyData;
                    EnqueueEvent(wlanConnectionNotificationEventData);
                }
            }

            internal void OnWlanReason(Wlan.WlanNotificationData notifyData, Wlan.WlanReasonCode reasonCode)
            {
                this.WlanReasonNotification?.Invoke(notifyData, reasonCode);
                if (queueEvents)
                {
                    WlanReasonNotificationData wlanReasonNotificationData = default(WlanReasonNotificationData);
                    wlanReasonNotificationData.NotifyData = notifyData;
                    wlanReasonNotificationData.ReasonCode = reasonCode;
                    EnqueueEvent(wlanReasonNotificationData);
                }
            }

            internal void OnWlanNotification(Wlan.WlanNotificationData notifyData)
            {
                this.WlanNotification?.Invoke(notifyData);
            }

            private void EnqueueEvent(object queuedEvent)
            {
                lock (eventQueue)
                {
                    eventQueue.Enqueue(queuedEvent);
                }

                eventQueueFilled.Set();
            }
        }

        private IntPtr clientHandle;

        private readonly Dictionary<Guid, WlanInterface> ifaces = new Dictionary<Guid, WlanInterface>();

        private Wlan.WlanNotificationCallbackDelegate callback;

        public WlanInterface[] Interfaces
        {
            get
            {
                Wlan.ThrowIfError(Wlan.WlanEnumInterfaces(clientHandle, IntPtr.Zero, out var ppInterfaceList));
                try
                {
                    Wlan.WlanInterfaceInfoListHeader structure =
                        (Wlan.WlanInterfaceInfoListHeader)Marshal.PtrToStructure(ppInterfaceList,
                            typeof(Wlan.WlanInterfaceInfoListHeader));
                    long num = ppInterfaceList.ToInt64() + Marshal.SizeOf(structure);
                    WlanInterface[] array = new WlanInterface[structure.numberOfItems];
                    List<Guid> list = new List<Guid>();
                    for (int i = 0; i < structure.numberOfItems; i++)
                    {
                        Wlan.WlanInterfaceInfo wlanInterfaceInfo =
                            (Wlan.WlanInterfaceInfo)Marshal.PtrToStructure(new IntPtr(num),
                                typeof(Wlan.WlanInterfaceInfo));
                        num += Marshal.SizeOf(wlanInterfaceInfo);
                        list.Add(wlanInterfaceInfo.interfaceGuid);
                        if (!ifaces.TryGetValue(wlanInterfaceInfo.interfaceGuid, out var value))
                        {
                            value = new WlanInterface(this, wlanInterfaceInfo);
                            ifaces[wlanInterfaceInfo.interfaceGuid] = value;
                        }

                        array[i] = value;
                    }

                    Queue<Guid> queue = new Queue<Guid>();
                    foreach (Guid key2 in ifaces.Keys)
                    {
                        if (!list.Contains(key2))
                        {
                            queue.Enqueue(key2);
                        }
                    }

                    while (queue.Count != 0)
                    {
                        Guid key = queue.Dequeue();
                        ifaces.Remove(key);
                    }

                    return array;
                }
                finally
                {
                    Wlan.WlanFreeMemory(ppInterfaceList);
                }
            }
        }

        public WlanClient()
        {
            callback = TryOnWlanNotification;
            Wlan.ThrowIfError(Wlan.WlanOpenHandle(1u, IntPtr.Zero, out var _, out clientHandle));
            try
            {
                Wlan.ThrowIfError(Wlan.WlanRegisterNotification(clientHandle, Wlan.WlanNotificationSource.All,
                    ignoreDuplicate: false, callback, IntPtr.Zero, IntPtr.Zero, out var _));
            }
            catch
            {
                Close();
                throw;
            }
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            Close();
        }

        ~WlanClient()
        {
            Close();
        }

        private void Close()
        {
            if (clientHandle != IntPtr.Zero)
            {
                Wlan.WlanCloseHandle(clientHandle, IntPtr.Zero);
                clientHandle = IntPtr.Zero;
            }
        }

        private static Wlan.WlanConnectionNotificationData? ParseWlanConnectionNotification(
            ref Wlan.WlanNotificationData notifyData)
        {
            int num = Marshal.SizeOf(typeof(Wlan.WlanConnectionNotificationData));
            if (notifyData.dataSize < num)
            {
                return null;
            }

            Wlan.WlanConnectionNotificationData value =
                (Wlan.WlanConnectionNotificationData)Marshal.PtrToStructure(notifyData.dataPtr,
                    typeof(Wlan.WlanConnectionNotificationData));
            if (value.wlanReasonCode == Wlan.WlanReasonCode.Success)
            {
                IntPtr ptr = new IntPtr(notifyData.dataPtr.ToInt64() +
                                        Marshal.OffsetOf(typeof(Wlan.WlanConnectionNotificationData), "profileXml")
                                            .ToInt64());
                value.profileXml = Marshal.PtrToStringUni(ptr);
            }

            return value;
        }

        private void TryOnWlanNotification(ref Wlan.WlanNotificationData notifyData, IntPtr context)
        {
            try
            {
                OnWlanNotification(ref notifyData, context);
            }
            catch (Exception ex)
            {
                Log.Warning(ex);
            }
        }

        private void OnWlanNotification(ref Wlan.WlanNotificationData notifyData, IntPtr context)
        {
            ifaces.TryGetValue(notifyData.interfaceGuid, out var value);
            switch (notifyData.notificationSource)
            {
                case Wlan.WlanNotificationSource.ACM:
                    switch (notifyData.notificationCode)
                    {
                        case 9:
                        case 10:
                        case 11:
                        case 20:
                        case 21:
                        {
                            Wlan.WlanConnectionNotificationData? wlanConnectionNotificationData2 =
                                ParseWlanConnectionNotification(ref notifyData);
                            if (wlanConnectionNotificationData2.HasValue)
                            {
                                value?.OnWlanConnection(notifyData, wlanConnectionNotificationData2.Value);
                            }

                            break;
                        }
                        case 8:
                        {
                            int num = Marshal.SizeOf(typeof(int));
                            if (notifyData.dataSize >= num)
                            {
                                Wlan.WlanReasonCode reasonCode =
                                    (Wlan.WlanReasonCode)Marshal.ReadInt32(notifyData.dataPtr);
                                value?.OnWlanReason(notifyData, reasonCode);
                            }

                            break;
                        }
                    }

                    break;
                case Wlan.WlanNotificationSource.MSM:
                {
                    Wlan.WlanNotificationCodeMsm notificationCode =
                        (Wlan.WlanNotificationCodeMsm)notifyData.notificationCode;
                    if ((uint)(notificationCode - 1) <= 5u || (uint)(notificationCode - 9) <= 4u)
                    {
                        Wlan.WlanConnectionNotificationData? wlanConnectionNotificationData =
                            ParseWlanConnectionNotification(ref notifyData);
                        if (wlanConnectionNotificationData.HasValue)
                        {
                            value?.OnWlanConnection(notifyData, wlanConnectionNotificationData.Value);
                        }
                    }

                    break;
                }
            }

            value?.OnWlanNotification(notifyData);
        }

        public string GetStringForReasonCode(Wlan.WlanReasonCode reasonCode)
        {
            StringBuilder stringBuilder = new StringBuilder(1024);
            Wlan.ThrowIfError(Wlan.WlanReasonCodeToString(reasonCode, stringBuilder.Capacity, stringBuilder,
                IntPtr.Zero));
            return stringBuilder.ToString();
        }
    }
}