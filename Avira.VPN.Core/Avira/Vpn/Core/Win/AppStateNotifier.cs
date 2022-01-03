using System;
using Microsoft.Win32;
using Serilog;

namespace Avira.VPN.Core.Win
{
    [DiContainer.Export(typeof(IAppStateNotifier))]
    public class AppStateNotifier : IAppStateNotifier
    {
        public event EventHandler Suspending;

        public event EventHandler Resuming;

        public event EventHandler ResumingWithInternetAccess;

        public AppStateNotifier()
        {
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        private async void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                Log.Debug("PowerModeChanged: suspend");
                this.Suspending?.Invoke(this, new EventArgs());
            }

            if (e.Mode == PowerModes.Resume)
            {
                Log.Debug("PowerModeChanged: resume");
                this.Resuming?.Invoke(this, new EventArgs());
                if (await HttpAsyncHelper.WaitForInternetConnection(60000))
                {
                    Log.Debug("PowerModeChanged - resume: got internet connection");
                    this.ResumingWithInternetAccess?.Invoke(this, new EventArgs());
                }
            }
        }
    }
}