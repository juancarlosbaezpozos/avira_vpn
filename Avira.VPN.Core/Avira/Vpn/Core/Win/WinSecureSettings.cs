namespace Avira.VPN.Core.Win
{
    [DiContainer.Export(typeof(ISecureSettings))]
    public class WinSecureSettings : ISecureSettings
    {
        public string Get(string key, string defaultValue = "")
        {
            return GenericAccessor.Get(ProductSettings.SecureStorage, key, defaultValue);
        }

        public void Set(string key, string value)
        {
            GenericAccessor.Set(ProductSettings.SecureStorage, key, value);
        }
    }
}