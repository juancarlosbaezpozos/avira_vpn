using Avira.VPN.Core.Win;

namespace Avira.VpnService
{
    public class CommandLineParametersBuilder
    {
        public readonly string DefaultValue =
            "--auth-nocache --reneg-sec 0 --mute-replay-warnings --management-hold --block-outside-dns --management-query-passwords --service AviraVPNOpenVpnQuitEvent --redirect-gateway def1 --register-dns --resolv-retry 15 --connect-retry-max 3 --tls-exit --remap-usr1 SIGTERM";

        private string commandLineParameters;

        public RemoteConnectionSettings OpenVpnServerSettings { get; set; }

        public RemoteConnectionSettings ManagementConsoleSettings { get; set; }

        public bool IsIpV6 { get; set; }

        public string ConfigFilePath { get; set; }

        public string AdapterName { get; set; }

        public string Create()
        {
            commandLineParameters = DefaultValue;
            if (OpenVpnServerSettings != null)
            {
                AddToArgumentsList("remote", OpenVpnServerSettings.ToString());
                AddToArgumentsList("hand-window", OpenVpnServerSettings.TlsHadshakeWindow.ToString());
            }

            AddToArgumentsList("config", ConfigFilePath);
            if (ManagementConsoleSettings != null)
            {
                AddToArgumentsList("management", ManagementConsoleSettings.ToString());
            }

            if (IsIpV6)
            {
                AddToArgumentsList("tun-ipv6");
            }
            else
            {
                AddToArgumentsList("pull-filter", "ignore \"ifconfig-ipv6\"");
                AddToArgumentsList("pull-filter", "ignore \"route-ipv6\"");
            }

            if (!string.IsNullOrEmpty(AdapterName))
            {
                AddToArgumentsList("dev-node", "\"" + AdapterName + "\"");
            }

            return commandLineParameters;
        }

        private void AddToArgumentsList(string name, string value = "")
        {
            if (value != null)
            {
                string text = " --" + name;
                if (!string.IsNullOrEmpty(value))
                {
                    text = text + " " + value;
                }

                commandLineParameters += text;
            }
        }
    }
}