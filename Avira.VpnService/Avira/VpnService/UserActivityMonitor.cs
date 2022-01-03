using System;
using System.Diagnostics.CodeAnalysis;
using System.Timers;
using Avira.VPN.Core.Win;
using Serilog;

namespace Avira.VpnService
{
    public sealed class UserActivityMonitor : IDisposable
    {
        private readonly IServicePersistentData persistentData;

        private readonly Timer popupTimer;

        private readonly TimeSpan userInactivityThreshold;

        private bool isAfterInstallation;

        private DateTime popupTimerStart;

        public event EventHandler Inactive;

        public event EventHandler Installed;

        public UserActivityMonitor(TimeSpan inactivityThreshold, TimeSpan popupTimeout)
            : this(inactivityThreshold, popupTimeout, new ServicePersistentData())
        {
        }

        internal UserActivityMonitor(TimeSpan inactivityThreshold, TimeSpan popupTimeout,
            IServicePersistentData persistentData)
        {
            userInactivityThreshold = inactivityThreshold;
            this.persistentData = persistentData;
            popupTimer = new Timer(popupTimeout.TotalMilliseconds)
            {
                AutoReset = false
            };
            popupTimer.Elapsed += OnTimedEvent;
        }

        public void OnStart()
        {
            Log.Debug(
                $"UserActivityMonitor.OnStart: LastActivityNotification = {persistentData.LastActivityNotification}, LastConnect = {ProductSettings.LastConnect}");
            if (IsOnStartAfterInstallation() && persistentData.LastActivityNotification == DateTime.MinValue)
            {
                isAfterInstallation = true;
                popupTimer.Start();
                DateTime dateTime = (popupTimerStart = (persistentData.LastActivityNotification = DateTime.UtcNow));
                return;
            }

            DateTime dateTime2 = (IsConnectPerformed() ? ProductSettings.LastConnect : ProductSettings.InstallDate);
            DateTime dateTime3 = ((persistentData.LastActivityNotification > dateTime2)
                ? persistentData.LastActivityNotification
                : dateTime2);
            if (!(DateTime.UtcNow - dateTime3 <= userInactivityThreshold))
            {
                isAfterInstallation = false;
                popupTimer.Start();
                persistentData.LastActivityNotification = DateTime.UtcNow;
            }
        }

        private static bool IsConnectPerformed()
        {
            return ProductSettings.LastConnect != DateTime.MinValue;
        }

        private bool IsOnStartAfterInstallation()
        {
            TimeSpan timeSpan = new TimeSpan(0, 0, 2, 0);
            if (DateTime.UtcNow - ProductSettings.InstallDate <= timeSpan)
            {
                return !IsConnectPerformed();
            }

            return false;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (isAfterInstallation)
            {
                if (popupTimerStart < ProductSettings.LastConnect)
                {
                    Log.Debug("Skiping Installed event due to user activity");
                }
                else
                {
                    this.Installed?.Invoke(this, null);
                }
            }
            else
            {
                this.Inactive?.Invoke(this, null);
            }
        }

        [SuppressMessage("ReSharper", "UseNullPropagation", Justification = "Issue with parser.")]
        public void Dispose()
        {
            if (popupTimer != null)
            {
                popupTimer.Dispose();
            }
        }
    }
}