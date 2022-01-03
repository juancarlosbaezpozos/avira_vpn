using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace Avira.Common.Core
{
    public static class WindowsInfo
    {
        internal static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern uint GetWindowsDirectory(StringBuilder lpBuffer, uint uSize);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWow64Process([In] IntPtr processHandle, out bool wow64Process);

            [DllImport("shlwapi.dll", EntryPoint = "#437", SetLastError = true)]
            public static extern bool IsOS(int os);
        }

        private static OsType? osType;

        public static string OsPlatform
        {
            get
            {
                if (!Is64BitOperatingSystem())
                {
                    return "x86";
                }

                return "x64";
            }
        }

        public static OsType OsType
        {
            get
            {
                osType = osType ?? GetOsType();
                return osType.Value;
            }
        }

        public static bool Is64BitOperatingSystem()
        {
            if (IntPtr.Size != 8)
            {
                return InternalCheckIsWow64();
            }

            return true;
        }

        private static OsType GetOsType()
        {
            if (!NativeMethods.IsOS(29))
            {
                return OsType.Desktop;
            }

            return OsType.Server;
        }

        private static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6)
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    if (!NativeMethods.IsWow64Process(process.Handle, out var wow64Process))
                    {
                        return false;
                    }

                    return wow64Process;
                }
            }

            return false;
        }

        public static bool IsWinXpOrLower()
        {
            return Environment.OSVersion.Version.Major < 6;
        }

        public static Version VersionNumber()
        {
            try
            {
                string[] array = FileVersionInfo
                    .GetVersionInfo(Path.Combine(Environment.SystemDirectory, "kernel32.dll")).FileVersion.Split('.');
                return new Version(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));
            }
            catch (Exception)
            {
                return new Version(0, 0, 0, 0);
            }
        }

        public static bool IsWindows10()
        {
            RegistryKey registryKey =
                Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion");
            if (registryKey == null)
            {
                return false;
            }

            string text = (string)registryKey.GetValue("ProductName");
            registryKey.Close();
            return text.StartsWith("Windows 10");
        }
    }
}