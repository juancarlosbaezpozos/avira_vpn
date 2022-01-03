using System;
using System.IO;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;

namespace Avira.VpnService
{
    public sealed class ProductSettingsBridge : IProductSettings
    {
        public string ProductLanguage => ProductSettings.ProductLanguage;

        public string ProductVersion => ProductSettings.ProductVersion.ToString();

        public string DownloadSource => ProductSettings.DownloadSource;

        public string BundleId => ProductSettings.BundleId;

        public string OsType => "Windows";

        public string ApplicationData => string.Empty;

        public string ApplicationName => "Avira Phantom VPN";

        public string InstallationPath => AppDomain.CurrentDomain.BaseDirectory;

        public bool IsSpotlightVpnIntegrated => ProductSettings.IsSpotlightVpnIntegrated;

        public string GetApplicationDataFile(string fileName)
        {
            return Path.Combine(ProductSettings.SettingsFilePath, fileName);
        }

        public bool IsGuiRunning()
        {
            throw new NotImplementedException();
        }

        public Version GetOsVersion()
        {
            return Environment.OSVersion.Version;
        }

        public bool IsSpotlightActive()
        {
            return ProductSettings.SpotlightIsActive();
        }

        public bool IsInSpotlightControlGroup()
        {
            return ProductSettings.IsInSpotlightControlGroup();
        }
    }
}