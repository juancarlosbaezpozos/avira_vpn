namespace Avira.VPN.Core
{
    public interface ISettings
    {
        string Get(string key, string defaultValue = "");

        void Set(string key, string value);
    }
}