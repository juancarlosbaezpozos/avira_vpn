using Avira.Win.Messaging;

namespace Avira.VPN.Core.Win
{
    public class ServiceLocator : IServiceLocator
    {
        public string GetServiceUrl(string serviceName)
        {
            switch (serviceName)
            {
                case "VPN":
                case "VpnService":
                    return "pipe://" + ProductSettings.VpnPipeName + "/VPN";
                case "Notifier":
                    return "pipe://" + ProductSettings.NotifierPipeName;
                default:
                    return string.Empty;
            }
        }
    }
}