using System;
using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Avira.VPN.Core.Win.Properties
{
    [CompilerGenerated]
    [GeneratedCode("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.7.0.0")]
    public sealed class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance = (Settings)SettingsBase.Synchronized(new Settings());

        public static Settings Default => defaultInstance;

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("2000-01-01")]
        public DateTime LastProfileUpdate
        {
            get { return (DateTime)this["LastProfileUpdate"]; }
            set { this["LastProfileUpdate"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("True")]
        public bool Tracking
        {
            get { return (bool)this["Tracking"]; }
            set { this["Tracking"] = value; }
        }

        [UserScopedSetting]
        [DebuggerNonUserCode]
        [DefaultSettingValue("True")]
        public bool CallUpgrade
        {
            get { return (bool)this["CallUpgrade"]; }
            set { this["CallUpgrade"] = value; }
        }
    }
}