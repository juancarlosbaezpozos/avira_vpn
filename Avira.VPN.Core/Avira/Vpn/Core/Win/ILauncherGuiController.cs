namespace Avira.VPN.Core.Win
{
    public interface ILauncherGuiController
    {
        void Initialize();

        void Stop();

        void SetVpnProvider(IVpnProvider vpnProvider);
    }
}