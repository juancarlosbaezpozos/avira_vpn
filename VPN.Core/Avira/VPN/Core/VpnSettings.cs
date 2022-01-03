namespace Avira.VPN.Core
{
    public class VpnSettings
    {
        protected static ISettings Settings => DiContainer.Resolve<ISettings>();

        public static string VpnApiUrl => Settings?.Get("vpn_api", "https://api.phantom.avira-vpn.com/v1/");

        public static string VpnNodeApi => Settings?.Get("vpn_node_api", "http://185.123.227.250:61453");

        public static string OeApiUrl => Settings?.Get("oe_api", "https://api.my.avira.com/");

        public static string OeWebsocketUrl => Settings?.Get("oe_socket", "wss://ssld.oes.avira.com");

        public static int ProductId => int.Parse(Settings?.Get("product_id", "1371"));

        public static bool UseAcceptance => bool.Parse(Settings?.Get("use_acceptance", "true"));
    }
}