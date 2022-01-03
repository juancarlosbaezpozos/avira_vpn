using System;
using System.Configuration;
using System.IO;
using Avira.VPN.Core.Win;
using Avira.VpnService.Properties;
using Serilog;

namespace Avira.VpnService
{
    public class SettingsMigrator
    {
        public static void MigrateSettings()
        {
            if (!ProductSettings.SettingsMigrated)
            {
                try
                {
                    ProductSettings.UpgradeSettings();
                    ProductSettings.MigrateCoreSettings();
                    MigrateServiceSettings();
                    DesktopShell.ShellExecute(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProductSettings.WebAppHostExe),
                        "/migrateSettings", AppDomain.CurrentDomain.BaseDirectory);
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Failed to migrate settings. Using the default ones.");
                }

                ProductSettings.SettingsMigrated = true;
                ProductSettings.DeleteUserSettingsFolder();
            }
        }

        public static void UpgradeSettings()
        {
            try
            {
                Log.Debug("Users settings file: " + ConfigurationManager
                    .OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath);
                if (Settings.Default.CallUpgrade)
                {
                    Settings.Default.Upgrade();
                    Settings.Default.CallUpgrade = false;
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to upgrade settings.");
            }
        }

        private static void MigrateServiceSettings()
        {
            UpgradeSettings();
            ProductSettings.LastUpdateCheck = Settings.Default.LastUpdateCheck;
            ProductSettings.LicenseData = Settings.Default.LicenseData;
            ProductSettings.AccessToken = Settings.Default.AccessToken;
            ProductSettings.UsedTraffic = (long)Settings.Default.UsedTraffic;
            ProductSettings.StartGuiAfterUpdate = Settings.Default.StartGuiAfterUpdate;
            ProductSettings.LastConnect = Settings.Default.LastConnect;
        }
    }
}