using System;
using System.Collections.Generic;
using Avira.Common.Core.Networking;

namespace Avira.VpnService
{
    public class KnownWifis : List<KnownWifis.WiFiData>
    {
        public class WiFiData
        {
            public string Id { get; set; }

            public string Ssid { get; set; }

            public TrustMode TrustMode { get; set; }

            public DateTime LastConnectionTime { get; set; }

            public WifiConnectionSecurityMode SecurityMode { get; set; }
        }

        public void Trust(string id)
        {
            Find((WiFiData d) => d.Id == id).TrustMode = TrustMode.Trusted;
        }

        public void Untrust(string id)
        {
            Find((WiFiData d) => d.Id == id).TrustMode = TrustMode.Untrusted;
        }

        public void UpdateConnectionTime(string id, DateTime date)
        {
            Find((WiFiData d) => d.Id == id).LastConnectionTime = date;
        }

        public int GetConnectedWifis(int daysAgo)
        {
            return FindAll((WiFiData d) => d.LastConnectionTime.Date >= DateTime.Today.AddDays(-daysAgo)).Count;
        }
    }
}