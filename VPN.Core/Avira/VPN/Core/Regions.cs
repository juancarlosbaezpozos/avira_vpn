using System;
using System.Threading.Tasks;
using Avira.Messaging;

namespace Avira.VPN.Core
{
    public class Regions
    {
        private IApiClient<RegionList> client;

        private IProductSettings productSettings;

        private IApplicationIds applicationIds;

        [Routing("Data")] public RegionList RegionList { get; private set; }

        [Routing("DataChanged")] public event EventHandler<EventArgs<RegionList>> RegionListChanged;

        public Regions(IApiClient<RegionList> client)
            : this(client, DiContainer.Resolve<IProductSettings>(), DiContainer.Resolve<IApplicationIds>())
        {
        }

        public Regions(IApiClient<RegionList> client, IProductSettings productSettings, IApplicationIds applicationIds)
        {
            this.client = client;
            RegionList = client.Data;
            this.productSettings = productSettings;
            this.client.DataChanged += OnDataChanged;
            this.applicationIds = applicationIds;
        }

        public async Task Refresh()
        {
            string protocolType = GetProtocolType();
            await client.Refresh("?device_id=" + applicationIds?.ClientId + "&type=" + protocolType + "&lang=" +
                                 (productSettings?.ProductLanguage ?? "en-US"));
        }

        private string GetProtocolType()
        {
            if (!DiContainer.Resolve<IDevice>().IsSandboxed())
            {
                return "openvpn";
            }

            return "ipsec";
        }

        private void OnDataChanged(object sender, EventArgs args)
        {
            RegionList = client.Data;
            this.RegionListChanged?.Invoke(this, new EventArgs<RegionList>(RegionList));
        }
    }
}