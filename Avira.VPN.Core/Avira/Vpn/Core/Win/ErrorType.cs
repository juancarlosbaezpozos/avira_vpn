namespace Avira.VPN.Core.Win
{
    public enum ErrorType
    {
        NoError = 0,
        DnsError = 1,
        NetworkError = 2,
        ServerError = 3,
        GeneralError = 104,
        Fatal = 5,
        ConnectedError = 106,
        TrafficLimitReached = 7,
        TapAdapterError = 8,
        TapAdapterRestartRequired = 9,
        PingReset = 109,
        DecryptionError = 110,
        UdpErrorReconnecting = 11,
        TlsError = 112,
        AuthError = 113
    }
}