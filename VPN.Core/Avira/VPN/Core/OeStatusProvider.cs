using System;
using Avira.VPN.Shared.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public class OeStatusProvider : IOeStatusProvider
    {
        private DateTime lastConnect;

        private DateTime lastGuiOpened = DateTime.Now;

        private IVpnConnector vpnConnector;

        private readonly TimeSpan oeInusePeriod;

        public JObject HeartbeatCustomData => new JObject
        {
            ["version"] = (JToken)(DiContainer.Resolve<IProductSettings>()?.ProductVersion),
            ["state"] = (JToken)(IsAppInUse() ? "inuse" : "active")
        };

        public JObject EventsCustomData
        {
            get
            {
                return new JObject
                {
                    ["version"] = (JToken)(DiContainer.Resolve<IProductSettings>()?.ProductVersion),
                    ["license_type"] = "paid"
                };
            }
        }

        public OeStatusProvider(IVpnConnector vpnConnector, TimeSpan oeInusePeriod)
        {
            OeStatusProvider oeStatusProvider = this;
            this.vpnConnector = vpnConnector;
            this.oeInusePeriod = oeInusePeriod;
            vpnConnector.StatusChanged += delegate
            {
                if (vpnConnector.Status == VpnStatus.Connected)
                {
                    oeStatusProvider.lastConnect = DateTime.Now;
                }
            };
        }

        internal bool IsAppInUse()
        {
            if (!WasAppOpened())
            {
                return WasConnectFeatureUsed();
            }

            return true;
        }

        public void UpdateLastGuiOpened(DateTime guiOpened)
        {
            lastGuiOpened = guiOpened;
        }

        public bool WasConnectFeatureUsed()
        {
            ISettings settings = DiContainer.Resolve<ISettings>();
            bool flag = vpnConnector.Status == VpnStatus.Connected;
            DateTime value = JsonConvert.DeserializeObject<DateTime>(settings.Get(SettingsKeys.LastConnectName,
                JsonConvert.SerializeObject(new DateTime(2000, 1, 1))));
            if (!flag)
            {
                return DateTime.Now.Subtract(value) <= oeInusePeriod;
            }

            return true;
        }

        public bool WasAppOpened()
        {
            return DateTime.Now.Subtract(lastGuiOpened) <= oeInusePeriod;
        }
    }
}