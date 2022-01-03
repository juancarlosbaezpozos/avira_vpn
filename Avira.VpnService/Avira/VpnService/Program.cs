using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Serilog;

namespace Avira.VpnService
{
    public static class Program
    {
        private static ServiceBase serviceToRun;

        static Program()
        {
            if (!Assembly.GetExecutingAssembly().GetName().Name.StartsWith("Avira."))
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        public static void Main(string[] args)
        {
            DiContainer.SetInstance<JsonStorage>(new JsonStorage(ProductSettings.GetJsonDefaults()));
            Logger.SetDefaultInstance();
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs eventArgs)
            {
                Log.Error(eventArgs.ExceptionObject as Exception, "Unhandled exception.");
            };
            CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("en-US");
            try
            {
                cultureInfo = CultureInfo.CreateSpecificCulture(ProductSettings.ProductLanguage);
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "CreateSpecificCulture failed.");
            }

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            if (IsConsoleMode(args))
            {
                RunInConsoleMode();
            }
            else
            {
                RunAsWindowsService();
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("Avira."))
            {
                int num = args.Name.IndexOf(',');
                string path = args.Name.Substring("Avira.".Length, num - "Avira.".Length) + ".dll";
                return Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
            }

            return null;
        }

        private static void RunInConsoleMode()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            Log.Debug("Running in console mode");
            serviceToRun = new VpnService();
            Console.WriteLine("Service running in interactive mode.");
            Console.WriteLine();
            Log.Debug("ServiceHost started");
            MethodInfo method =
                typeof(ServiceBase).GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic);
            Console.Write("Starting {0}...", serviceToRun.ServiceName);
            method.Invoke(serviceToRun, new object[1] { new string[0] });
            Console.Write("Started");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Press any key to stop the service ...");
            Console.ReadKey();
            Console.WriteLine();
            MethodInfo method2 =
                typeof(ServiceBase).GetMethod("OnStop", BindingFlags.Instance | BindingFlags.NonPublic);
            Console.Write("Stopping {0}...", serviceToRun.ServiceName);
            method2.Invoke(serviceToRun, null);
            Console.WriteLine("Stopped");
            Log.Debug("ServiceHost stopped");
            Thread.Sleep(1000);
        }

        private static void RunAsWindowsService()
        {
            ServiceBase.Run(new ServiceBase[1]
            {
                new VpnService()
            });
        }

        private static bool IsConsoleMode(IEnumerable<string> args)
        {
            return args.Contains("/console");
        }
    }
}