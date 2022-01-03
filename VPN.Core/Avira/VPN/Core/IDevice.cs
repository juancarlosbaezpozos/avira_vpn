namespace Avira.VPN.Core
{
    public interface IDevice
    {
        string OperatingSystemVersion { get; }

        string MachineName { get; }

        string OperatingSystem { get; }

        string UserAgentString { get; }

        string OperatingSystemLanguage { get; }

        string OperatingSystemArchitecture { get; }

        bool IsSandboxed();
    }
}