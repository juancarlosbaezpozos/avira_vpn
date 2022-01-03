using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Serilog;

namespace Avira.VPN.Notifier
{
    internal static class Program
    {
        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        [STAThread]
        private static void Main()
        {
            DiContainer.SetInstance<JsonStorage>(new JsonStorage(ProductSettings.GetJsonDefaults()));
            Logger.SetDefaultInstance();
            Log.Debug("Notifier started.");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(defaultValue: false);
            Application.Run(new NotificationWindow());
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
    }
}