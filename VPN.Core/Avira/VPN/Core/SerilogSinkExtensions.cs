using System;
using Serilog;
using Serilog.Configuration;

namespace Avira.VPN.Core
{
    public static class SerilogSinkExtensions
    {
        public static LoggerConfiguration SerilogSink(this LoggerSinkConfiguration loggerConfiguration,
            string serverName, string defaultDSN, IFormatProvider formatProvider = null)
        {
            IProductSettings productSettings = DiContainer.Resolve<IProductSettings>();
            bool isStoreApplication = DiContainer.Resolve<IDevice>().IsSandboxed();
            string deviceId = DiContainer.Resolve<IApplicationIds>()?.ClientId;
            string sentryUrl = DiContainer.Resolve<ISettings>().Get("sentry_url", defaultDSN);
            return loggerConfiguration.SerilogSink(sentryUrl, "user", serverName, productSettings.ProductVersion,
                productSettings.ProductLanguage, isStoreApplication, deviceId,
                () => DiContainer.Resolve<IAppSettings>().Get().AppImprovement, formatProvider);
        }

        public static LoggerConfiguration SerilogSink(this LoggerSinkConfiguration loggerConfiguration,
            string sentryUrl, string sentryUserName, string serverName, string productVersion, string language,
            bool isStoreApplication, string deviceId, Func<bool> evaluateTracking,
            IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new SentrySerilogSink(formatProvider, sentryUrl, sentryUserName, serverName,
                productVersion, language, isStoreApplication, deviceId, evaluateTracking));
        }
    }
}