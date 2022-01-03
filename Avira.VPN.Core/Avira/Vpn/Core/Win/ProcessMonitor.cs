using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using Avira.Win.Messaging;

namespace Avira.VPN.Core.Win
{
    public sealed class ProcessMonitor : IProcessMonitor, IDisposable
    {
        private ManagementEventWatcher startWatch;

        private ManagementEventWatcher stopWatch;

        public event EventHandler<EventArgs<string>> ProcessStarted;

        public event EventHandler<EventArgs<string>> ProcessStopped;

        public void Start()
        {
            startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            startWatch.EventArrived += delegate(object s, EventArrivedEventArgs e)
            {
                this.ProcessStarted?.Invoke(this,
                    new EventArgs<string>(e.NewEvent.Properties["ProcessName"].Value.ToString()));
            };
            startWatch.Start();
            stopWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
            stopWatch.EventArrived += delegate(object s, EventArrivedEventArgs e)
            {
                this.ProcessStopped?.Invoke(this,
                    new EventArgs<string>(e.NewEvent.Properties["ProcessName"].Value.ToString()));
            };
            stopWatch.Start();
        }

        public void Stop()
        {
            startWatch?.Stop();
            stopWatch?.Stop();
        }

        public List<string> ActiveProcesses()
        {
            return (from p in Process.GetProcesses()
                select p.ProcessName).ToList();
        }

        public void Dispose()
        {
            Stop();
            startWatch?.Dispose();
            stopWatch?.Dispose();
        }
    }
}