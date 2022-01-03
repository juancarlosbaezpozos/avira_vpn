using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Reflection;
using Avira.Common.Core;
using Avira.VPN.Core.Win.Properties;
using Microsoft.Win32;
using Serilog;

namespace Avira.VPN.Core.Win
{
    public class ProductSettings
    {
        public class ProductVersionChangedEventArgs : EventArgs
        {
            public Version PreviousVersion { get; set; }

            public Version CurrentVersion { get; set; }

            public ProductVersionChangedEventArgs(Version previousVersion, Version currentVersion)
            {
                PreviousVersion = previousVersion;
                CurrentVersion = currentVersion;
            }
        }

        private static string mixpanelToken = string.Empty;

        private static IStorage sharedStorage;

        private static IStorage secureStorage;

        private static string ProductSettingsPath => "Avira\\VPN\\Defaults\\ProductSettings.json";

        private static string DefaultTrackingToken => DiContainer.Resolve<JsonStorage>().Get("MixpanelProdToken");

        private static string DevTrackingToken => DiContainer.Resolve<JsonStorage>().Get("MixpanelDevToken");

        private static string CITrackingToken => DiContainer.Resolve<JsonStorage>().Get("MixpanelCIToken");

        private static string DefaultProductFeedbackUrl => DiContainer.Resolve<JsonStorage>().Get("FeedbackUrl");

        public static string SentryUrl => DiContainer.Resolve<JsonStorage>().Get("SentryUrl");

        public static int ProductId
        {
            get
            {
                int result = 1370;
                int.TryParse(DiContainer.Resolve<JsonStorage>().Get("ProductId"), out result);
                return result;
            }
        }

        public static string ProductName => DiContainer.Resolve<JsonStorage>().Get("ApplicationName");

        public static string SharedSettingsFileName => "VpnSharedSettings.config";

        public static string SecureSettingsFileName => "VpnPrivateSettings.config";

        public static string NetworkBlockerFileName => "Avira.NetworkBlocker.exe";

        public static string WebAppHostExe => "Avira.WebAppHost.exe";

        public static string DiagnosticExe => "Avira.VPN.Diag.exe";

        public static string NotifierExe => "Avira.VPN.Notifier.exe";

        public static string ServiceName => DiContainer.Resolve<JsonStorage>().Get("ServiceName");

        public static string DeviceIdPrefix => DiContainer.Resolve<JsonStorage>().Get("DeviceIdPrefix");

        public static string VpnPipeName => DiContainer.Resolve<JsonStorage>().Get("ServicePipeName");

        public static string NotifierPipeName
        {
            get
            {
                if (!SpotlightIsActive() && !IsSpotlightVpnIntegrated)
                {
                    return VpnNotifierPipeName;
                }

                return "2CA0C806-3838-4E3B-95D6-03FB1853436B";
            }
        }

        public static string VpnNotifierPipeName => DiContainer.Resolve<JsonStorage>().Get("NotifierPipeName");

        public static string WebAppHostMutex => DiContainer.Resolve<JsonStorage>().Get("WebAppHostMutex");

        public static string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            DiContainer.Resolve<JsonStorage>().Get("AppDataDirName"));

        public static Version ProductVersionInfo => FileSystem.GetVersion("Avira.VpnService.exe");

        public static string ProductVersion => ProductVersionInfo.ToString();

        public static string ShortProductVersion => RemoveBuildNumber(ProductVersion);

        public static string UacProductName => DiContainer.Resolve<JsonStorage>().Get("UacProductName");

        public static string OpenVpnPath => DiContainer.Resolve<JsonStorage>().Get("OpenVpnPath");

        public static string OpenVpnConfigPath => "VpnClient.ovpn";

        public static string TapDeviceName => DiContainer.Resolve<JsonStorage>().Get("TapDeviceName");

        public static string TapDriverFileName => DiContainer.Resolve<JsonStorage>().Get("TapDriverFileName");

        public static string UpdateLocation =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                DiContainer.Resolve<JsonStorage>().Get("UpdateLocation"));

        public static string DiagnosticLocation => Path.Combine(SettingsFilePath, "Diagnostics");

        public static string DiagnosticHistoryLocation => Path.Combine(DiagnosticLocation, "History");

        public static string DownloadSource
        {
            get { return GenericAccessor.Get(SharedStorage, "DownloadSource", string.Empty); }
            set { GenericAccessor.Set(SharedStorage, "DownloadSource", value); }
        }

        public static string BundleId
        {
            get { return GenericAccessor.Get(SharedStorage, "BundleId", string.Empty); }
            set { GenericAccessor.Set(SharedStorage, "BundleId", value); }
        }

        public static bool IsInsider
        {
            get { return GenericAccessor.Get(SharedStorage, "Insider", SettingsProperty("Insider")); }
            set { GenericAccessor.Set(SharedStorage, "Insider", value); }
        }

        public static IStorage SharedStorage
        {
            get
            {
                if (sharedStorage == null)
                {
                    sharedStorage = new XmlStorage(SettingsFilePath, SharedSettingsFileName);
                }

                return sharedStorage;
            }
            set { sharedStorage = value; }
        }

        public static IStorage SecureStorage
        {
            get
            {
                IStorage result = secureStorage ??
                                  new XmlStorage(SettingsFilePath, SecureSettingsFileName, StorageType.AllUserAccess);
                secureStorage = result;
                return result;
            }
            set { secureStorage = value; }
        }

        public static string ProductLanguage
        {
            get
            {
                try
                {
                    string text = ((ClientSettingsSection)(ConfigurationManager
                            .OpenExeConfiguration(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, WebAppHostExe))
                            .GetSectionGroup("applicationSettings")?.Sections["Avira.WebAppHost.Properties.Settings"]))
                        ?.Settings.Get("LANG").Value.ValueXml.InnerText;
                    if (!string.IsNullOrEmpty(text))
                    {
                        return MakeLanguageRegionUpperCase(text);
                    }

                    Serilog.Log.Error(
                        "LANG section is missing from WebAppHost config file. Returning default language en-US.");
                }
                catch (Exception exception)
                {
                    Serilog.Log.Error(exception,
                        "Failed to retrieve the product languge. Returning default language en-US.");
                }

                return "en-US";
            }
        }

        public static string CommonAviraRegistryKeyPath { get; internal set; } =
            "HKEY_LOCAL_MACHINE\\Software\\Classes\\{80b8c23c-16e0-4cd8-bbc3-cecec9a78b79}";


        public static string MixPanelToken
        {
            get
            {
                if (!string.IsNullOrEmpty(mixpanelToken))
                {
                    return mixpanelToken;
                }

                string text = (string)Registry.GetValue(CommonAviraRegistryKeyPath, "trackingProject", string.Empty);
                if (string.IsNullOrEmpty(text))
                {
                    mixpanelToken = DefaultTrackingToken;
                }
                else if (text == "CI")
                {
                    mixpanelToken = CITrackingToken;
                }
                else
                {
                    mixpanelToken = DevTrackingToken;
                }

                return mixpanelToken;
            }
            set { mixpanelToken = value; }
        }

        public static bool ExtraLogging => 1 == (int)Registry.GetValue(CommonAviraRegistryKeyPath, "extraLogging", 0);

        public static bool ProductImprovementUserSetting
        {
            get { return GenericAccessor.Get(SharedStorage, "ProductImprovement", defaultValue: true); }
            set
            {
                if (ProductImprovementUserSetting != value)
                {
                    GenericAccessor.Set(SharedStorage, "ProductImprovement", value);
                }
            }
        }

        public static DateTime LastGuiOpened
        {
            get { return GenericAccessor.Get(SharedStorage, "LastGuiOpened", DateTime.MinValue); }
            set { GenericAccessor.Set(SharedStorage, "LastGuiOpened", value); }
        }

        public static DateTime LastOeReport
        {
            get
            {
                DateTime dateTime = GenericAccessor.Get(SharedStorage, "LastOeReport", new DateTime(2000, 1, 1));
                if (!(dateTime == DateTime.MinValue))
                {
                    return dateTime;
                }

                return DateTime.Now;
            }
            set { GenericAccessor.Set(SharedStorage, "LastOeReport", value); }
        }

        public static string UiSettings
        {
            get { return GenericAccessor.Get(SharedStorage, "UiSettings", "{}"); }
            set { GenericAccessor.Set(SharedStorage, "UiSettings", value); }
        }

        public static string DisplaySettings
        {
            get
            {
                return GenericAccessor.Get(SharedStorage, "display_settings",
                    "{\"OsSettings\":true,\"LightTheme\":false,\"DarkTheme\":false}");
            }
            set { GenericAccessor.Set(SharedStorage, "display_settings", value); }
        }

        public static string ThemeSelection
        {
            get { return GenericAccessor.Get(SharedStorage, "theme_selection", "{\"displayed\":false}"); }
            set { GenericAccessor.Set(SharedStorage, "theme_selection", value); }
        }

        public static Point WindowLocation
        {
            get { return GenericAccessor.Get(SharedStorage, "WindowLocation", new Point(-1, -1)); }
            set { GenericAccessor.Set(SharedStorage, "WindowLocation", value); }
        }

        public static bool UdpSupportUserSetting
        {
            get
            {
                return GenericAccessor.Get(SharedStorage, "UdpSupport",
                    DiContainer.Resolve<IFeatures>().IsSwitchedOn("udp_support"));
            }
            set { GenericAccessor.Set(SharedStorage, "UdpSupport", value); }
        }

        public static bool MalwareProtectionUserSetting
        {
            get
            {
                return GenericAccessor.Get(SharedStorage, "MalwareProtection",
                    DiContainer.Resolve<IFeatures>().IsSwitchedOn("malwareProtection"));
            }
            set { GenericAccessor.Set(SharedStorage, "MalwareProtection", value); }
        }

        public static string UserProfile
        {
            get { return GenericAccessor.Get(SecureStorage, "UserProfile", ""); }
            set { GenericAccessor.Set(SecureStorage, "UserProfile", value); }
        }

        public static string LastSavedRemoteFeatures
        {
            get { return GenericAccessor.Get(SharedStorage, "RemoteFeatures", "{\"features\":[]}"); }
            set { GenericAccessor.Set(SharedStorage, "RemoteFeatures", value); }
        }

        public static bool KillSwitchUserSetting
        {
            get
            {
                return GenericAccessor.Get(SharedStorage, "KillSwitch",
                    DiContainer.Resolve<IFeatures>().IsSwitchedOn("kill_switch"));
            }
            set { GenericAccessor.Set(SharedStorage, "KillSwitch", value); }
        }

        public static bool SettingsMigrated
        {
            get { return GenericAccessor.Get(SecureStorage, "SettingsMigrated", defaultValue: false); }
            set { GenericAccessor.Set(SecureStorage, "SettingsMigrated", value); }
        }

        public static DateTime LastUpdateCheck
        {
            get { return GenericAccessor.Get(SecureStorage, "LastUpdateCheck", new DateTime(2000, 1, 1)); }
            set { GenericAccessor.Set(SecureStorage, "LastUpdateCheck", value); }
        }

        public static string ClientId
        {
            get { return GenericAccessor.Get<string>(SharedStorage, "ClientId"); }
            set { GenericAccessor.Set<string>(SharedStorage, "ClientId", value); }
        }

        public static string InitialClientId
        {
            get { return GenericAccessor.Get<string>(SharedStorage, "InitialClientId"); }
            set { GenericAccessor.Set<string>(SharedStorage, "InitialClientId", value); }
        }

        public static int ClientIdChangeTotal
        {
            get { return GenericAccessor.Get<int>(SharedStorage, "ClientIdChangeTotal", 0); }
            set { GenericAccessor.Set<int>(SharedStorage, "ClientIdChangeTotal", value); }
        }

        public static DateTime TokenExpiration
        {
            get { return GenericAccessor.Get(SecureStorage, "TokenExpiration", new DateTime(2000, 1, 1)); }
            set { GenericAccessor.Set(SecureStorage, "TokenExpiration", value); }
        }

        public static string LicenseData
        {
            get
            {
                return GenericAccessor.Get(SecureStorage, "LicenseData",
                    "{ \"expiration_date\": null, \"grace_period\": 60, \"traffic_limit\": 524288000, \"type\": \"unregistered\" }");
            }
            set { GenericAccessor.Set(SecureStorage, "LicenseData", value); }
        }

        public static string AccessToken
        {
            get { return GenericAccessor.Get(SecureStorage, "AccessToken", string.Empty); }
            set { GenericAccessor.Set(SecureStorage, "AccessToken", value); }
        }

        public static string RefreshToken
        {
            get { return GenericAccessor.Get(SecureStorage, "RefreshToken", string.Empty); }
            set { GenericAccessor.Set(SecureStorage, "RefreshToken", value); }
        }

        public static long UsedTraffic
        {
            get { return GenericAccessor.Get(SecureStorage, "UsedTraffic", 0L); }
            set { GenericAccessor.Set(SecureStorage, "UsedTraffic", value); }
        }

        public static Version PreviousVersion
        {
            get
            {
                try
                {
                    return new Version(GenericAccessor.Get(SharedStorage, "PreviousVersion", string.Empty));
                }
                catch (Exception)
                {
                }

                return new Version();
            }
            set { GenericAccessor.Set(SharedStorage, "PreviousVersion", value.ToString()); }
        }

        public static bool StartGuiAfterUpdate
        {
            get { return GenericAccessor.Get(SecureStorage, "StartGuiAfterUpdate", defaultValue: false); }
            set { GenericAccessor.Set(SecureStorage, "StartGuiAfterUpdate", value); }
        }

        public static DateTime LastConnect
        {
            get { return GenericAccessor.Get(SecureStorage, "LastConnect", DateTime.MinValue); }
            set { GenericAccessor.Set(SecureStorage, "LastConnect", value); }
        }

        public static DateTime InstallDate
        {
            get { return GenericAccessor.Get(SecureStorage, "InstallDate", DateTime.MinValue); }
            set { GenericAccessor.Set(SecureStorage, "InstallDate", value); }
        }

        public static int GeneralFeedbackNotificationCount
        {
            get { return GenericAccessor.Get<int>(SecureStorage, "FeedbackNotificationCount", 0); }
            set { GenericAccessor.Set<int>(SecureStorage, "FeedbackNotificationCount", value); }
        }

        public static int ProductFeedbackNotificationCount
        {
            get { return GenericAccessor.Get<int>(SecureStorage, "ProductFeedbackNotificationCount", 0); }
            set { GenericAccessor.Set<int>(SecureStorage, "ProductFeedbackNotificationCount", value); }
        }

        public static bool LastProductFeedbackStatus
        {
            get { return GenericAccessor.Get(SecureStorage, "LastProductFeedbackStatus", defaultValue: false); }
            set { GenericAccessor.Set(SecureStorage, "LastProductFeedbackStatus", value); }
        }

        public static string LastProductFeedbackUrl
        {
            get { return GenericAccessor.Get(SecureStorage, "LastProductFeedbackUrl", DefaultProductFeedbackUrl); }
            set { GenericAccessor.Set(SecureStorage, "LastProductFeedbackUrl", value); }
        }

        public static DateTime LastFeedbackNotificationDate
        {
            get { return GenericAccessor.Get(SecureStorage, "LastFeedbackNotificationDate", DateTime.MinValue); }
            set { GenericAccessor.Set(SecureStorage, "LastFeedbackNotificationDate", value); }
        }

        public static TimeSpan GeneralFeedbackDelay
        {
            get { return GenericAccessor.Get(SecureStorage, "GeneralFeedbackDelay", new TimeSpan(5, 0, 0, 0)); }
            set { GenericAccessor.Set(SecureStorage, "GeneralFeedbackDelay", value); }
        }

        public static TimeSpan FeedbackNotificationMinPeriod
        {
            get
            {
                return GenericAccessor.Get(SecureStorage, "FeedbackNotificationMinPeriod", new TimeSpan(1, 0, 0, 0));
            }
            set { GenericAccessor.Set(SecureStorage, "FeedbackNotificationMinPeriod", value); }
        }

        public static bool AdBlockingUserSetting
        {
            get
            {
                return GenericAccessor.Get(SharedStorage, "AdBlocking",
                    DiContainer.Resolve<IFeatures>().IsSwitchedOn("adBlocking"));
            }
            set { GenericAccessor.Set(SharedStorage, "AdBlocking", value); }
        }

        public static bool FastFeedbackStillShowUserSetting
        {
            get { return GenericAccessor.Get(SharedStorage, "FastFeedback", defaultValue: true); }
            set { GenericAccessor.Set(SharedStorage, "FastFeedback", value); }
        }

        public static string ThemeColor
        {
            get { return GenericAccessor.Get(SharedStorage, "theme_color", string.Empty); }
            set { GenericAccessor.Set(SharedStorage, "theme_color", value); }
        }

        public static bool IsSpotlightVpnIntegrated
        {
            get { return GenericAccessor.Get(SharedStorage, "spotlight_vpnIntegrated", defaultValue: false); }
            set { GenericAccessor.Set(SharedStorage, "spotlight_vpnIntegrated", value); }
        }

        public static event EventHandler<ProductVersionChangedEventArgs> ProductVersionChanged;

        public static bool SpotlightIsActive()
        {
            if (IsWhitelabeledProduct())
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(GetSpotlightExperimentId());
        }

        public static bool IsWhitelabeledProduct()
        {
            try
            {
                return bool.Parse(DiContainer.Resolve<JsonStorage>().Get("WhiteLabeledProduct", "false"));
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Failed to check WhiteLabeledProduct property.");
                return false;
            }
        }

        public static string GetSpotlightExperimentId()
        {
            try
            {
                return Registry.GetValue(CommonAviraRegistryKeyPath, "ExperimentId", string.Empty).ToString();
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Failed to check Spotlight ExperimentId.");
                return string.Empty;
            }
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static string GetJsonDefaults()
        {
            var ruta = Path.Combine(AssemblyDirectory, "Defaults", "ProductSettings.json");
            //return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), ProductSettingsPath);
            return ruta;
        }

        public static bool IsInSpotlightControlGroup()
        {
            try
            {
                return Registry.GetValue(CommonAviraRegistryKeyPath, "ExperimentGroup", "").ToString() == "Control";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetUpdaterArguments(bool isBeta)
        {
            string result = "/S /LANG=\"" + ProductLanguage + "\"" + (isBeta ? " /beta" : string.Empty);
            string text = DiContainer.Resolve<JsonStorage>().Get("UpdaterArguments");
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            return result;
        }

        private static string MakeLanguageRegionUpperCase(string productLanguage)
        {
            string[] array = productLanguage.Split('-');
            return array[0] + "-" + array[1].ToUpper();
        }

        private static string FilterUnsupportedLanguages(string productLanguage)
        {
            if (string.Equals(productLanguage, "en-US") || string.Equals(productLanguage, "de-DE"))
            {
                return productLanguage;
            }

            return "en-US";
        }

        public static void UpgradeSettings()
        {
            try
            {
                Serilog.Log.Debug("Users settings file: " + ConfigurationManager
                    .OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath);
                if (Settings.Default.CallUpgrade)
                {
                    Settings.Default.Upgrade();
                    Settings.Default.CallUpgrade = false;
                }
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Failed to upgrade settings.");
            }
        }

        public static void DeleteUserSettingsFolder()
        {
            try
            {
                DirectoryInfo parent = Directory.GetParent(Directory.GetParent(ConfigurationManager
                    .OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath).FullName);
                if (parent.Exists)
                {
                    parent.Delete(recursive: true);
                }
            }
            catch (Exception exception)
            {
                Serilog.Log.Warning(exception, "Failed to removed user settings folder.");
            }
        }

        public static void MigrateCoreSettings()
        {
            ProductImprovementUserSetting = Settings.Default.Tracking;
        }

        public static void CheckForVersionUpdate()
        {
            Version previousVersion = PreviousVersion;
            Version version = new Version(ProductVersionInfo.Major, ProductVersionInfo.Minor, ProductVersionInfo.Build);
            if (!IsNewInstallation() && previousVersion < version)
            {
                ProductSettings.ProductVersionChanged?.Invoke(null,
                    new ProductVersionChangedEventArgs(previousVersion, version));
            }

            PreviousVersion = version;
        }

        public static bool IsNewInstallation()
        {
            if (!(InstallDate == DateTime.MinValue))
            {
                return DateTime.UtcNow - InstallDate < new TimeSpan(0, 0, 2, 0);
            }

            return true;
        }

        private static string RemoveBuildNumber(string versionNumber)
        {
            int num = versionNumber.LastIndexOf('.');
            if (num > 0)
            {
                return versionNumber.Substring(0, num);
            }

            return versionNumber;
        }

        private static bool SettingsProperty(string key, bool defaultValue = false)
        {
            ApplicationSettingsBase applicationSettingsBase = DiContainer.Resolve<ApplicationSettingsBase>();
            if (applicationSettingsBase == null)
            {
                return defaultValue;
            }

            try
            {
                return (bool)applicationSettingsBase[key];
            }
            catch (Exception exception)
            {
                Serilog.Log.Warning(exception, "Failed to get setting.");
                return defaultValue;
            }
        }
    }
}