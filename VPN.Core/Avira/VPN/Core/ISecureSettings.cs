namespace Avira.VPN.Core
{
    public interface ISecureSettings
    {
        string Get(string key, string defaultValue = "");

        void Set(string key, string value);
    }
}