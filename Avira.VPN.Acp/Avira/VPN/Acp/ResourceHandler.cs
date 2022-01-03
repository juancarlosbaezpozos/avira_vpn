using System;
using System.Threading.Tasks;
using Avira.Common.Acp.AppClient;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Serilog;

namespace Avira.VPN.Acp
{
    public sealed class ResourceHandler : IDisposable
    {
        private class TokenRequest
        {
            public string grant_type { get; set; }

            public string client_id { get; set; }
        }

        private readonly QuickActionRepository quickActionRepository = new QuickActionRepository();

        private readonly VpnActionRepository vpnActionRepository = new VpnActionRepository();

        private readonly AppStatusRepository appStatusRepository = new AppStatusRepository();

        private readonly QuickActionHandler quickActionHandler;

        private AcpCommunicator acpCommunicator;

        public QuickActionRepository QuickActionRepository => quickActionRepository;

        public QuickActionHandler QuickActionHandler => quickActionHandler;

        public AppStatusRepository AppStatusRepository => appStatusRepository;

        public ResourceHandler()
        {
            acpCommunicator = new AcpCommunicator("vpn");
            DiContainer.SetInstance<IAcpCommunicator>(acpCommunicator);
            quickActionHandler = new QuickActionHandler(quickActionRepository, vpnActionRepository);
        }

        public async void Connect()
        {
            acpCommunicator.RegisterRepository(quickActionRepository, "/quickactions");
            acpCommunicator.RegisterRepository(vpnActionRepository, "/vpnactions");
            acpCommunicator.RegisterRepository(appStatusRepository, "/app-statuses");
            try
            {
                await acpCommunicator.ConnectToLauncher();
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Could not connect to Launcher using ACP.");
            }

            UpdateDownloadSource().CatchAll();
        }

        public async Task UpdateDownloadSource()
        {
            if (string.IsNullOrEmpty(ProductSettings.DownloadSource))
            {
                ProductSettings.DownloadSource = (await new Device().Get()).Item1?.download_source;
            }
        }

        public void Dispose()
        {
            acpCommunicator.Dispose();
        }
    }
}