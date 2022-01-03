using System;
using Newtonsoft.Json;
using Serilog;

namespace Avira.VPN.Core
{
    public static class OeReportSettings
    {
        private const string oeLastReportKey = "last_oe_report";

        private const string oeStatusUpdateIntervalKey = "oe_status_update_interval";

        private static DateTime lastOeReport = DateTime.MinValue;

        private static ISettings Settings => DiContainer.Resolve<ISettings>();

        public static DateTime LastOeReport
        {
            get
            {
                if (lastOeReport == DateTime.MinValue)
                {
                    lastOeReport = JsonConvert.DeserializeObject<DateTime>(Settings?.Get("last_oe_report",
                        JsonConvert.SerializeObject(new DateTime(2000, 1, 1))));
                }

                return lastOeReport;
            }
            set
            {
                lastOeReport = value;
                Settings?.Set("last_oe_report", JsonConvert.SerializeObject(value));
            }
        }

        public static TimeSpan OeStatusUpdateInterval
        {
            get
            {
                try
                {
                    return TimeSpan.FromSeconds(int.Parse(Settings?.Get("oe_status_update_interval", "86400")));
                }
                catch (Exception exception)
                {
                    Log.Warning(exception, "Getting OeStatusUpdateInterval failed.");
                    return TimeSpan.FromSeconds(86400.0);
                }
            }
        }
    }
}