namespace Avira.VPN.Core
{
    public enum VpnError
    {
        Unknown = -1,
        Success = 0,
        ErrorResolvingHostAddress = 1,
        NoNetworkAvailable = 2,
        ConnectionLost = 3,
        FatalError = 5,
        TapAdapterNotFound = 8,
        UdpFailureRetryingWithTcp = 11,
        IpsecBlocked = 12,
        ConnectionFailed = 13
    }
}