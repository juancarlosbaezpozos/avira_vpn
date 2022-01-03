using System;
using System.Globalization;
using System.Resources;
using Avira.VPN.Acp;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Avira.Win.Messaging;
using Serilog;

namespace Avira.VPN.OeConnector
{
    [DiContainer.Export(typeof(ILauncherGuiController))]
    public class LauncherGuiController : ILauncherGuiController
    {
        private readonly ResourceHandler acpResourceHandler;

        private IVpnProvider vpn;

        private VpnQuickAction toggleQuickAction;

        public LauncherGuiController()
            : this(new ResourceHandler())
        {
        }

        public LauncherGuiController(ResourceHandler resourceHandler)
        {
            acpResourceHandler = resourceHandler;
        }

        private void Vpn_StatusChanged(object sender, EventArgs<Status> e)
        {
            try
            {
                UpdateLauncherGuiItems(e.Value.NewState);
                acpResourceHandler.QuickActionHandler.Update(toggleQuickAction);
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to update quick action status.");
            }
        }

        public void Initialize()
        {
            QuickActionHandler quickActionHandler = acpResourceHandler.QuickActionHandler;
            toggleQuickAction = new VpnQuickAction
            {
                Id = "vpnToggle",
                Tag = "vpnToggleStatus"
            };
            UpdateLauncherGuiItems(ConnectionState.Disconnected);
            quickActionHandler.Add(toggleQuickAction);
            acpResourceHandler.Connect();
        }

        public void Stop()
        {
            acpResourceHandler.AppStatusRepository.Update("stopped", string.Empty);
            acpResourceHandler.QuickActionRepository.DeleteAll(string.Empty);
        }

        public void SetVpnProvider(IVpnProvider vpnProvider)
        {
            vpn = vpnProvider;
            vpn.StatusChanged += Vpn_StatusChanged;
        }

        private void UpdateLauncherGuiItems(ConnectionState status)
        {
            AppStatusRepository appStatusRepository = acpResourceHandler.AppStatusRepository;
            switch (status)
            {
                case ConnectionState.Connected:
                    toggleQuickAction.Text = GetString("Disconnect");
                    toggleQuickAction.Enabled = true;
                    toggleQuickAction.Action = delegate { vpn.Disconnect(); };
                    appStatusRepository.Update(status.ToString(),
                        string.Format(GetString("StatusConnected"), vpn.ConnectedRegion.Name));
                    break;
                case ConnectionState.Connecting:
                    toggleQuickAction.Text = GetString("Connecting");
                    toggleQuickAction.Enabled = false;
                    toggleQuickAction.Action = null;
                    appStatusRepository.Update(status.ToString(), GetString("Connecting"));
                    break;
                case ConnectionState.Disconnected:
                    toggleQuickAction.Text = GetString("TurnOn");
                    toggleQuickAction.Enabled = true;
                    toggleQuickAction.Action = delegate
                    {
                        vpn.ConnectToLastSelectedRegion("Launcher");
                        vpn.StartClientApp("Launcher", startMinimized: true);
                    };
                    appStatusRepository.Update(status.ToString(), GetString("StatusDisconnected"));
                    break;
                case ConnectionState.Disconnecting:
                    toggleQuickAction.Text = GetString("Disconnecting");
                    toggleQuickAction.Enabled = false;
                    toggleQuickAction.Action = null;
                    appStatusRepository.Update(status.ToString(), GetString("Disconnecting"));
                    break;
            }
        }

        private string GetString(string key)
        {
            CultureInfo value = DiContainer.GetValue<CultureInfo>("Culture");
            return DiContainer.GetValue<ResourceManager>("ResourceManager")?.GetString(key, value);
        }
    }
}