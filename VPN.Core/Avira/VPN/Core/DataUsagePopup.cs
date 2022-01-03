using System;
using System.Collections.Generic;
using Avira.Messaging;
using Avira.VPN.Shared.Core;
using Newtonsoft.Json;
using Serilog;

namespace Avira.VPN.Core
{
    public class DataUsagePopup
    {
        private const uint MinimumTimeBetweenNotifications = 1u;

        private ulong trafficWhenUserConnected;

        private ulong sessionTraffic;

        private int trafficPercentage;

        private Func<bool> isDataUsagePopupEnabled;

        [Routing("displayDataUsagePopup")] public event EventHandler DisplayDataUsagePopupDialog;

        public DataUsagePopup(Func<bool> isDataUsagePopupEnabled)
        {
            this.isDataUsagePopupEnabled = isDataUsagePopupEnabled;
            ITraffic traffic = DiContainer.Resolve<ITraffic>();
            if (traffic != null)
            {
                trafficWhenUserConnected = traffic.TrafficData.UsedTraffic;
            }

            DiContainer.Resolve<IVpnConnector>().StatusChanged += VpnStatusChangedHandler;
        }

        [Routing("getProDataUsage")]
        public void GetProDataUsage()
        {
            Tracker.TrackEvent(Tracker.Events.DataUsageClicked, new Dictionary<string, string>
            {
                {
                    "Session Traffic (bytes)",
                    sessionTraffic.ToString()
                },
                {
                    "Used Percentage",
                    trafficPercentage.ToString()
                }
            });
        }

        [Routing("notNowDataUsage")]
        public void NotNowDataUsage()
        {
            Tracker.TrackEvent(Tracker.Events.DataUsageDismissed, new Dictionary<string, string>
            {
                {
                    "Session Traffic (bytes)",
                    sessionTraffic.ToString()
                }
            });
        }

        private VpnStatus GetVpnStatus(EventArgs e)
        {
            IVpnConnector vpnConnector = DiContainer.Resolve<IVpnConnector>();
            if (vpnConnector == null)
            {
                throw new Exception("VpnConnector instance not available.");
            }

            if (e is StatusEventArgs)
            {
                return (e as StatusEventArgs).Status;
            }

            return vpnConnector.Status;
        }

        public void VpnStatusChangedHandler(object sender, EventArgs e)
        {
            try
            {
                if (GetVpnStatus(e) == VpnStatus.Connected)
                {
                    trafficWhenUserConnected = DiContainer.Resolve<ITraffic>().TrafficData.UsedTraffic;
                }
            }
            catch (Exception)
            {
            }
        }

        private bool ShouldShowDialog()
        {
            return false;
        }

        public void ShowDialogIfNeeded()
        {
            if (ShouldShowDialog())
            {
                TrafficData trafficData = DiContainer.Resolve<ITraffic>().TrafficData;
                sessionTraffic = trafficData.UsedTraffic - trafficWhenUserConnected;
                if (trafficData.TrafficLimit != 0L)
                {
                    trafficPercentage =
                        (int)Math.Round((double)trafficData.UsedTraffic * 100.0 / (double)trafficData.TrafficLimit);
                }

                Log.Information("Showing DataUsage dialog...");
                this.DisplayDataUsagePopupDialog?.Invoke(this, EventArgs.Empty);
                Tracker.TrackEvent(Tracker.Events.DataUsageShown, new Dictionary<string, string>
                {
                    {
                        "Session Traffic (bytes)",
                        sessionTraffic.ToString()
                    }
                });
                DiContainer.Resolve<ISettings>()
                    .Set("last_data_usage_shown", JsonConvert.SerializeObject(DateTime.Now));
            }
        }
    }
}