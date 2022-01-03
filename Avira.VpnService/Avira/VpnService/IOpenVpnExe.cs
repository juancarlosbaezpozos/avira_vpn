using System;
using Avira.Win.Messaging;

namespace Avira.VpnService
{
    public interface IOpenVpnExe
    {
        event EventHandler Exited;

        event EventHandler<EventArgs<string>> Output;
    }
}