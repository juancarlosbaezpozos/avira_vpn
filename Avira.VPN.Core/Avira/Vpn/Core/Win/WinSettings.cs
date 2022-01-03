using System;
using System.Configuration;

namespace Avira.VPN.Core.Win
{
    [DiContainer.Export(typeof(ISettings))]
    public class WinSettings : ISettings
    {
        private ApplicationSettingsBase settings;

        public WinSettings(ApplicationSettingsBase settings)
        {
            this.settings = settings;
        }

        public string Get(string key, string defaultValue = "")
        {
            string defaultValue2 = defaultValue;
            if (settings != null && SettingsContainsKey(key))
            {
                try
                {
                    defaultValue2 = settings[key].ToString();
                }
                catch (Exception)
                {
                }
            }

            return GenericAccessor.Get(ProductSettings.SharedStorage, key, defaultValue2);
        }

        private bool SettingsContainsKey(string key)
        {
            foreach (SettingsProperty item in settings?.Properties)
            {
                if (item.Name == key)
                {
                    return true;
                }
            }

            return false;
        }

        public void Set(string key, string value)
        {
            GenericAccessor.Set(ProductSettings.SharedStorage, key, value);
        }
    }
}