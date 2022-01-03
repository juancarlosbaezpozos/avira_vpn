namespace Avira.VpnService
{
    public interface IOpenVpnClient
    {
        void EnableStateNotification();

        void Release();

        void Auth(string user, string password);

        void EnableByteCountNotification(int period);

        void EnableLogging();

        void SetVerbosityLevel(ushort level);
    }
}