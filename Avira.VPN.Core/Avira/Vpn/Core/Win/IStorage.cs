namespace Avira.VPN.Core.Win
{
    public interface IStorage
    {
        void Set(string key, string value);

        string Get(string key);
    }
}