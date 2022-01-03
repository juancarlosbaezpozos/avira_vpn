namespace Avira.VPN.Core.Win
{
    public interface ICommunicationChannel
    {
        void SendMessage(string message);
    }
}