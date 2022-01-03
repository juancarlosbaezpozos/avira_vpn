using System.IO;
using Serilog;
using Serilog.Events;

namespace Avira.VPN.Core.Win
{
    public class Logger
    {
        public static string LogFilePath => Path.Combine(ProductSettings.SettingsFilePath, "vpndbg.log");

        public static void SetDefaultInstance()
        {
            SetDefaultInstance(LogFilePath);
        }

        public static void SetDefaultInstance(string filePath)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().Enrich.WithProcessMeta().WriteTo
                .File(filePath, LogEventLevel.Verbose,
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {ProcessMeta} {Message:lj}{NewLine}{Exception}",
                    null, 10485760L, null, buffered: false).WriteTo.SerilogSink(ProductSettings.SentryUrl, "user",
                    "default", ProductSettings.ProductVersion, ProductSettings.ProductLanguage,
                    isStoreApplication: false, GeneratedDeviceInfo.GetClientId(),
                    () => ProductSettings.ProductImprovementUserSetting).CreateLogger();
        }

        public static void SetTestEnvInstance(string filePath)
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().Enrich.WithProcessMeta().WriteTo.File(filePath,
                LogEventLevel.Verbose,
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {ProcessMeta} {Message:lj}{NewLine}{Exception}",
                null, 10485760L, null, buffered: false).CreateLogger();
        }
    }
}