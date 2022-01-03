namespace Avira.VPN.Core.Win
{
    public class ApplicationIds : IApplicationIds
    {
        public string DeviceId => GeneratedDeviceInfo.GetDeviceId();

        public string TrackingId => GeneratedDeviceInfo.GetTrackingId();

        public string ClientId => GeneratedDeviceInfo.GetClientId();
    }
}