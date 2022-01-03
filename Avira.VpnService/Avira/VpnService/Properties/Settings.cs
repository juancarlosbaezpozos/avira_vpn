using System;
using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Avira.VpnService.Properties
{
    [CompilerGenerated]
    [GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.6.0.0")]
    internal sealed class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

        public static Settings Default => defaultInstance;

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("https://api.phantom.avira-vpn.com/v1/")]
        public string VpnBackendUrl => (string)this["VpnBackendUrl"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("https://dispatch.avira-update.com")]
        public string UpdateServerUrl => (string)this["UpdateServerUrl"];

        [UserScopedSetting]
        [DebuggerNonUserCode]
        public DateTime LastUpdateCheck
        {
            get { return (DateTime)this["LastUpdateCheck"]; }
            set { this["LastUpdateCheck"] = value; }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("86400")]
        public int UpdateCheckIntervalInSeconds => (int)this["UpdateCheckIntervalInSeconds"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("30")]
        public int InitialUpdateCheckInSeconds => (int)this["InitialUpdateCheckInSeconds"];

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("True")]
        public bool CallUpgrade
        {
            get { return (bool)this["CallUpgrade"]; }
            set { this["CallUpgrade"] = value; }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("1")]
        public ushort OpenVpnVerbosity => (ushort)this["OpenVpnVerbosity"];

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        public string RefreshToken
        {
            get { return (string)this["RefreshToken"]; }
            set { this["RefreshToken"] = value; }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("10")]
        public int LicensePollIntervalInSeconds => (int)this["LicensePollIntervalInSeconds"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("300")]
        public int LicensePollMaxTimeInSeconds => (int)this["LicensePollMaxTimeInSeconds"];

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue(
            "{ \"expiration_date\": null, \"grace_period\": 60, \"traffic_limit\": 524288000, \"type\": \"unregistered\" }")]
        public string LicenseData
        {
            get { return (string)this["LicenseData"]; }
            set { this["LicenseData"] = value; }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("21600")]
        public long InstallNewPackageIntervalInSeconds => (long)this["InstallNewPackageIntervalInSeconds"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("1800")]
        public long UpdateTimersIntervalInSeconds => (long)this["UpdateTimersIntervalInSeconds"];

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        public string AccessToken
        {
            get { return (string)this["AccessToken"]; }
            set { this["AccessToken"] = value; }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool IsBeta => (bool)this["IsBeta"];

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("0")]
        public ulong UsedTraffic
        {
            get { return (ulong)this["UsedTraffic"]; }
            set { this["UsedTraffic"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool StartGuiAfterUpdate
        {
            get { return (bool)this["StartGuiAfterUpdate"]; }
            set { this["StartGuiAfterUpdate"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        public DateTime LastConnect
        {
            get { return (DateTime)this["LastConnect"]; }
            set { this["LastConnect"] = value; }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("https://api.my.avira.com/v2/")]
        public string OeApi => (string)this["OeApi"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("14.00:00:00")]
        public TimeSpan InactivityThreshold => (TimeSpan)this["InactivityThreshold"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("00:00:30")]
        public TimeSpan PopupTimeout => (TimeSpan)this["PopupTimeout"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("86400")]
        public long LicenseRefreshIntervalInSeconds => (long)this["LicenseRefreshIntervalInSeconds"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("echo.phantom.avira-vpn.com")]
        public string UdpEchoServerUrl => (string)this["UdpEchoServerUrl"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool DisablePipeAccessAuthorization => (bool)this["DisablePipeAccessAuthorization"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("True")]
        public bool EnableNetworkBlockerOnConnect => (bool)this["EnableNetworkBlockerOnConnect"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("unknown")]
        public string InstallationBundleId => (string)this["InstallationBundleId"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("Avira.VPN.OeConnector.dll")]
        public string UserManagementAsembly => (string)this["UserManagementAsembly"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("True")]
        public bool EducationMessageActive => (bool)this["EducationMessageActive"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("AviraVPNInstaller")]
        public string UpdatePackageName => (string)this["UpdatePackageName"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool Insider => (bool)this["Insider"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("https://iron-dot-cobalt-antenna-219709.appspot.com/v1/")]
        public string FrontingApi => (string)this["FrontingApi"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("http://185.123.227.250:61453")]
        public string VpnNodeApi => (string)this["VpnNodeApi"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("86400000")]
        public int MixpanelPingInterval => (int)this["MixpanelPingInterval"];

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool MalwareProtection
        {
            get { return (bool)this["MalwareProtection"]; }
            set { this["MalwareProtection"] = value; }
        }

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("86400")]
        public int OeStatusUpdateInterval => (int)this["OeStatusUpdateInterval"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("20000")]
        public int RemoteFeatureRequestInitialDelay => (int)this["RemoteFeatureRequestInitialDelay"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("86400000")]
        public int RemoteFeatureRequestDelay => (int)this["RemoteFeatureRequestDelay"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("True")]
        public bool kill_switch => (bool)this["kill_switch"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool kill_switch_switched_on => (bool)this["kill_switch_switched_on"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool udp_support => (bool)this["udp_support"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool udp_support_switched_on => (bool)this["udp_support_switched_on"];

        [ApplicationScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("{}")]
        public string disabled_notifications => (string)this["disabled_notifications"];
    }
}