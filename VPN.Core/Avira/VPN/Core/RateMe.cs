using System;
using Avira.Messaging;
using Avira.VPN.Shared.Core;
using Newtonsoft.Json;
using Serilog;

namespace Avira.VPN.Core
{
    public class RateMe
    {
        private const ulong minimumTrafficConsumption = 52428800uL;

        private const string TimesRateMeDialogWasShown = "times_rate_me_dialog_was_showed";

        private const uint MinimumTimeBetweenNotifications = 1u;

        private ulong trafficWhenUserConnected;

        [Routing("displayRateMe")] public event EventHandler DisplayRateMeDialog;

        public RateMe()
        {
            ITraffic traffic = DiContainer.Resolve<ITraffic>();
            if (traffic != null)
            {
                trafficWhenUserConnected = traffic.TrafficData.UsedTraffic;
            }

            IDevice device = DiContainer.Resolve<IDevice>();
            if (device != null && device.IsSandboxed())
            {
                DiContainer.Resolve<IVpnConnector>().StatusChanged += VpnStatusChangedHandler;
            }
        }

        internal void VpnStatusChangedHandler(object sender, EventArgs e)
        {
            if (WasDialogShowed())
            {
                return;
            }

            IVpnConnector vpnConnector = DiContainer.Resolve<IVpnConnector>();
            if (vpnConnector != null)
            {
                switch ((!(e is StatusEventArgs)) ? vpnConnector.Status : (e as StatusEventArgs).Status)
                {
                    case VpnStatus.Connected:
                        trafficWhenUserConnected = DiContainer.Resolve<ITraffic>().TrafficData.UsedTraffic;
                        break;
                    case VpnStatus.Disconnected:
                        ShowDialogIfNeeded();
                        break;
                }
            }
        }

        private bool WasDialogShowed()
        {
            DateTime dateTime = JsonConvert.DeserializeObject<DateTime>(DiContainer.Resolve<ISettings>()
                .Get("last_feedback_shown", JsonConvert.SerializeObject(new DateTime(2000, 1, 1))));
            if (DateTime.Now < dateTime.AddDays(1.0))
            {
                return true;
            }

            return int.Parse(DiContainer.Resolve<ISettings>().Get("times_rate_me_dialog_was_showed", "0")) > 0;
        }

        private void ShowDialogIfNeeded()
        {
            ulong usedTraffic = DiContainer.Resolve<ITraffic>().TrafficData.UsedTraffic;
            if (trafficWhenUserConnected + 52428800 < usedTraffic)
            {
                Log.Information("Showing rate me dialog...");
                this.DisplayRateMeDialog?.Invoke(this, EventArgs.Empty);
                DiContainer.Resolve<ISettings>().Set("times_rate_me_dialog_was_showed", "1");
                DiContainer.Resolve<ISettings>().Set("last_feedback_shown", JsonConvert.SerializeObject(DateTime.Now));
                Tracker.TrackEvent(Tracker.Events.RateMeShown);
            }
        }
    }
}