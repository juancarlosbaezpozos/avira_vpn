using System;
using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    internal static class NativeMethods
    {
        public enum ServiceState
        {
            SERVICE_STOPPED = 1,
            SERVICE_START_PENDING,
            SERVICE_STOP_PENDING,
            SERVICE_RUNNING,
            SERVICE_CONTINUE_PENDING,
            SERVICE_PAUSE_PENDING,
            SERVICE_PAUSED
        }

        public struct ServiceStatus
        {
            public int dwServiceType;

            public ServiceState dwCurrentState;

            public int dwControlsAccepted;

            public int dwWin32ExitCode;

            public int dwServiceSpecificExitCode;

            public int dwCheckPoint;

            public int dwWaitHint;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
    }
}