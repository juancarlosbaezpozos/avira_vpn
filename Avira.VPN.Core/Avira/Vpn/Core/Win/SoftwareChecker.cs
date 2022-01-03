using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.AccessControl;
using Microsoft.Win32;

namespace Avira.VPN.Core.Win
{
    public class SoftwareChecker
    {
        public static bool IsInstalled(List<string> appsList)
        {
            return Catch.All(delegate
            {
                List<string> listOfApps = appsList.Select((string x) => x.ToLowerInvariant()).ToList();
                if (IsInstalledForCurrentUser(listOfApps, RegistryView.Registry32) ||
                    IsInstalledForAllUsers(listOfApps, RegistryView.Registry32))
                {
                    return true;
                }

                return Environment.Is64BitOperatingSystem &&
                       (IsInstalledForCurrentUser(listOfApps, RegistryView.Registry64) ||
                        IsInstalledForAllUsers(listOfApps, RegistryView.Registry64));
            }, defaultValue: false);
        }

        public static bool IsAntivirusInstalled()
        {
            string scope = "root\\SecurityCenter2";
            try
            {
                foreach (ManagementBaseObject item in new ManagementObjectSearcher(scope,
                             "SELECT * FROM AntivirusProduct").Get())
                {
                    if (!item["displayName"].ToString().Contains("Windows Defender"))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        protected static bool IsInstalledForCurrentUser(List<string> listOfApps, RegistryView registryView)
        {
            string currentUserSid = LoggedOnUser.GetCurrentUserSid();
            if (!string.IsNullOrEmpty(currentUserSid))
            {
                using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.Users, registryView);
                using RegistryKey baseKey = registryKey.OpenSubKey(currentUserSid);
                if (IsInstalled(baseKey, listOfApps))
                {
                    return true;
                }
            }

            return false;
        }

        protected static bool IsInstalledForAllUsers(List<string> listOfApps, RegistryView registryView)
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView);
            return IsInstalled(baseKey, listOfApps);
        }

        protected static bool IsInstalled(RegistryKey baseKey, List<string> listOfApps)
        {
            using (RegistryKey registryKey =
                   baseKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                       RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ExecuteKey))
            {
                string[] subKeyNames = registryKey.GetSubKeyNames();
                string displayName;
                foreach (string name in subKeyNames)
                {
                    using RegistryKey registryKey2 = registryKey.OpenSubKey(name);
                    displayName = registryKey2.GetValue("DisplayName") as string;
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        displayName = displayName.ToLowerInvariant();
                        if (listOfApps.Any((string app) => displayName.Contains(app)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}