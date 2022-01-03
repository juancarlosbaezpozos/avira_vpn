using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using Avira.Common.Core;
using Avira.VPN.Core.Win;
using Avira.Win.Messaging;
using Serilog;

namespace Avira.VpnService
{
    public class OpenVpnExe : IDisposable, IOpenVpnExe
    {
        private readonly string managementServer;

        private readonly int port;

        private readonly IProcess process;

        private readonly IWhiteList whitelist;

        private bool disposed;

        public string ChildLog { get; private set; }

        public string TlsLog { get; private set; }

        public event EventHandler Exited
        {
            add { process.Exited += value; }
            remove { process.Exited -= value; }
        }

        public event EventHandler<EventArgs<string>> Output;

        public OpenVpnExe(string managementServer, int port)
        {
            this.managementServer = managementServer;
            this.port = port;
            process = new ProcessWrapper();
            whitelist = new PathWhiteList();
        }

        public void Start(RemoteConnectionSettings region, string adapter)
        {
            Serilog.Log.Debug($"Starting OpenVpn Management Server on {managementServer}:{port}");
            ChildLog = string.Empty;
            TlsLog = string.Empty;
            ProcessRunner processRunner = new ProcessRunner(process, whitelist)
            {
                FileName = FileSystem.MakeFullPath(ProductSettings.OpenVpnPath),
                Arguments = CreateCommandLineParameters(region, ProductSettings.OpenVpnConfigPath, managementServer,
                    port, adapter),
                WorkingDirectory = FileSystem.MakeFullPath("OpenVpn")
            };
            process.EnableRaisingEvents = true;
            processRunner.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
            {
                if (e.Data != null)
                {
                    if (!new Regex("^\\w{3} \\w{3} \\d{1,2}").IsMatch(e.Data) && e.Data != string.Empty)
                    {
                        ChildLog = ChildLog + "\n" + e.Data;
                    }

                    if (e.Data.IndexOf("tls", StringComparison.InvariantCultureIgnoreCase) != -1)
                    {
                        TlsLog = TlsLog + "\n" + e.Data;
                    }

                    this.Output?.Invoke(this, new EventArgs<string>(e.Data));
                }
            };
            processRunner.Start();
            Serilog.Log.Information("OpenVpn was started with arguments : " + processRunner.Arguments);
        }

        public void Stop()
        {
            EventWaitHandle eventWaitHandle = new EventWaitHandle(initialState: false, EventResetMode.ManualReset,
                "AviraVPNOpenVpnQuitEvent");
            eventWaitHandle.Set();
            try
            {
                process.WaitForExit();
                process.Close();
            }
            catch (Exception exception)
            {
                Serilog.Log.Information(exception, "Could not stop openvpn process.");
            }

            eventWaitHandle.Reset();
            Serilog.Log.Information("OpenVpn stopped.");
        }

        private static string CreateCommandLineParameters(RemoteConnectionSettings region, string configPath,
            string managementServer, int managementPort, string adapter)
        {
            if (region == null)
            {
                throw new Exception("Can't get VPN default server.");
            }

            Serilog.Log.Information($"Default VPN server {region.Uri}:{region.Port} using {region.Protocol}");
            InterfaceConfig interfaceConfig =
                new InterfaceConfig("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters");
            Serilog.Log.Debug($"OpenVpnExe: tls handshake window {region.TlsHadshakeWindow}");
            return new CommandLineParametersBuilder
            {
                ConfigFilePath = configPath,
                ManagementConsoleSettings = new RemoteConnectionSettings
                {
                    Uri = managementServer,
                    Port = managementPort
                },
                OpenVpnServerSettings = region,
                IsIpV6 = interfaceConfig.IsIpV6(),
                AdapterName = adapter
            }.Create();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    process.Close();
                }

                disposed = true;
            }
        }
    }
}