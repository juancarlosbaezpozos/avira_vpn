using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avira.Common.Core;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Serilog;

namespace Avira.VpnService
{
    public class TapDriver
    {
        private const string StorageKey = "tapDriverUpdateRequired";

        private readonly IProcess process;

        private readonly IWhiteList whitelist;

        private readonly ISettings settings;

        private string infPath;

        private string tapInstall;

        private string aviraRootCertificate = "Certificates\\avira.cer";

        private string workingDirectory;

        public TapDriver(IProcess process, IWhiteList whitelist, ISettings settings)
            : this(process, whitelist, settings, FileSystem.MakeFullPath("OpenVpn\\TAP"))
        {
        }

        public TapDriver(IProcess process, IWhiteList whitelist, ISettings settings, string openVpnTapPath)
        {
            this.settings = settings;
            this.process = process;
            this.whitelist = whitelist;
            ConstructComponentsFullPath(openVpnTapPath);
        }

        public void Install()
        {
            using CertificatesManager certificatesManager = new CertificatesManager(aviraRootCertificate);
            bool num = WindowsInfo.IsWindows10();
            if (!num)
            {
                Serilog.Log.Debug("Adding avira certificate to trusted publishers.");
                certificatesManager.AddToTrustedPublisher();
            }

            Serilog.Log.Debug("Installing tap driver...");
            Tuple<int, string> tuple = RunDevcon("install  " + infPath + " " + ProductSettings.TapDeviceName);
            if (!num)
            {
                Serilog.Log.Debug("Deleting avira certificate from trusted publishers.");
                certificatesManager.DeleteFromTrustedPublisher();
            }

            if (tuple.Item1 != 0 || !tuple.Item2.Contains("Drivers installed successfully"))
            {
                Tracker.TrackEvent(Tracker.Events.TapDriverError, new Dictionary<string, string>
                {
                    {
                        "Error",
                        tuple.ToString()
                    }
                });
                if (tuple.Item1 == 1)
                {
                    throw new VpnException(
                        "A new driver has been installed, please restart your computer to complete the installation",
                        ErrorType.TapAdapterRestartRequired);
                }

                throw new Exception("Couldn't install the Tap Driver. Output: " + tuple);
            }

            settings.Set("tapDriverUpdateRequired", false.ToString());
        }

        public void Uninstall()
        {
            Tuple<int, string> tuple = RunDevcon("tap_remove " + ProductSettings.TapDeviceName);
            if (tuple.Item1 != 0 || tuple.Item2.Contains("No devices were removed"))
            {
                throw new Exception("Couldn't uninstall the Tap Driver. Output: " + tuple);
            }
        }

        public void Update()
        {
            using CertificatesManager certificatesManager = new CertificatesManager(aviraRootCertificate);
            bool num = WindowsInfo.IsWindows10();
            if (!num)
            {
                Serilog.Log.Debug("Adding avira certificate to trusted publishers.");
                certificatesManager.AddToTrustedPublisher();
            }

            Serilog.Log.Debug("Updating the tap driver...");
            Tuple<int, string> tuple = RunDevcon("update " + infPath + " " + ProductSettings.TapDeviceName);
            if (!num)
            {
                Serilog.Log.Debug("Deleting avira certificate from trusted publishers.");
                certificatesManager.DeleteFromTrustedPublisher();
            }

            if (tuple.Item1 != 0 || !tuple.Item2.Contains("Drivers installed successfully"))
            {
                throw new Exception("Couldn't install the Tap Driver. Output: " + tuple);
            }
        }

        public bool IsUpdateRequired()
        {
            return bool.Parse(settings.Get("tapDriverUpdateRequired", false.ToString()));
        }

        public bool IsInstalled()
        {
            Tuple<int, string> tuple = RunDevcon("find " + ProductSettings.TapDeviceName);
            if (tuple.Item1 == 0)
            {
                return !tuple.Item2.Contains("No matching devices found.");
            }

            return false;
        }

        public bool IsRunning()
        {
            Tuple<int, string> tuple = RunDevcon("status " + ProductSettings.TapDeviceName);
            if (tuple.Item1 == 0)
            {
                return tuple.Item2.Contains("Driver is running.");
            }

            return false;
        }

        public void LogStatus()
        {
            Tuple<int, string> tuple = RunDevcon("status " + ProductSettings.TapDeviceName);
            Serilog.Log.Debug("TapDriver.Status: " + tuple.Item2.Replace("\n", "\r").Replace("\r", " "));
        }

        private void ConstructComponentsFullPath(string openVpnTapPath)
        {
            bool flag = WindowsInfo.Is64BitOperatingSystem();
            bool flag2 = WindowsInfo.IsWindows10();
            Serilog.Log.Information(flag2 ? "Selecting Windows 10 drivers..." : "Selecting Windows 7 drivers!");
            workingDirectory = openVpnTapPath;
            workingDirectory = Path.Combine(workingDirectory, flag2 ? "win10" : "win7");
            workingDirectory = Path.Combine(workingDirectory, flag ? "amd64" : "i386");
            tapInstall = Path.Combine(workingDirectory, "tapinstall.exe");
            infPath = "\"" + Path.Combine(workingDirectory, "OemVista.inf") + "\"";
            aviraRootCertificate = FileSystem.MakeFullPath(aviraRootCertificate);
        }

        private Tuple<int, string> RunDevcon(string arguments)
        {
            int num = new ProcessRunner(process, whitelist)
            {
                FileName = tapInstall,
                Arguments = arguments,
                WorkingDirectory = workingDirectory
            }.StartAndWaitForExit();
            if (num != 0)
            {
                Serilog.Log.Error("ERROR when running devcon.exe with arguments " + arguments + ". Exception: " +
                                  process.StandardError.ReadToEnd() + " Error: " + process.ExitCode);
            }

            return new Tuple<int, string>(num, process.StandardOutput.ReadToEnd());
        }

        private Version GetTapDriverVersion(string path)
        {
            FileVersionInfo versionInfo =
                FileVersionInfo.GetVersionInfo(Path.Combine(path, ProductSettings.TapDriverFileName));
            return new Version(versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart,
                versionInfo.FilePrivatePart);
        }

        public void UpdateTapDriverIfNecessary()
        {
            try
            {
                string path = Path.Combine(Environment.SystemDirectory, "drivers");
                Version tapDriverVersion = GetTapDriverVersion(path);
                Version tapDriverVersion2 = GetTapDriverVersion(workingDirectory);
                Serilog.Log.Debug("Installed Tap driver version : " + tapDriverVersion.ToString() +
                                  " Installation folder Tap driver version : " + tapDriverVersion2.ToString());
                if (tapDriverVersion < tapDriverVersion2)
                {
                    settings.Set("tapDriverUpdateRequired", true.ToString());
                }
            }
            catch (FileNotFoundException)
            {
                Serilog.Log.Debug("Tap driver not found.");
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Failed to check tap driver version after update.");
            }
        }
    }
}