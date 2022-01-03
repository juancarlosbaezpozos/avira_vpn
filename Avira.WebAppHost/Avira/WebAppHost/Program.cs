using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Avira.WebAppHost.Properties;
using Avira.Win.Messaging;
using Serilog;

namespace Avira.WebAppHost
{
    public class Program
    {
        private static Mutex guardMutex;

        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        [STAThread]
        public static int Main(string[] args)
        {
            DiContainer.SetInstance<JsonStorage>(new JsonStorage(ProductSettings.GetJsonDefaults()));
            Logger.SetDefaultInstance();
            Application.ThreadException += ApplicationOnThreadException;
            if (StartForServiceRestart(args))
            {
                return RestartService();
            }

            if (MigrateSettings(args))
            {
                ExecuteSettingsMigration();
                return 0;
            }

            if (UpdateLicense(args))
            {
                SendUpdateLicenseMessageToService();
                return 0;
            }

            Process[] processes = FindRunningInstances();
            GlobalWindow globalWindow = new GlobalWindow();
            Process process = globalWindow.FindInstance(processes);
            if (process != null)
            {
                if (StartMinimized(args))
                {
                    return 0;
                }

                Log.Debug("Activate user instance");
                globalWindow.Activate(process);
                return 0;
            }

            if (IsAnotherInstanceRunningForCurrentUser())
            {
                Log.Debug("Another instance running for user is detected. Exit.");
                return 0;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(defaultValue: false);
            Application.Run(new VpnGuiForm(StartMinimized(args), ShowSettingsView(args)));
            return 0;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("Avira.WebAppHost.resources"))
            {
                return null;
            }

            if (args.Name.StartsWith("Avira."))
            {
                int num = args.Name.IndexOf(',');
                string path = args.Name.Substring("Avira.".Length, num - "Avira.".Length) + ".dll";
                return Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
            }

            return null;
        }

        private static void ExecuteSettingsMigration()
        {
            try
            {
                ProductSettings.UpgradeSettings();
                ProductSettings.WindowLocation = Settings.Default.WindowLocation;
                ProductSettings.UiSettings = Settings.Default.UiSettings;
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to migrate Avira.WebAppHost settings.");
            }

            ProductSettings.DeleteUserSettingsFolder();
        }

        private static bool IsAnotherInstanceRunningForCurrentUser()
        {
            bool createdNew = false;
            guardMutex = new Mutex(initiallyOwned: true, ProductSettings.WebAppHostMutex, out createdNew);
            if (!createdNew)
            {
                return true;
            }

            return false;
        }

        private static bool MigrateSettings(string[] args)
        {
            return args.Contains("/migrateSettings");
        }

        private static bool StartMinimized(IEnumerable<string> args)
        {
            return args.Contains("/hide");
        }

        private static bool ShowSettingsView(IEnumerable<string> args)
        {
            return args.Contains("/settingsView");
        }

        private static bool StartForServiceRestart(IEnumerable<string> args)
        {
            return args.Contains("/service");
        }

        private static bool UpdateLicense(IEnumerable<string> args)
        {
            return args.Contains("/updateLicense");
        }

        public static bool WaitUntil(Func<bool> condition, int timeout = 4000)
        {
            int tickCount = Environment.TickCount;
            while (Environment.TickCount - tickCount < timeout)
            {
                if (condition())
                {
                    return true;
                }

                Thread.Sleep(5);
            }

            return false;
        }

        private static void SendUpdateLicenseMessageToService()
        {
            try
            {
                Log.Information("VpnGui: sending update license to service.");
                PipeChannelFactory pipeChannelFactory = new PipeChannelFactory();
                IService service = new ServiceInterfaceFactory(new ServiceLocator(), pipeChannelFactory)
                    .CreateServiceInterface("VPN");
                bool responceReceived = false;
                service.Request(Avira.Win.Messaging.Message.CreateRequest("license/update"),
                    delegate { responceReceived = true; }, null);
                WaitUntil(() => responceReceived);
                pipeChannelFactory.Dispose();
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Error on creating communication channel with service.");
            }
        }

        private static void ApplicationOnThreadException(object sender,
            ThreadExceptionEventArgs threadExceptionEventArgs)
        {
            Log.Error(threadExceptionEventArgs.Exception, "Unhandled exception.");
        }

        private static Process[] FindRunningInstances()
        {
            return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location));
        }

        private static int RestartService()
        {
            int result = 0;
            using ServiceController serviceController = new ServiceController
            {
                ServiceName = ProductSettings.ServiceName
            };
            if (serviceController.Status == ServiceControllerStatus.Running)
            {
                return result;
            }

            if (serviceController.Status == ServiceControllerStatus.StartPending)
            {
                Log.Debug("Service " + ProductSettings.ServiceName +
                          " is in starting state. Trying to kill the process and stop the service.");
                KillProcess(serviceController);
            }

            Log.Debug("Starting " + ProductSettings.ServiceName + " service ...");
            try
            {
                TimeSpan timeout = TimeSpan.FromSeconds(5.0);
                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running, timeout);
                Log.Debug(ProductSettings.ServiceName + " service is started");
                return result;
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Can't start " + ProductSettings.ServiceName);
                return -1;
            }
        }

        private static void KillProcess(ServiceController sc)
        {
            try
            {
                Process[] processesByName = Process.GetProcessesByName("Avira.VpnService");
                foreach (Process process in processesByName)
                {
                    process.Kill();
                    Log.Debug($"Killed process Id: {process.Id}, Name: {process.ProcessName}");
                }

                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5.0));
                Log.Debug("Service " + ProductSettings.ServiceName + " is in state: " + sc.Status);
            }
            catch (Exception exception)
            {
                Log.Debug(exception,
                    "Error trying to kill the process or the service does not reach status 'stopped'.");
            }
        }
    }
}