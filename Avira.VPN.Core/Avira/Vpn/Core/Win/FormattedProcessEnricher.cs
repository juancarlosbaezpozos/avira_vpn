using System;
using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Avira.VPN.Core.Win
{
    public class FormattedProcessEnricher : ILogEventEnricher
    {
        private readonly string cached_id;

        private readonly string cached_name;

        private const string PropertyName = "ProcessMeta";

        public FormattedProcessEnricher()
        {
            using Process process = Process.GetCurrentProcess();
            cached_id = "[" + process.Id.ToString("00000") + "]";
            cached_name = "[" + string.Format("{0,16}", process.ProcessName) + "]";
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            string value = cached_id + " [" + Environment.CurrentManagedThreadId.ToString("00") + "] " + cached_name;
            LogEventProperty property = propertyFactory.CreateProperty("ProcessMeta", value);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}