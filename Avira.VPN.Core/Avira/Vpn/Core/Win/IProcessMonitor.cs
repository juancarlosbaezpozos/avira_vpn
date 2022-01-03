using System;
using System.Collections.Generic;
using Avira.Win.Messaging;

namespace Avira.VPN.Core.Win
{
    public interface IProcessMonitor : IDisposable
    {
        event EventHandler<EventArgs<string>> ProcessStarted;

        event EventHandler<EventArgs<string>> ProcessStopped;

        void Start();

        void Stop();

        List<string> ActiveProcesses();
    }
}