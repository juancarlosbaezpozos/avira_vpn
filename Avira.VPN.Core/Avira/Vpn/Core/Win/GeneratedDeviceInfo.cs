using System;
using Microsoft.Win32;
using Serilog;

namespace Avira.VPN.Core.Win
{
    public static class GeneratedDeviceInfo
    {
        private const string MachineIdValueName = "machine";

        private const string TelemetryIdValueName = "telemetry";

        private const string ClientIdValueName = "vpnclient";

        private static string deviceId;

        private static string trackingId;

        private static string clientId;

        internal static Func<string, string, object, object> GetRegistryValue;

        internal static Action<string, string, object> SetRegistryValue;

        internal static bool IgnoreCachedValue;

        static GeneratedDeviceInfo()
        {
            GetRegistryValue = Registry.GetValue;
            SetRegistryValue = Registry.SetValue;
            IgnoreCachedValue = false;
            DiContainer.SetGetter("DeviceId", GetDeviceId);
            DiContainer.SetGetter("TelemetryId", GetTrackingId);
        }

        public static string GetDeviceId()
        {
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = GetUniqueId("machine");
            }

            return deviceId;
        }

        public static string GetTrackingId()
        {
            if (string.IsNullOrEmpty(trackingId))
            {
                trackingId = GetUniqueId("telemetry");
            }

            return trackingId;
        }

        public static string GetClientId()
        {
            if (string.IsNullOrEmpty(clientId) || IgnoreCachedValue)
            {
                clientId = ProductSettings.DeviceIdPrefix + "-" + GetUniqueId("vpnclient");
            }

            if (string.IsNullOrEmpty(ProductSettings.ClientId))
            {
                ProductSettings.ClientId = clientId;
            }

            if (string.IsNullOrEmpty(ProductSettings.InitialClientId))
            {
                ProductSettings.InitialClientId = clientId;
            }

            if (clientId != ProductSettings.ClientId)
            {
                ProductSettings.ClientIdChangeTotal++;
                ProductSettings.ClientId = clientId;
            }

            return clientId;
        }

        private static string GetUniqueId(string keyName)
        {
            string text = GetUniqueIdFromRegistry(keyName);
            if (string.IsNullOrWhiteSpace(text))
            {
                text = GenerateId();
                SetUniqueIdToRegistry(text, keyName);
            }

            return text;
        }

        private static string GenerateId()
        {
            return (Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N").Substring(0, 8)).ToLower();
        }

        private static string GetUniqueIdFromRegistry(string valueName)
        {
            string text = null;
            try
            {
                return (string)GetRegistryValue(ProductSettings.CommonAviraRegistryKeyPath, valueName, string.Empty);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to retrieve Unique Id from the registry.");
                return GenericAccessor.Get<string>(ProductSettings.SharedStorage, "ApplicationIdsCache-" + valueName);
            }
        }

        private static void SetUniqueIdToRegistry(string deviceId, string valueName)
        {
            try
            {
                SetRegistryValue(ProductSettings.CommonAviraRegistryKeyPath, valueName, deviceId);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to set the Unique Id in the registry.");
                GenericAccessor.Set<string>(ProductSettings.SharedStorage, "ApplicationIdsCache-" + valueName,
                    deviceId);
            }
        }

        internal static void Clear()
        {
            deviceId = null;
            trackingId = null;
            clientId = null;
        }
    }
}