using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    [Guid("C0E8AE9F-306E-11D1-AACF-00805FC1270E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface INetCfgLock
    {
        [PreserveSig]
        int AcquireWriteLock([In] [MarshalAs(UnmanagedType.I4)] uint cmsTimeout,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pszwClientDescription,
            [MarshalAs(UnmanagedType.LPWStr)] out string ppszwClientDescription);

        [PreserveSig]
        int ReleaseWriteLock();

        [PreserveSig]
        int IsWriteLocked([Out] [MarshalAs(UnmanagedType.LPWStr)] string ppszwClientDescription);
    }
}