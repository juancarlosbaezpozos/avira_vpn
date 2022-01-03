using System;

namespace Avira.VpnService
{
    public class ManagementMessage : EventArgs
    {
        public string Data { get; set; }
    }
}