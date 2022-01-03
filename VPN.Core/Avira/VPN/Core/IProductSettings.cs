using System;

namespace Avira.VPN.Core
{
    public interface IProductSettings
    {
        string ProductLanguage { get; }

        string ProductVersion { get; }

        string DownloadSource { get; }

        string BundleId { get; }

        string OsType { get; }

        string ApplicationData { get; }

        string ApplicationName { get; }

        string InstallationPath { get; }

        bool IsSpotlightVpnIntegrated { get; }

        Version GetOsVersion();

        string GetApplicationDataFile(string fileName);

        bool IsGuiRunning();

        bool IsSpotlightActive();

        bool IsInSpotlightControlGroup();
    }
}