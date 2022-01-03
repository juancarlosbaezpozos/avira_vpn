using System;
using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    [Guid("C0E8AE9D-306E-11D1-AACF-00805FC1270E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface INetCfgClassSetup
    {
        [PreserveSig]
        int SelectAndInstall([In] IntPtr hwndParent, [In] IntPtr oboToken,
            [MarshalAs(UnmanagedType.IUnknown)] out object item);

        [PreserveSig]
        int Install([In] [MarshalAs(UnmanagedType.LPWStr)] string pszwInfId, [In] IntPtr oboToken,
            [In] [MarshalAs(UnmanagedType.U4)] int setupFlags,
            [In] [MarshalAs(UnmanagedType.U4)] int upgradeFromBuildNo,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string answerFile,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string answerSections,
            [MarshalAs(UnmanagedType.IUnknown)] out object item);

        [PreserveSig]
        int DeInstall([Out] [MarshalAs(UnmanagedType.IUnknown)] object component, [In] IntPtr oboToken,
            [MarshalAs(UnmanagedType.LPWStr)] out string refs);
    }
}