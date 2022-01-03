using System;

namespace Avira.VPN.Core
{
    public class WsErrorEventArgs : EventArgs
    {
        public int Code { get; set; }

        public string Reason { get; set; }
    }
}