using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Avira.VPN.Core.Win
{
    public class RemoteConnectionSettings
    {
        private string uri;

        [JsonProperty(PropertyName = "id")] public string Id { get; set; }

        [JsonProperty(PropertyName = "name")] public string Name { get; set; }

        [JsonProperty(PropertyName = "host")]
        public string Uri
        {
            get { return uri; }
            set
            {
                if (!string.IsNullOrEmpty(value) && !IsValidUri(value))
                {
                    throw new ArgumentException("Invalid connection URI!");
                }

                uri = value;
            }
        }

        [JsonProperty(PropertyName = "port")] public int Port { get; set; }

        [JsonProperty(PropertyName = "protocol")]
        public string Protocol { get; set; }

        [JsonProperty(PropertyName = "latency")]
        public string LatencyDisplay { get; set; }

        [JsonProperty(PropertyName = "license_type")]
        public string LicenseType { get; set; }

        public int Latency
        {
            get
            {
                IEnumerable<char> values = (LatencyDisplay ?? "").TakeWhile(char.IsDigit);
                int.TryParse(string.Join("", values), out var result);
                return result;
            }
        }

        public string FallbackProtocol { get; set; }

        public int FallbackPort { get; set; }

        public int TlsHadshakeWindow { get; set; }

        [JsonProperty(PropertyName = "trigger_source")]
        public string TriggerSource { get; set; }

        public RemoteConnectionSettings()
        {
        }

        public RemoteConnectionSettings(RemoteConnectionSettings remoteConnectionSettings)
        {
            Id = remoteConnectionSettings.Id;
            Name = remoteConnectionSettings.Name;
            Uri = remoteConnectionSettings.Uri;
            Port = remoteConnectionSettings.Port;
            Protocol = remoteConnectionSettings.Protocol;
            LatencyDisplay = remoteConnectionSettings.LatencyDisplay;
            FallbackProtocol = remoteConnectionSettings.FallbackProtocol;
            FallbackPort = remoteConnectionSettings.FallbackPort;
            TlsHadshakeWindow = remoteConnectionSettings.TlsHadshakeWindow;
            LicenseType = remoteConnectionSettings.LicenseType;
            TriggerSource = remoteConnectionSettings.TriggerSource;
        }

        private bool IsValidUri(string uri)
        {
            return !uri.Contains(" ");
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Protocol))
            {
                return Uri + " " + Port + " " + Protocol?.ToLower();
            }

            return Uri + " " + Port;
        }
    }
}