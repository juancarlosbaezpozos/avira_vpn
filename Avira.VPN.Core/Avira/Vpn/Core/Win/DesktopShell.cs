using System;
using Serilog;
using SHDocVw;
using Shell32;

namespace Avira.VPN.Core.Win
{
    public class DesktopShell
    {
        public static void ShellExecute(string fileName)
        {
            ShellExecute(fileName, string.Empty, string.Empty);
        }

        public static void ShellExecute(string fileName, string parameters, string directory, string verb = "open",
            int show = 0)
        {
            try
            {
                GetDesktopShellIDispatch2().ShellExecute(fileName, parameters, directory, verb, show);
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "ShellExecute failed running " + fileName);
            }
        }

        internal static IShellDispatch2 GetDesktopShellIDispatch2()
        {
            return FindShellDispatch(
                (IShellWindows)Activator.CreateInstance(Type.GetTypeFromCLSID(ShellComHelpers.clsidShellWindows)));
        }

        private static IShellDispatch2 FindShellDispatch(IShellWindows shellWindows)
        {
            object pvarloc = 0;
            object pvarlocRoot = 0;
            int pHWND;
            dynamic val = shellWindows.FindWindowSW(ref pvarloc, ref pvarlocRoot, 8, out pHWND, 1);
            long num = shellWindows.Count;
            if (num != 0L)
            {
                for (int i = 0; i < num; i++)
                {
                    IShellDispatch2 shellDispatch = GetShellDispatch(shellWindows.Item(i));
                    if (shellDispatch != null)
                    {
                        return shellDispatch;
                    }
                }

                return DesktopShell.GetShellDispatch(val);
            }

            return DesktopShell.GetShellDispatch(val);
        }

        private static IShellDispatch2 GetShellDispatch(object firstDesktop)
        {
            if (firstDesktop == null)
            {
                return null;
            }

            ShellComHelpers.IShellBrowser shellBrowser = ShellComHelpers.GetShellBrowser(firstDesktop);
            ShellComHelpers.IShellView ppshv = null;
            shellBrowser.QueryActiveShellView(ref ppshv);
            ppshv.GetItemObject(0u, ref ShellComHelpers.iidDispatch, out var ppv);
            try
            {
                IShellFolderViewDual shellFolderViewDual = (IShellFolderViewDual)ppv;
                return (IShellDispatch2)(dynamic)shellFolderViewDual.Application;
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}