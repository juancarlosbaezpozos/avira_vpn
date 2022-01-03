using System;
using Avira.VPN.Shared.Core;

namespace Avira.VPN.Core
{
    public class StatusEventArgs : EventArgs
    {
        public VpnStatus Status { get; private set; }

        public StatusEventArgs(VpnStatus status)
        {
            Status = status;
        }
    }
}