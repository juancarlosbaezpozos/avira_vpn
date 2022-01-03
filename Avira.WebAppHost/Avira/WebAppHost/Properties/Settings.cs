using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace Avira.WebAppHost.Properties
{
    [CompilerGenerated]
    [GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

        public static Settings Default => defaultInstance;

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("-1, -1")]
        public Point WindowLocation
        {
            get { return (Point)this["WindowLocation"]; }
            set { this["WindowLocation"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("")]
        public string UiSettings
        {
            get { return (string)this["UiSettings"]; }
            set { this["UiSettings"] = value; }
        }

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
        [DefaultSettingValue("en-US")]
        public string LANG => (string)this["LANG"];

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("True")]
        public bool SendDiagnosticData
        {
            get { return (bool)this["SendDiagnosticData"]; }
            set { this["SendDiagnosticData"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("False")]
        public bool AutoSecureUntrustedWifi
        {
            get { return (bool)this["AutoSecureUntrustedWifi"]; }
            set { this["AutoSecureUntrustedWifi"] = value; }
        }
    }
}