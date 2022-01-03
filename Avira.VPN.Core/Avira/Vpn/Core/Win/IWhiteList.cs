namespace Avira.VPN.Core.Win
{
    public interface IWhiteList
    {
        bool IsWhiteListed(string item);
    }
}