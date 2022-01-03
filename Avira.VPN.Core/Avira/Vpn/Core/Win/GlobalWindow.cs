using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Serilog;

namespace Avira.VPN.Core.Win
{
    public class GlobalWindow
    {
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Naming as in WinApi.")]
        internal static class NativeMethods
        {
            public delegate bool EnumThreadWindowsCallback(IntPtr hWnd, IntPtr lParam);

            public delegate bool EnumWinPropProc(IntPtr hWnd, IntPtr propery, IntPtr data, IntPtr lParam);

            public const int SW_HIDE = 0;

            public const int SW_SHOWNORMAL = 1;

            public const int SW_SHOWMINIMIZED = 2;

            public const int SW_SHOWMAXIMIZED = 3;

            public const int SW_SHOWNOACTIVATE = 4;

            public const int SW_RESTORE = 9;

            public const int SW_SHOWDEFAULT = 10;

            public const int WM_CLOSE = 16;

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SetProp(IntPtr hWnd, string lpString, IntPtr hData);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr GetProp(IntPtr hWnd, string lpString);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool EnumWindows(EnumThreadWindowsCallback callback, IntPtr extraData);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int EnumPropsExW(IntPtr hWnd, EnumWinPropProc enumProp, IntPtr extraData);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            public static extern IntPtr GetForegroundWindow();
        }

        private const string GlobalWindowId = "783A344C-5AE1-408f-BEC8-085DD6A4FB00";

        private const int GlobalWindowTag = 65451;

        private static List<IntPtr> guiHandles = new List<IntPtr>();

        private readonly string usersWindowId;

        private IntPtr globalWindowHandle;

        public bool IsForeground
        {
            get
            {
                IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
                if (globalWindowHandle != IntPtr.Zero && foregroundWindow != IntPtr.Zero)
                {
                    return globalWindowHandle == foregroundWindow;
                }

                return false;
            }
        }

        public GlobalWindow()
        {
            usersWindowId = "783A344C-5AE1-408f-BEC8-085DD6A4FB00" + WindowsIdentity.GetCurrent()?.Name;
        }

        public static void CloseAllGuiWindows()
        {
            foreach (IntPtr item in FindGuiWindowHandles())
            {
                NativeMethods.PostMessage(item, 16u, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public static bool HaveVisibleGuiWindows()
        {
            return FindGuiWindowHandles().Any(NativeMethods.IsWindowVisible);
        }

        public static List<IntPtr> FindGuiWindowHandles()
        {
            guiHandles.Clear();
            NativeMethods.EnumWindows(EnumWindowsByGlobalProperty, IntPtr.Zero);
            return guiHandles;
        }

        private static bool EnumWindowsByGlobalProperty(IntPtr hwnd, IntPtr lparam)
        {
            if (NativeMethods.EnumPropsExW(hwnd, EnumProps, IntPtr.Zero) == 0)
            {
                guiHandles.Add(hwnd);
            }

            return true;
        }

        private static bool EnumProps(IntPtr hwnd, IntPtr property, IntPtr data, IntPtr lparam)
        {
            if (data.ToInt32() != 65451)
            {
                return true;
            }

            try
            {
                if (Marshal.PtrToStringUni(property, 64).StartsWith("783A344C-5AE1-408f-BEC8-085DD6A4FB00"))
                {
                    return false;
                }
            }
            catch (Exception)
            {
            }

            return true;
        }

        public void Activate(Process process)
        {
            IntPtr intPtr = Get(process);
            if (intPtr == IntPtr.Zero)
            {
                Log.Warning($"Failed to find global window in process {process.Id}.");
                return;
            }

            NativeMethods.ShowWindow(intPtr, 9);
            NativeMethods.ShowWindow(intPtr, 1);
            NativeMethods.SetForegroundWindow(intPtr);
        }

        private IntPtr Get(Process process)
        {
            NativeMethods.EnumWindows(EnumTheWindows, (IntPtr)process.Id);
            return globalWindowHandle;
        }

        private bool EnumTheWindows(IntPtr windowHandle, IntPtr param)
        {
            if (NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId) == 0)
            {
                return true;
            }

            if (processId != (int)param)
            {
                return true;
            }

            IntPtr prop = NativeMethods.GetProp(windowHandle, usersWindowId);
            if (prop == IntPtr.Zero && (int)prop != 65451)
            {
                return true;
            }

            globalWindowHandle = windowHandle;
            return false;
        }

        public void Set(IntPtr windowHandle)
        {
            if (!NativeMethods.SetProp(windowHandle, usersWindowId, (IntPtr)65451))
            {
                throw new Exception(
                    $"Failed to set property {usersWindowId} to window. Error: {Marshal.GetLastWin32Error()}");
            }
        }

        public Process FindInstance(Process[] processes)
        {
            return (from process in processes
                let windowHandle = Get(process)
                where windowHandle != IntPtr.Zero
                select process).FirstOrDefault();
        }
    }
}