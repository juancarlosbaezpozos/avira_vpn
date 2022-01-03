using System;
using Microsoft.Win32;
using Serilog;

namespace Avira.VpnService
{
    public class InterfaceConfig
    {
        private readonly string configPath;

        public InterfaceConfig(string configPath)
        {
            this.configPath = configPath;
        }

        public bool IsIpV6()
        {
            try
            {
                return ((int)Registry.GetValue(configPath, "DisabledComponents", 0) & 0x10) != 16;
            }
            catch (UnauthorizedAccessException exception)
            {
                Log.Error(exception, "Failed to get value " + configPath + " from registry.");
                return false;
            }
        }
    }
}