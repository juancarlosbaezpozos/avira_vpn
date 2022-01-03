using System;
using Serilog;
using Serilog.Configuration;

namespace Avira.VPN.Core.Win
{
    public static class LoggingExtensions
    {
        public static LoggerConfiguration WithProcessMeta(this LoggerEnrichmentConfiguration enrich)
        {
            if (enrich == null)
            {
                throw new ArgumentNullException("enrich");
            }

            return enrich.With<FormattedProcessEnricher>();
        }
    }
}