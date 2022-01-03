using System.Diagnostics;
using Avira.Common.Core;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Serilog;

namespace Avira.VpnService
{
    public class NetworkBlocker
    {
        public static bool Enabled { get; set; }

        public static int Enable()
        {
            if (!DiContainer.Resolve<IFeatures>().IsActive("kill_switch"))
            {
                return 0;
            }

            Serilog.Log.Information("NetworkBlocker::Enable");
            int num = Run(string.Empty);
            if (num == 0)
            {
                Enabled = true;
            }

            return num;
        }

        public static int Disable()
        {
            if (!DiContainer.Resolve<IFeatures>().IsActive("kill_switch"))
            {
                return 0;
            }

            Serilog.Log.Information("NetworkBlocker::Disable");
            int num = Run("delete");
            if (num == 0)
            {
                Enabled = false;
            }

            return num;
        }

        private static int Run(string argument)
        {
            if (!DiContainer.Resolve<IFeatures>().IsActive("kill_switch"))
            {
                Serilog.Log.Information("Kill switch feature disabled");
                return 0;
            }

            using ProcessWrapper processWrapper = new ProcessWrapper(new Process());
            int result = new ProcessRunner(processWrapper, new PathWhiteList())
            {
                FileName = FileSystem.MakeFullPath(ProductSettings.NetworkBlockerFileName),
                Arguments = argument
            }.StartAndWaitForExit();
            Serilog.Log.Information(processWrapper.StandardOutput.ReadToEnd());
            return result;
        }
    }
}