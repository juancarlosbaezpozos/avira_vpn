using System;
using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class DisconnectTimerEventArgs : EventArgs
    {
        [JsonProperty(PropertyName = "RemainingSeconds")]
        public int RemainingSeconds { get; set; }

        [JsonProperty(PropertyName = "stopCountdown")]
        public string StopCountdown { get; set; }
    }
}