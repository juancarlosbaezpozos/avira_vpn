using System;

namespace Avira.VpnService
{
    public abstract class OpenVpnNotification : EventArgs
    {
        public DateTime Timestamp { get; protected set; }

        public string Reason { get; protected set; }

        protected static DateTime UnixTimeToDateTime(string timestamp)
        {
            if (!double.TryParse(timestamp, out var result))
            {
                throw new Exception("Invalid Unix Timestamp.");
            }

            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(result).ToLocalTime();
        }
    }
}