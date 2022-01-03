using Avira.VPN.Core;
using Avira.VPN.Core.Win;

namespace Avira.VPN.OeConnector
{
    [DiContainer.Export(typeof(IDashboardAccessor))]
    public class DashboardAccessor : IDashboardAccessor
    {
        private const string JsonRegisterCommand =
            "{\"c\": \"SVC.EXTERNAL.OPENWEBRESOURCE\", \"p\": { \"serviceId\": \"\", \"section\": \"login\" }}";

        private const string JsonUpgraderCommand =
            "{\"c\": \"SVC.EXTERNAL.OPENWEBRESOURCE\", \"p\": { \"serviceId\": \"vpn\", \"section\": \"upgrade\" }}";

        private const string JsonOpenDashboardCommand =
            "{\"c\": \"SVC.EXTERNAL.OPENWEBRESOURCE\", \"p\": { \"serviceId\": \"vpn\", \"section\": \"dashboard\" }}";

        private readonly ICommunicationChannel communicationChannel;

        public DashboardAccessor()
            : this(new LauncherCommunicator())
        {
        }

        public DashboardAccessor(ICommunicationChannel communicationChannel)
        {
            this.communicationChannel = communicationChannel;
        }

        public void Register()
        {
            communicationChannel?.SendMessage(
                "{\"c\": \"SVC.EXTERNAL.OPENWEBRESOURCE\", \"p\": { \"serviceId\": \"\", \"section\": \"login\" }}");
        }

        public void Upgrade()
        {
            communicationChannel?.SendMessage(
                "{\"c\": \"SVC.EXTERNAL.OPENWEBRESOURCE\", \"p\": { \"serviceId\": \"vpn\", \"section\": \"upgrade\" }}");
        }

        public void OpenDashboard()
        {
            communicationChannel?.SendMessage(
                "{\"c\": \"SVC.EXTERNAL.OPENWEBRESOURCE\", \"p\": { \"serviceId\": \"vpn\", \"section\": \"dashboard\" }}");
        }
    }
}