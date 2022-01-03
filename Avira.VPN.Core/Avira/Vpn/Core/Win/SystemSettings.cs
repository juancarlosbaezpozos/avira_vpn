using System;
using Microsoft.Win32;
using Serilog;

namespace Avira.VPN.Core.Win
{
    public class SystemSettings
    {
        private const string AppsThemeKeyPath =
            "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

        private const string AppsThemeValueName = "AppsUseLightTheme";

        internal static Func<string, string, object, object> GetRegistryValue = Registry.GetValue;

        internal static Action<string, string, object> SetRegistryValue = Registry.SetValue;

        public string ThemeAppsUse { get; private set; }

        public event EventHandler<SystemSettingsData> SystemSettingsChanged;

        public SystemSettings()
        {
            ThemeAppsUse = GetThemeAppsUse();
            subscribeForThemeChanges();
        }

        ~SystemSettings()
        {
            unsubscribeFromThemeChanges();
        }

        private string GetThemeAppsUse()
        {
            string text = "LightTheme";
            object obj =
                GetRegistryValue("HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
                    "AppsUseLightTheme", 1);
            if (obj != null)
            {
                text = (((int)obj == 0) ? "DarkTheme" : "LightTheme");
                Log.Debug("Theme specified in the system settings : " + text);
            }

            return text;
        }

        internal void SystemEvents_PreferencesChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                string themeAppsUse = GetThemeAppsUse();
                if (ThemeAppsUse != themeAppsUse)
                {
                    ThemeAppsUse = themeAppsUse;
                    this.SystemSettingsChanged?.Invoke(this, new SystemSettingsData
                    {
                        Theme = ThemeAppsUse
                    });
                }
            }
        }

        private void subscribeForThemeChanges()
        {
            SystemEvents.UserPreferenceChanged += SystemEvents_PreferencesChanged;
        }

        private void unsubscribeFromThemeChanges()
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_PreferencesChanged;
        }
    }
}