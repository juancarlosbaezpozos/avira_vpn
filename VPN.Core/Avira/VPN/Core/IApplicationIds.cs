namespace Avira.VPN.Core
{
    public interface IApplicationIds
    {
        string DeviceId { get; }

        string TrackingId { get; }

        string ClientId { get; }
    }
}