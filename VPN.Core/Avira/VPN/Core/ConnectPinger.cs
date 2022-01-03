using System;
using Avira.Messaging;
using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public sealed class ConnectPinger : GenericPinger
    {
        private const string LastPingSentName = "last_device_ping_sent";

        private readonly int intervalInMiliseconds;

        [Routing("sendDevicePing")] public event EventHandler TriggerSendGdprPing;

        public ConnectPinger(int intervalInMiliseconds)
            : base(intervalInMiliseconds, "last_device_ping_sent", startOnCreation: false)
        {
            this.intervalInMiliseconds = intervalInMiliseconds;
        }

        public override void SendPing()
        {
            this.TriggerSendGdprPing?.Invoke(this, EventArgs.Empty);
            DiContainer.Resolve<ISettings>().Set("last_device_ping_sent", JsonConvert.SerializeObject(DateTime.Now));
        }

        [Routing("startDevicePinger")]
        public void StartConnectPinger()
        {
            StartPinger();
        }
    }
}