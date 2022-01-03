using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using NativeWifi;

namespace Avira.Common.Core.Networking
{
    public class WifiNetworkMonitor : IWifiNetworkMonitor
    {
        private readonly WlanClient wlanClient;

        public event EventHandler<WifiConnectionEventArgs> StatusChanged;

        public WifiNetworkMonitor()
        {
            wlanClient = new WlanClient();
            RegisterForWifiConnectivity();
        }

        public WifiConnectionEventArgs GetConnectedWifi()
        {
            Wlan.Dot11Ssid currentConnectionSsid = GetCurrentConnectionSsid();
            if (currentConnectionSsid.SSID != null)
            {
                return new WifiConnectionEventArgs(
                    Encoding.Default.GetString(currentConnectionSsid.SSID, 0, (int)currentConnectionSsid.SSIDLength),
                    GetConnectionMode(currentConnectionSsid.SSID), WifiConnectionStatus.Connected);
            }

            return null;
        }

        private void RegisterForWifiConnectivity()
        {
            foreach (WlanClient.WlanInterface item in wlanClient.Interfaces.Where(
                         delegate(WlanClient.WlanInterface wlanInterface)
                         {
                             NetworkInterface networkInterface = wlanInterface.NetworkInterface;
                             return networkInterface != null && networkInterface.NetworkInterfaceType ==
                                 NetworkInterfaceType.Wireless80211;
                         }))
            {
                item.WlanConnectionNotification += WlanConnectionNotificationEventHandler;
            }
        }

        private void WlanConnectionNotificationEventHandler(Wlan.WlanNotificationData notifyData,
            Wlan.WlanConnectionNotificationData connNotifyData)
        {
            try
            {
                if (notifyData.notificationSource != Wlan.WlanNotificationSource.ACM)
                {
                    return;
                }

                if (IsConnectedNotification(notifyData))
                {
                    Wlan.WlanConnectionNotificationData wlanConnectionNotificationData =
                        (Wlan.WlanConnectionNotificationData)Marshal.PtrToStructure(notifyData.dataPtr,
                            typeof(Wlan.WlanConnectionNotificationData));
                    Wlan.Dot11Ssid currentConnectionSsid = GetCurrentConnectionSsid();
                    if (currentConnectionSsid.SSID == null)
                    {
                        Log.Warning("Could not retrieve SSID for " + wlanConnectionNotificationData.profileName);
                    }
                    else
                    {
                        this.StatusChanged?.Invoke(this,
                            new WifiConnectionEventArgs(wlanConnectionNotificationData.profileName,
                                GetConnectionMode(currentConnectionSsid.SSID), WifiConnectionStatus.Connected));
                    }
                }
                else if (IsDisconnectedNotification(notifyData))
                {
                    Wlan.WlanConnectionNotificationData wlanConnectionNotificationData2 =
                        (Wlan.WlanConnectionNotificationData)Marshal.PtrToStructure(notifyData.dataPtr,
                            typeof(Wlan.WlanConnectionNotificationData));
                    this.StatusChanged?.Invoke(this,
                        new WifiConnectionEventArgs(wlanConnectionNotificationData2.profileName,
                            WifiConnectionSecurityMode.Unknown, WifiConnectionStatus.Disconnected));
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex);
            }
        }

        public string GetProfileUniqueId(string profileName)
        {
            string profile = GetProfile(profileName);
            string text = (string.IsNullOrEmpty(profile) ? string.Empty : GetPassword(profile));
            return GetSha256(profileName + text);
        }

        private string GetProfile(string profileName)
        {
            string result = string.Empty;
            try
            {
                WlanClient.WlanInterface[] interfaces = wlanClient.Interfaces;
                foreach (WlanClient.WlanInterface wlanInterface in interfaces)
                {
                    if (wlanInterface.InterfaceState == Wlan.WlanInterfaceState.Connected)
                    {
                        result = wlanInterface.GetProfileXml(profileName);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Warning(ex);
                return result;
            }
        }

        private static string GetPassword(string profile)
        {
            string result = string.Empty;
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(profile);
                result = xmlDocument.GetElementsByTagName("keyMaterial")[0].InnerText;
                return result;
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
                return result;
            }
        }

        private static string GetSha256(string input)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (SHA256 sHA = SHA256.Create())
            {
                Encoding uTF = Encoding.UTF8;
                byte[] array = sHA.ComputeHash(uTF.GetBytes(input));
                foreach (byte b in array)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }
            }

            return stringBuilder.ToString();
        }

        private static bool IsConnectedNotification(Wlan.WlanNotificationData notifyData)
        {
            return string.Equals(notifyData.NotificationCode.ToString(),
                Wlan.WlanNotificationCodeAcm.ConnectionComplete.ToString());
        }

        private static bool IsDisconnectedNotification(Wlan.WlanNotificationData notifyData)
        {
            return string.Equals(notifyData.NotificationCode.ToString(),
                Wlan.WlanNotificationCodeAcm.Disconnected.ToString());
        }

        internal Wlan.Dot11Ssid GetCurrentConnectionSsid()
        {
            WlanClient.WlanInterface[] interfaces = wlanClient.Interfaces;
            foreach (WlanClient.WlanInterface wlanInterface in interfaces)
            {
                if (wlanInterface.InterfaceState == Wlan.WlanInterfaceState.Connected)
                {
                    return wlanInterface.CurrentConnection.wlanAssociationAttributes.dot11Ssid;
                }
            }

            return default(Wlan.Dot11Ssid);
        }

        private WifiConnectionSecurityMode GetConnectionMode(byte[] ssid)
        {
            WlanClient.WlanInterface[] interfaces = wlanClient.Interfaces;
            for (int i = 0; i < interfaces.Length; i++)
            {
                Wlan.WlanAvailableNetwork[] availableNetworkList =
                    interfaces[i].GetAvailableNetworkList((Wlan.WlanGetAvailableNetworkFlags)0);
                for (int j = 0; j < availableNetworkList.Length; j++)
                {
                    Wlan.WlanAvailableNetwork wlanAvailableNetwork = availableNetworkList[j];
                    if (wlanAvailableNetwork.dot11Ssid.SSID.SequenceEqual(ssid))
                    {
                        if (!wlanAvailableNetwork.securityEnabled)
                        {
                            return WifiConnectionSecurityMode.Unsecure;
                        }

                        return WifiConnectionSecurityMode.Secure;
                    }
                }
            }

            return WifiConnectionSecurityMode.Unknown;
        }
    }
}