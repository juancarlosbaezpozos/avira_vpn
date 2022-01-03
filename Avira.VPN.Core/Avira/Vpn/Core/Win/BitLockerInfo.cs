using System;
using System.Management;
using Serilog;

namespace Avira.VPN.Core.Win
{
    public static class BitLockerInfo
    {
        public static bool IsActive()
        {
            int num = 0;
            try
            {
                foreach (ManagementBaseObject item in new ManagementObjectSearcher(
                             "root\\CIMV2\\Security\\MicrosoftVolumeEncryption",
                             "SELECT * FROM Win32_EncryptableVolume").Get())
                {
                    Log.Information(string.Format("Bitlocker protection status: {0}",
                        item.GetPropertyValue("ProtectionStatus")));
                    num |= int.Parse(item.GetPropertyValue("ProtectionStatus").ToString());
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception, "BitLocker");
            }

            Log.Information($"Bitlocker status is {num}");
            return num > 0;
        }
    }
}