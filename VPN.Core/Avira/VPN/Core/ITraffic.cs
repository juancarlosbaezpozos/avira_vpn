namespace Avira.VPN.Core
{
    public interface ITraffic
    {
        TrafficData TrafficData { get; }

        void Update(ulong additionalBytes);
    }
}